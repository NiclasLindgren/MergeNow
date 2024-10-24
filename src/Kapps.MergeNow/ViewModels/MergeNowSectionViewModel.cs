using MergeNow.Mvvm;
using MergeNow.Mvvm.Commands;
using MergeNow.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MergeNow.ViewModels
{
    public class MergeNowSectionViewModel : BaseViewModel
    {
        private readonly IMergeNowService _mergeNowService;

        public MergeNowSectionViewModel(IMergeNowService mergeNowService)
        {
            _mergeNowService = mergeNowService;

            FindCommand = new AsyncCommand(ShowError, FindChangesetAsync, CanFindChangeset);
            LinkToViewModel(FindCommand);

            BrowseCommand = new AsyncCommand(ShowError, BrowseChangesetAsync);
            LinkToViewModel(BrowseCommand);

            MergeCommand = new AsyncCommand(ShowError, MergeChangesetAsync, CanMergeChangeset);
            LinkToViewModel(MergeCommand);

            OpenChangesetCommand = new AsyncCommand(ShowError, OpenChangesetAsync, CanOpenChangeset);
            LinkToViewModel(OpenChangesetCommand);

            TargetBranches = new ObservableCollection<string>();
            LinkToViewModel(TargetBranches);

            ChangesetNumber = string.Empty;
        }

        private Changeset SelectedChangeset { get;set; }

        public AsyncCommand FindCommand { get; }
        public AsyncCommand BrowseCommand { get; }
        public AsyncCommand MergeCommand { get; }
        public AsyncCommand OpenChangesetCommand { get; }

        private string _changesetNumber;
        public string ChangesetNumber
        {
            get => _changesetNumber;
            set
            {
                SetValue(ref _changesetNumber, value);
                ResetView();
            }
        }

        private string _changesetName;
        public string ChangesetName
        {
            get => _changesetName;
            set => SetValue(ref _changesetName, value);
        }

        public ObservableCollection<string> TargetBranches { get; }

        private string _selectedTargetBranch;
        public string SelectedTargetBranch
        {
            get => _selectedTargetBranch;
            set => SetValue(ref _selectedTargetBranch, value);
        }

        public bool AnyTargetBranches => TargetBranches.Any();

        private async Task FindChangesetAsync()
        {
            var changeset = await _mergeNowService.FindChangesetAsync(ChangesetNumber);

            if (changeset == null)
            {
                throw new InvalidOperationException($"Changeset '{ChangesetNumber}' does not exist.");
            }

            await ApplyChangesetAsync(changeset);
        }

        private async Task BrowseChangesetAsync()
        {
            var changeset = await _mergeNowService.BrowseChangesetAsync();

            // Browse was cancelled
            if (changeset == null)
            {
                return;
            }

            await ApplyChangesetAsync(changeset);
        }

        private async Task ApplyChangesetAsync(Changeset changeset)
        {
            ChangesetNumber = changeset.ChangesetId.ToString();
            ChangesetName = changeset.Comment;
            SelectedChangeset = changeset;

            var branches = await _mergeNowService.GetTargetBranchesAsync(SelectedChangeset);
            branches?.ToList().ForEach(branch => TargetBranches.Add(branch));
        }

        private void ResetView()
        {
            SelectedChangeset = null;
            ChangesetName = string.Empty;

            SelectedTargetBranch = string.Empty;
            TargetBranches.Clear();
        }

        private bool CanFindChangeset()
        {
            return !string.IsNullOrWhiteSpace(ChangesetNumber) &&
                int.TryParse(ChangesetNumber, out var number) &&
                number > 0;
        }

        private Task MergeChangesetAsync()
        {
            return _mergeNowService.MergeAsync(SelectedChangeset, SelectedTargetBranch);
        }

        private bool CanMergeChangeset()
        {
            return !string.IsNullOrWhiteSpace(SelectedTargetBranch);
        }

        private Task OpenChangesetAsync()
        {
            return _mergeNowService.ViewChangesetDetailsAsync(SelectedChangeset);
        }

        private bool CanOpenChangeset()
        {
            return !string.IsNullOrWhiteSpace(ChangesetName);
        }

        private static void ShowError(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            MessageBox.Show(exception.Message, "Merge Now", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}
