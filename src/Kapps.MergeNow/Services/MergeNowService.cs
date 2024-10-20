using EnvDTE;
using MergeNow.Utils;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeNow.Services
{
    internal class MergeNowService : IMergeNowService
    {
        private readonly AsyncPackage _asyncPackage;
        private AsyncLazy<VersionControlServer> _versionControlConnectionTask;

        public MergeNowService(AsyncPackage asyncPackage)
        {
            _asyncPackage = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            RenewVersionControlConnectection();
        }

        public async Task<IEnumerable<string>> GetTargetBranchesAsync(string changeset)
        {
            if (!int.TryParse(changeset, out int changesetNumber))
            {
                throw new ArgumentException("Please provide a valid changeset number.");
            }

            var versionControlServer = await GetVersionControlAsync();

            Changeset tfsChangeset = versionControlServer.GetChangeset(changesetNumber);
            if (changeset == null)
            {
                throw new InvalidOperationException($"Changeset '{changeset}' does not exist.");
            }

            var targetBranches = new List<string>();

            var branchOwnerships = versionControlServer.QueryBranchObjectOwnership(new[] { tfsChangeset.ChangesetId });
            var sourceBranches = branchOwnerships.Select(bo => bo.RootItem.Item).ToList();

            foreach (var sourceBranch in sourceBranches)
            {
                var branches = versionControlServer.QueryMergeRelationships(sourceBranch)
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.Item)
                    .Reverse();

                targetBranches.AddRange(branches);
            }

            return targetBranches;
        }

        public async Task MergeAsync(string changeset, string targetBranch)
        {
            if (!int.TryParse(changeset, out int changesetNumber))
            {
                throw new ArgumentException("Please provide a valid changeset number.");
            }

            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new ArgumentException("Please provide a valid target branch.");
            }

            var versionControlServer = await GetVersionControlAsync();

            Changeset tfsChangeset = versionControlServer.GetChangeset(changesetNumber);
            if (changeset == null)
            {
                throw new InvalidOperationException($"Changeset '{changeset}' does not exist.");
            }

            BranchObjectOwnership[] branchOwnerships = versionControlServer.QueryBranchObjectOwnership(new[] { changesetNumber });
            var sourceBranches = branchOwnerships.Select(bo => bo.RootItem.Item).ToList();

            var pendingChangesPage = await GetPendingChangesPageAsync();
            Workspace workspace = GetCurrentWorkspace(pendingChangesPage);
            ChangesetVersionSpec changesetVersionSpec = new ChangesetVersionSpec(changeset);

            foreach (var sourceBranch in sourceBranches)
            {
                workspace.Merge(sourceBranch, targetBranch, changesetVersionSpec, changesetVersionSpec, LockLevel.None, RecursionType.Full, MergeOptionsEx.None);
            }

            var mergeComment = GetMergeComment(sourceBranches, targetBranch, tfsChangeset);
            SetMergeComment(mergeComment, pendingChangesPage);

            foreach (var workItem in tfsChangeset.WorkItems)
            {
                AssociateWorkItem(workItem.Id, pendingChangesPage);
            }

            pendingChangesPage.Refresh();
        }

        private async Task<VersionControlServer> GetVersionControlAsync()
        {
            try
            {
                return await _versionControlConnectionTask.GetValueAsync();
            }
            catch
            {
                RenewVersionControlConnectection();
                throw;
            }
        }

        private void RenewVersionControlConnectection()
        {
            _versionControlConnectionTask = new AsyncLazy<VersionControlServer>(ConnectToVersionControlAsync, ThreadHelper.JoinableTaskFactory);
        }

        private async Task<VersionControlServer> ConnectToVersionControlAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var dte = (DTE)await _asyncPackage.GetServiceAsync(typeof(DTE));

            TeamFoundationServerExt foundationServerExt = dte.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt") as TeamFoundationServerExt;

            if (string.IsNullOrWhiteSpace(foundationServerExt?.ActiveProjectContext?.DomainUri))
            {
                throw new InvalidOperationException("The TFS is not online at the moment.");
            }

            TfsTeamProjectCollection projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(foundationServerExt.ActiveProjectContext.DomainUri));
            projectCollection.Connect(ConnectOptions.None);
            projectCollection.EnsureAuthenticated();

            var versionControlServer = projectCollection.GetService<VersionControlServer>();
            return versionControlServer;
        }

        private async Task<ITeamExplorer> GetTeamExplorerAsync()
        {
            var teamExplorer = await _asyncPackage.GetServiceAsync(typeof(ITeamExplorer));
            return (ITeamExplorer)teamExplorer;
        }

        private async Task<ITeamExplorerPage> GetPendingChangesPageAsync()
        {
            var teamExplorer = await GetTeamExplorerAsync();
            ITeamExplorerPage pendingChangesPage = teamExplorer.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
            return pendingChangesPage;
        }

        private static object GetPendingChangesPageModel(ITeamExplorerPage pendingChangesPage)
        {
            var model = ReflectionUtils.GetProperty("Model", pendingChangesPage);
            return model;
        }

        private static Workspace GetCurrentWorkspace(ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            var workspace = ReflectionUtils.GetProperty<Workspace>("Workspace", model);

            return workspace;
        }

        private static string GetMergeComment(List<string> sourceBranches, string targetBranch, Changeset changeset)
        {
            var builder = new StringBuilder();
            builder.Append("Merge ");

            for (int i = 0; i < sourceBranches.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                string sourceBranch = sourceBranches[i];
                var commonPrefix = StringUtils.FindCommonPrefix(sourceBranch, targetBranch);
                var sourceBranchShort = sourceBranch.Substring(commonPrefix.Length);
                var targetBranchShort = targetBranch.Substring(commonPrefix.Length);

                builder.Append($"{sourceBranchShort}->{targetBranchShort}");
            }

            builder.Append($", c{changeset.ChangesetId}");
            builder.Append($", {changeset.Comment}");

            return builder.ToString();
        }

        private static void SetMergeComment(string comment, ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            ReflectionUtils.SetProperty("CheckinComment", model, comment);
        }

        private static void AssociateWorkItem(int workItemId, ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);

            var enumType = ReflectionUtils.GetNestedType("WorkItemsAddSource", model.GetType().BaseType);
            var addByIdValue = Enum.Parse(enumType, "AddById");

            ReflectionUtils.InvokeMethod("AddWorkItemsByIdAsync", model, new int[] { workItemId }, addByIdValue);
        }
    }
}
