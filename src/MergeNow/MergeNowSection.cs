using MergeNow.Views;
using Microsoft.TeamFoundation.Controls;
using System;
using System.ComponentModel;

namespace MergeNow
{
    [TeamExplorerSection(MergeNowSectionId, TeamExplorerPageIds.PendingChanges, MergeNowSectionSortOrder)]
    public class MergeNowSection : ITeamExplorerSection
    {
        public const string MergeNowSectionId = "0210c7cf-7c17-494b-a30b-836432a1bcfd";
        public const int MergeNowSectionSortOrder = 100;

        private readonly MergeNowSectionControl _sectionContent;
        private bool _isVisible = true;
        private bool _isExpanded = true;
        private bool _isBusy = false;

        public MergeNowSection()
        {
            _sectionContent = new MergeNowSectionControl();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title => "Merge Now";

        public object SectionContent => _sectionContent;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public void Initialize(object sender, SectionInitializeEventArgs e)
        {
        }

        public void Loaded(object sender, SectionLoadedEventArgs e)
        {
        }

        public void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
        }

        public void Refresh()
        {
        }

        public void Cancel()
        {
        }

        public object GetExtensibilityService(Type serviceType)
        {
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
