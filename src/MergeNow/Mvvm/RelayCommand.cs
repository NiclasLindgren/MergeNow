using System;
using System.Windows.Input;

namespace MergeNow.Mvvm
{
    public class RelayCommand : ICommand
    {
        private readonly Action execute = null;
        private readonly Func<bool> canExecute = null;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute()
        {
            return canExecute?.Invoke() ?? true;
        }

        public void Execute()
        {
            if (!CanExecute())
                return;

            execute?.Invoke();
        }

        bool ICommand.CanExecute(object parameter) => CanExecute();
        void ICommand.Execute(object parameter) => Execute();

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
