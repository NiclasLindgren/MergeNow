using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel;

namespace MergeNow.Settings
{
    public class MergeNowSettings : DialogPage, IMergeNowSettings
    {
        private const string CollectionPath = "MergeNow";

        private const string DefaultCommentFormat = "Merge {MergeFromTo}, c{ChangesetNumber}, {ChangesetComment}";
        private const string DefaultMergeDelimeter = "->";

        private readonly ShellSettingsManager _settingsManager;

        [Category("General")]
        [DisplayName("Comment Format")]
        [Description("Specify a merge comment format. Available special tags: {MergeFromTo}, {ChangesetNumber}, {ChangesetComment}, {ChangesetOwner}.")]
        public string CommentFormat { get; set; }

        [Category("General")]
        [DisplayName("Merge Delimeter")]
        [Description("Delimeter used for {MergeFromTo} special tag in 'Comment Format' setting.")]
        public string MergeDelimeter { get; set; }

        public MergeNowSettings()
        {
            try
            {
                _settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            }
            catch (Exception ex)
            {
                Logger.Error("Cannot initialize Merge Now settings store.", ex);
            }

            ReloadAll();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            SaveAll();
            base.OnApply(e);
        }

        private void ReloadAll()
        {
            try
            {
                CommentFormat = GetSetting(nameof(CommentFormat), DefaultCommentFormat);
                MergeDelimeter = GetSetting(nameof(MergeDelimeter), DefaultMergeDelimeter);
            }
            catch (Exception ex)
            {
                Logger.Error("Cannot load Merge Now settings.", ex);
            }
        }

        private void SaveAll()
        {
            try
            {
                SaveSetting(nameof(CommentFormat), CommentFormat);
                SaveSetting(nameof(MergeDelimeter), MergeDelimeter);
            }
            catch (Exception ex)
            {
                Logger.Error("Cannot save Merge Now settings.", ex);
            }
        }

        private void SaveSetting<T>(string propertyName, T value)
        {
            if (_settingsManager == null)
            {
                return;
            }

            var writableStore = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!writableStore.CollectionExists(CollectionPath))
            {
                writableStore.CreateCollection(CollectionPath);
            }

            writableStore.SetString(CollectionPath, propertyName, value?.ToString());
        }

        private string GetSetting(string propertyName, string defaultValue = "")
        {
            if (_settingsManager == null)
            {
                return defaultValue;
            }

            var store = _settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            if (store.PropertyExists(CollectionPath, propertyName))
            {
                return store.GetString(CollectionPath, propertyName, defaultValue);
            }

            return defaultValue;
        }
    }
}
