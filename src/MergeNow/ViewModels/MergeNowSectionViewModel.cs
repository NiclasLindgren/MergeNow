using MergeNow.Mvvm;
using MergeNow.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MergeNow.ViewModels
{
    public class MergeNowSectionViewModel : BaseViewModel
    {
        private readonly IMergeNowService _mergeNowService;

        public MergeNowSectionViewModel(IMergeNowService mergeNowService)
        {
            _mergeNowService = mergeNowService;

            FindCommand = new RelayCommand(FindChangeset, CanFindChangeset);
            LinkToViewModel(FindCommand);

            MergeCommand = new RelayCommand(MergeChangeset, CanMergeChangeset);
            LinkToViewModel(MergeCommand);

            TargetBranches = new ObservableCollection<string>();
            LinkToViewModel(TargetBranches);

            Changeset = string.Empty;
        }

        public RelayCommand FindCommand { get; }
        public RelayCommand MergeCommand { get; }

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

        private void FindChangeset()
        {
            SelectedTargetBranch = null;
            TargetBranches.Clear();

            var branches = _mergeNowService.GetTargetBranches(Changeset);
            branches?.ToList().ForEach(branch => TargetBranches.Add(branch));
        }

        private bool CanFindChangeset()
        {
            return !string.IsNullOrWhiteSpace(Changeset) &&
                int.TryParse(Changeset, out var number) &&
                number > 0;
        }

        private void MergeChangeset()
        {
            _mergeNowService.Merge(Changeset, SelectedTargetBranch);
        }

        private bool CanMergeChangeset()
        {
            return !string.IsNullOrWhiteSpace(SelectedTargetBranch);
        }
    }
}
