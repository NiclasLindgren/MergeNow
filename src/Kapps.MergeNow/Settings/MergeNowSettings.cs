using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
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
            _settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);

            AppendComment = GetBoolSetting(nameof(AppendComment), true);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            SaveSetting(nameof(AppendComment), AppendComment);

            base.OnApply(e);
        }

        private void SaveSetting<T>(string propertyName, T value)
        {
            var writableStore = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!writableStore.CollectionExists(CollectionPath))
            {
                writableStore.CreateCollection(CollectionPath);
            }

            writableStore.SetString(CollectionPath, propertyName, value?.ToString());
        }

        private string GetSetting(string propertyName, string defaultValue = "")
        {
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
