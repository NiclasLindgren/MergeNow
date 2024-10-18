using EnvDTE;
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MergeNow.Services
{
    internal class MergeNowService : IMergeNowService
    {
        private readonly AsyncPackage _asyncPackage;
        private AsyncLazy<VersionControlServer> _versionControlTask;

        public MergeNowService(AsyncPackage asyncPackage)
        {
            _asyncPackage = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            CreateVersionControlTask();
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

            foreach (BranchObjectOwnership branchOwnership in branchOwnerships)
            {
                var branches = versionControlServer.QueryMergeRelationships(branchOwnership.RootItem.Item)
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

            var branchOwnerships = versionControlServer.QueryBranchObjectOwnership(new[] { changesetNumber });

            Workspace workspace = await GetCurrentWorkspaceAsync();

            foreach (BranchObjectOwnership branchOwnership in branchOwnerships)
            {
                var sourcBranch = branchOwnership.RootItem.Item;
                ChangesetVersionSpec changesetVersionSpec = new ChangesetVersionSpec(changeset);
                workspace.Merge(sourcBranch, targetBranch, changesetVersionSpec, changesetVersionSpec, LockLevel.None, RecursionType.Full, MergeOptionsEx.None);
            }

            var teamExplorer = (ITeamExplorer)await _asyncPackage.GetServiceAsync(typeof(ITeamExplorer));
            var page = teamExplorer.NavigateToPage(new Guid("FD273AA7-0538-474B-954F-2327F91CEF5E"), null);
            var pendingChangesExt = page.GetExtensibilityService(typeof(PendingChangesExt)) as PendingChangesExt;
        }

        private async Task<VersionControlServer> GetVersionControlAsync()
        {
            try
            {
                return await _versionControlTask.GetValueAsync();
            }
            catch
            {
                CreateVersionControlTask();
                throw;
            }
        }

        private void CreateVersionControlTask()
        {
            _versionControlTask = new AsyncLazy<VersionControlServer>(GetVersionControlServerAsync, ThreadHelper.JoinableTaskFactory);
        }

        private async Task<VersionControlServer> GetVersionControlServerAsync()
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

        [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "No other way to get workspace")]
        private async Task<Workspace> GetCurrentWorkspaceAsync()
        {
            var pendingChangesPage = await GetPendingChangesPageAsync();

            var modelProperty = pendingChangesPage.GetType().GetProperty("Model", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var model = modelProperty.GetValue(pendingChangesPage);

            var workspaceProperty = model.GetType().GetProperty("Workspace", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var workspace = workspaceProperty.GetValue(model) as Workspace;

            return workspace;
        }
    }
}
