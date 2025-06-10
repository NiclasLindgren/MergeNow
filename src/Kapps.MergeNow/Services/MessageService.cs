
using MergeNow.ViewModels;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace MergeNow.Services
{
    public class MessageService : IMessageService
    {
        private readonly AsyncPackage _package;
        private MergeNowSectionViewModel _viewModel;

        public MessageService(AsyncPackage package)
        {
            _package = package;
        }

        public void SetViewModel(MergeNowSectionViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void ShowMessage(string message) => Show(message, OLEMSGICON.OLEMSGICON_INFO);

        public void ShowWarning(string message) => Show(message, OLEMSGICON.OLEMSGICON_WARNING);

        public void ShowError(string message)
        {
            Logger.Error(message);
            Show(message, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        public void ShowError(Exception exception)
        {
            Logger.Error(exception);
            Show(exception?.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        private void Show(string message, OLEMSGICON icon)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (icon == OLEMSGICON.OLEMSGICON_INFO)
            {
                _viewModel.Message = message;
            }
            else
            {
                VsShellUtilities.ShowMessageBox(_package, message, null, icon,
                OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
