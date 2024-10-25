using EnvDTE;
using MergeNow.Settings;
using MergeNow.Core.Utils;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
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
        private readonly IMergeNowSettings _settings;
        private AsyncLazy<VersionControlServer> _versionControlConnectionTask;

        public MergeNowService(AsyncPackage asyncPackage, IMergeNowSettings settings)
        {
            _settings = settings;
            _asyncPackage = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            RenewVersionControlConnectection();
        }

        public async Task<Changeset> FindChangesetAsync(string changesetNumber)
        {
            if (!int.TryParse(changesetNumber, out int changesetNo))
            {
                throw new ArgumentException("Please provide a valid changeset number.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var changeset = versionControlServer.GetChangeset(changesetNo);
            return changeset;
        }

        public async Task<Changeset> BrowseChangesetAsync()
        {
            var versionControlExt = await GetVersionControlExtAsync();
            var changeset = versionControlExt.FindChangeset();
            return changeset;
        }

        public async Task ViewChangesetDetailsAsync(Changeset changeset)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            var versionControlExt = await GetVersionControlExtAsync();
            versionControlExt.ViewChangesetDetails(changeset.ChangesetId);
        }

        public async Task<IEnumerable<string>> GetTargetBranchesAsync(Changeset changeset)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var branchOwnerships = versionControlServer.QueryBranchObjectOwnership(new[] { changeset.ChangesetId });
            var sourceBranches = branchOwnerships.Select(bo => bo.RootItem.Item).ToList();

            var targetBranches = new List<string>();

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

        public async Task MergeAsync(Changeset changeset, string targetBranch)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new ArgumentException("Please provide a valid target branch.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var branchOwnerships = versionControlServer.QueryBranchObjectOwnership(new[] { changeset.ChangesetId });
            var sourceBranches = branchOwnerships.Select(bo => bo.RootItem.Item).ToList();

            var pendingChangesPage = await GetPendingChangesPageAsync();
            Workspace workspace = GetCurrentWorkspace(pendingChangesPage);
            ChangesetVersionSpec changesetVersionSpec = new ChangesetVersionSpec(changeset.ChangesetId);

            GetStatus mergeStatus = null;

            for (int i = 0; i < sourceBranches.Count; i++)
            {
                var status = workspace.Merge(sourceBranches[i], targetBranch, changesetVersionSpec, changesetVersionSpec, LockLevel.None, RecursionType.Full, MergeOptionsEx.None);

                if (i == 0)
                {
                    mergeStatus = status;
                }
                else
                {
                    mergeStatus.Combine(status);
                }
            }

            if (mergeStatus == null || mergeStatus.NumFiles == 0)
            {
                throw new Exception("There are no files to merge.");
            }

            var mergeComment = GetMergeComment(sourceBranches, targetBranch, changeset);
            SetMergeComment(mergeComment, pendingChangesPage);

            foreach (var workItem in changeset.WorkItems)
            {
                AssociateWorkItem(workItem.Id, pendingChangesPage);
            }

            if (workspace.QueryConflicts(new string[] { targetBranch }, true).Any())
            {
                await OpenResolveConfiltsPageAsync();
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
            var foundationServerExt = await GetTfsObjectAsync<TeamFoundationServerExt>("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt");

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

        private async Task<T> GetTfsObjectAsync<T>(string name) where T : class
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var dte = (DTE)await _asyncPackage.GetServiceAsync(typeof(DTE));

            T tfsObject = dte.GetObject(name) as T;
            return tfsObject;
        }

        private async Task ExecuteCommandAsync(string commandName, string commandArgs = "")
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var dte = (DTE)await _asyncPackage.GetServiceAsync(typeof(DTE));

            dte.ExecuteCommand(commandName, commandArgs);
        }

        private async Task<VersionControlExt> GetVersionControlExtAsync()
        {
            var versionControlExt = await GetTfsObjectAsync<VersionControlExt>("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
            return versionControlExt ?? throw new InvalidOperationException("VersionControlExt not available.");
        }

        private Task OpenResolveConfiltsPageAsync()
        {
            return ExecuteCommandAsync("TeamFoundationContextMenus.PendingChangesPageMoreLink.TfsContextPendingChangesResolveConflicts");
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

        private void SetMergeComment(string comment, ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);

            if (_settings.AppendComment)
            {
                var existingComment = ReflectionUtils.GetProperty("CheckinComment", model).ToString();

                if (!string.IsNullOrWhiteSpace(existingComment))
                {
                    comment = $"{existingComment}, {comment}";
                }
            }

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
