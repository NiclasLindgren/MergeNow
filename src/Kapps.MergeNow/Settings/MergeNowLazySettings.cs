using Microsoft.VisualStudio.Shell;
using System;

namespace MergeNow.Settings
{
    public class MergeNowLazySettings : IMergeNowSettings
    {
        private readonly AsyncPackage _package;
        private readonly Lazy<IMergeNowSettings> _lazySettings;

        public MergeNowLazySettings(AsyncPackage package)
        {
            _package = package;
            _lazySettings = new Lazy<IMergeNowSettings>(() => (MergeNowSettings)_package.GetDialogPage(typeof(MergeNowSettings)));
        }

        public string CommentFormat => _lazySettings.Value.CommentFormat;
        public string MergeDelimeter => _lazySettings.Value.MergeDelimeter;
        public bool StartExpanded => _lazySettings.Value.StartExpanded;
    }
}
