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

        private readonly ShellSettingsManager _settingsManager;

        [Category("General")]
        [DisplayName("Append Comment")]
        [Description("Append merge comment to existing comment on Pending Changes view. Otherwise replace the comment.")]
        public bool AppendComment { get; set; }

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
                AppendComment = GetBoolSetting(nameof(AppendComment), true);
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
                SaveSetting(nameof(AppendComment), AppendComment);
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

        private bool GetBoolSetting(string propertyName, bool defaultValue = false)
        {
            var setting = GetSetting(propertyName);

            return !string.IsNullOrWhiteSpace(setting) ? setting.ToUpper() == "TRUE" : defaultValue;
        }
    }
}
