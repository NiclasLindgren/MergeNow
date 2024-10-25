using System;

namespace MergeNow.Core.Mvvm.Commands
{
    public abstract class BaseCommand
    {
        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
