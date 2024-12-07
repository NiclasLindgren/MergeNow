using MergeNow.ViewModels;
using System;
using System.Windows;

namespace MergeNow.Services
{
    public class MessageService : IMessageService
    {
        private MergeNowSectionViewModel _viewModel;
        public MessageService() 
        {
        }

        public void SetViewModel(MergeNowSectionViewModel viewModel)
        {
            _viewModel = viewModel;
        }
        public void ShowMessage(string message) => Show(message, MessageBoxImage.Information);

        public void ShowWarning(string message) => Show(message, MessageBoxImage.Warning);

        public void ShowError(string message)
        {
            Logger.Error(message);
            Show(message, MessageBoxImage.Error);
        }

        public void ShowError(Exception exception)
        {
            Logger.Error(exception);
            Show(exception?.Message, MessageBoxImage.Error);
        }

        private void Show(string message, MessageBoxImage messageBoxImage)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (messageBoxImage == MessageBoxImage.Information)
            {
                _viewModel.Message = message;
            }
            else
            {
                MessageBox.Show(message, "Merge Now", MessageBoxButton.OK, messageBoxImage);
            }
        }
    }
}
