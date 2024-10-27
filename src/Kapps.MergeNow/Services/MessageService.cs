using System;
using System.Windows;

namespace MergeNow.Services
{
    public class MessageService : IMessageService
    {
        public void ShowMessage(string message) => ShowInternal(message, MessageBoxImage.Information);

        public void ShowWarning(string message) => ShowInternal(message, MessageBoxImage.Warning);

        public void ShowError(string message) => ShowInternal(message, MessageBoxImage.Error);

        public void ShowError(Exception exception) => ShowInternal(exception?.Message, MessageBoxImage.Error);

        private static void ShowInternal(string message, MessageBoxImage messageBoxImage)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            MessageBox.Show(message, "Merge Now", MessageBoxButton.OK, messageBoxImage);
        }
    }
}
