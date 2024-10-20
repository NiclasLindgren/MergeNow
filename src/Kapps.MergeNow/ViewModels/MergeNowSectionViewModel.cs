using MergeNow.Mvvm;
using MergeNow.Mvvm.Commands;
using MergeNow.Services;
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

            MergeCommand = new AsyncCommand(ShowError, MergeChangesetAsync, CanMergeChangeset);
            LinkToViewModel(MergeCommand);

            TargetBranches = new ObservableCollection<string>();
            LinkToViewModel(TargetBranches);

            Changeset = string.Empty;
        }

        public AsyncCommand FindCommand { get; }
        public AsyncCommand MergeCommand { get; }

        private string _changeset;
        public string Changeset
        {
            get => _changeset;
            set
            {
                SetValue(ref _changeset, value);

                TargetBranches.Clear();
                SelectedTargetBranch = null;
            }
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
            SelectedTargetBranch = null;
            TargetBranches.Clear();

            var branches = await _mergeNowService.GetTargetBranchesAsync(Changeset);
            branches?.ToList().ForEach(branch => TargetBranches.Add(branch));
        }

        private bool CanFindChangeset()
        {
            return !string.IsNullOrWhiteSpace(Changeset) &&
                int.TryParse(Changeset, out var number) &&
                number > 0;
        }

        private Task MergeChangesetAsync()
        {
            return _mergeNowService.MergeAsync(Changeset, SelectedTargetBranch);
        }

        private bool CanMergeChangeset()
        {
            return !string.IsNullOrWhiteSpace(SelectedTargetBranch);
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
