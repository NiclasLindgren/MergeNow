﻿using EnvDTE;
using MergeNow.Core.Utils;
using MergeNow.Model;
using MergeNow.Settings;
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
        private readonly IMessageService _messageService;
        private AsyncLazy<VersionControlServer> _versionControlConnectionTask;

        public MergeNowService(AsyncPackage asyncPackage, IMergeNowSettings settings, IMessageService messageService)
        {
            _asyncPackage = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            RenewVersionControlConnectection();
        }

        public async Task<bool> IsOnlineAsync()
        {
            var versionControlServer = await GetVersionControlAsync();
            return versionControlServer != null;
        }

        public async Task<Changeset> FindChangesetAsync(string changesetNumber)
        {
            if (!int.TryParse(changesetNumber, out int changesetNo))
            {
                throw new ArgumentException("Please provide a valid changeset number.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var changeset = versionControlServer?.GetChangeset(changesetNo);
            return changeset;
        }

        public async Task<Changeset> BrowseChangesetAsync()
        {
            var versionControlExt = await GetVersionControlExtAsync();
            var changeset = versionControlExt?.FindChangeset();
            return changeset;
        }

        public async Task ViewChangesetDetailsAsync(Changeset changeset)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            var versionControlExt = await GetVersionControlExtAsync();
            versionControlExt?.ViewChangesetDetails(changeset.ChangesetId);
        }

        public async Task<IEnumerable<string>> GetTargetBranchesAsync(Changeset changeset)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            var versionControlServer = await GetVersionControlAsync();
            var sourceBranches = GetSourceBranches(versionControlServer, changeset);

            var targetBranches = new List<string>();

            foreach (var sourceBranch in sourceBranches)
            {
                var branches = GetMergeBranches(versionControlServer, sourceBranch);
                targetBranches.AddRange(branches);
            }

            return targetBranches.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public async Task MergeAsync(Changeset changeset, string targetBranch, MergeHistory mergeHistory)
        {
            if (changeset == null)
            {
                throw new ArgumentException("Please provide a valid changeset.");
            }

            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new ArgumentException("Please provide a valid target branch.");
            }

            if (mergeHistory == null)
            {
                throw new ArgumentException("Please provide a merge history.");
            }

            var pendingChangesPage = await GetPendingChangesPageAsync();
            Workspace workspace = GetCurrentWorkspace(pendingChangesPage);

            if (workspace == null)
            {
                _messageService.ShowWarning("No TFS workspace found.");
                return;
            }

            var versionControlServer = await GetVersionControlAsync();
            var sourceBranches = GetSourceBranches(versionControlServer, changeset);

            if (sourceBranches == null || !sourceBranches.Any())
            {
                _messageService.ShowWarning("There are no source branches to merge.");
                return;
            }

            var mergeBranches = new List<string>();

            foreach (var sourceBranch in sourceBranches)
            {
                var branches = GetMergeBranches(versionControlServer, sourceBranch);
                if (branches.Any(b => string.Equals(b, targetBranch, StringComparison.OrdinalIgnoreCase)))
                {
                    mergeBranches.Add(sourceBranch);
                }
            }

            if (!mergeBranches.Any())
            {
                _messageService.ShowWarning("There are no target branches to merge.");
                return;
            }

            ChangesetVersionSpec changesetVersionSpec = new ChangesetVersionSpec(changeset.ChangesetId);

            GetStatus mergeStatus = null;

            for (int i = 0; i < mergeBranches.Count; i++)
            {
                var status = workspace.Merge(mergeBranches[i], targetBranch, changesetVersionSpec, changesetVersionSpec, LockLevel.None, RecursionType.Full, MergeOptionsEx.None);

                if (i == 0)
                {
                    mergeStatus = status;
                }
                else
                {
                    mergeStatus.Combine(status);
                }
            }

            if (mergeStatus == null || mergeStatus.NoActionNeeded)
            {
                _messageService.ShowMessage("There are no files to merge.");
                return;
            }

            var failures = mergeStatus.GetFailures();
            if (failures.Any())
            {
                _messageService.ShowMessage("TODO: Failed to merged.");
            }

            mergeHistory.Add(mergeBranches, targetBranch);
            var mergeComment = GetMergeComment(mergeHistory, changeset);
            SetComment(mergeComment, pendingChangesPage);

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

        private static List<string> GetSourceBranches(VersionControlServer versionControlServer, Changeset changeset)
        {
            if (versionControlServer == null || changeset == null)
            {
                return new List<string>();
            }

            var branchOwnerships = versionControlServer.QueryBranchObjectOwnership(new[] { changeset.ChangesetId });
            var sourceBranches = branchOwnerships?
                .Where(bo => !bo.RootItem.IsDeleted)
                .Select(bo => bo.RootItem.Item).ToList();

            return sourceBranches ?? new List<string>();
        }

        private static IEnumerable<string> GetMergeBranches(VersionControlServer versionControlServer, string sourceBranch)
        {
            var mergeBranches = versionControlServer?.QueryMergeRelationships(sourceBranch)
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.Item)
                    .Reverse() ?? Enumerable.Empty<string>();

            return mergeBranches;
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
                return await _versionControlConnectionTask.GetValueAsync();
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

            T tfsObject = dte?.GetObject(name) as T;
            return tfsObject;
        }

        private async Task ExecuteCommandAsync(string commandName, string commandArgs = "")
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_asyncPackage.DisposalToken);
            var dte = (DTE)await _asyncPackage.GetServiceAsync(typeof(DTE));

            dte?.ExecuteCommand(commandName, commandArgs);
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
            ITeamExplorerPage pendingChangesPage = teamExplorer?.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
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
            if (model == null)
            {
                return null;
            }

            var workspace = ReflectionUtils.GetProperty<Workspace>("Workspace", model);
            return workspace;
        }

        private string GetMergeComment(MergeHistory mergeHistory, Changeset changeset)
        {
            var mergeFromTo = new StringBuilder();

            int index = 0;
            foreach (var mergeHistoryItem in mergeHistory.Items)
            {
                string sourceBranch = mergeHistoryItem.Key;
                List<string> targetBranches = mergeHistoryItem.Value;

                var sourceBranchShort = string.Empty;
                var targetBranchesShort = new List<string>();

                for (int i = 0; i < targetBranches.Count; i++)
                {
                    string targetBranch = targetBranches[i];
                    var commonPrefix = StringUtils.FindCommonPrefix(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase);
                    commonPrefix = StringUtils.TakeTillLastChar(commonPrefix, '/');

                    var currentSourceBranchShort = sourceBranch.Substring(commonPrefix.Length);
                    var currentTargetBranchShort = targetBranch.Substring(commonPrefix.Length);

                    targetBranchesShort.Add(currentTargetBranchShort);

                    if (i == 0)
                    {
                        sourceBranchShort = currentSourceBranchShort;
                    }
                    else
                    {
                        sourceBranchShort = StringUtils.PickShortest(sourceBranchShort, currentSourceBranchShort);
                    }
                }

                if (index > 0)
                {
                    mergeFromTo.Append("; ");
                }

                mergeFromTo.Append(sourceBranchShort);
                mergeFromTo.Append("->");
                mergeFromTo.Append(string.Join(", ", targetBranchesShort));

                index++;
            }

            string mergeComment = _settings.CommentFormat;

            var replacements = new Dictionary<string, string>
            {
                { "MergeFromTo", mergeFromTo.ToString() },
                { "ChangesetNumber", changeset.ChangesetId.ToString() },
                { "ChangesetComment", changeset.Comment },
                { "ChangesetOwner", changeset.OwnerDisplayName }
            };

            foreach (var placeholder in replacements)
            {
                mergeComment = mergeComment.Replace($"{{{placeholder.Key}}}", placeholder.Value);
            }

            return mergeComment;
        }

        private static void SetComment(string comment, ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            if (model == null)
            {
                return;
            }

            ReflectionUtils.SetProperty("CheckinComment", model, comment);
        }

        private static void AssociateWorkItem(int workItemId, ITeamExplorerPage pendingChangesPage)
        {
            var model = GetPendingChangesPageModel(pendingChangesPage);
            if (model == null)
            {
                return;
            }

            var enumType = ReflectionUtils.GetNestedType("WorkItemsAddSource", model.GetType().BaseType);
            if (enumType == null)
            {
                return;
            }

            var addByIdValue = Enum.Parse(enumType, "AddById");
            if (addByIdValue == null)
            {
                return;
            }

            ReflectionUtils.InvokeMethod("AddWorkItemsByIdAsync", model, new int[] { workItemId }, addByIdValue);
        }
    }
}
