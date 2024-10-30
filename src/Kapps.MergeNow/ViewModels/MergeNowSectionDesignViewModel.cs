using MergeNow.Core.Mvvm.Commands;
using System.Collections.ObjectModel;

namespace MergeNow.ViewModels
{
    public class MergeNowSectionDesignViewModel : IMergeNowSectionViewModel
    {
        public IBaseCommand BrowseCommand { get; } = new EmptyCommand();
        public IBaseCommand FindCommand { get; } = new EmptyCommand();
        public IBaseCommand OpenChangesetCommand { get; } = new EmptyCommand();
        public IBaseCommand MergeCommand { get; } = new EmptyCommand();
        public IBaseCommand ClearPageCommand { get; } = new EmptyCommand();
        public IBaseCommand ClearMergeNowCommand { get; } = new EmptyCommand();

        public ObservableCollection<string> TargetBranches { get; } = new ObservableCollection<string>
        {
            "$/releases/r01",
            "$/releases/r02"
        };

        public bool IsSectionEnabled { get; set; } = true;
        public string ChangesetNumber { get; set; } = "123456";
        public string ChangesetName { get; set; } = "My changeset name";
        public string SelectedTargetBranch { get; set; } = "$/releases/r01";
        public bool AnyTargetBranches => true;
        public bool CombinedMerge { get; set; } = true;
        public bool IsAdvancedExpanded { get; set; } = true;
    }
}
