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

        ObservableCollection<string> TargetBranches { get; }

        bool IsOnline { get; set; }
        string ChangesetNumber { get; set; }
        string ChangesetName { get; set; }
        string SelectedTargetBranch { get; set; }
        bool AnyTargetBranches { get; }
    }
}
