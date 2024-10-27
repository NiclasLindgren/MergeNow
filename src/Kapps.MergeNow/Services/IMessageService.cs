using System;

namespace MergeNow.Services
{
    public interface IMessageService
    {
        void ShowMessage(string message);

        void ShowWarning(string message);

        void ShowError(string message);

        void ShowError(Exception exception);
    }
}
