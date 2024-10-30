using MergeNow.Core.Mvvm.Commands;
using System.Collections.ObjectModel;

namespace MergeNow.ViewModels
{
    public interface IMergeNowSectionViewModel
    {
        IBaseCommand BrowseCommand { get; }
        IBaseCommand FindCommand { get; }
        IBaseCommand OpenChangesetCommand { get; }
        IBaseCommand MergeCommand { get; }
        IBaseCommand ClearPageCommand { get; }
        IBaseCommand ClearMergeNowCommand { get; }
        ObservableCollection<string> TargetBranches { get; }

        bool IsOnline { get; set; }
        string ChangesetNumber { get; set; }
        string ChangesetName { get; set; }
        string SelectedTargetBranch { get; set; }
        bool AnyTargetBranches { get; }
        bool CombinedMerge { get; set; }
        bool IsAdvancedExpanded { get; set; }
    }
}
