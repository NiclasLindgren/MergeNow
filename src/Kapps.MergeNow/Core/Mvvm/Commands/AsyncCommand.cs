using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MergeNow.Core.Mvvm.Commands
{
    public class AsyncCommand : BaseCommand, IAsyncCommand
    {
        private bool isExecuting;
        private readonly Func<Task> execute;
        private readonly Func<bool> canExecute;
        private readonly Action<Exception> errorHandler;

        public AsyncCommand(Action<Exception> errorHandler, Func<Task> execute, Func<bool> canExecute = null)
        {
            this.errorHandler = errorHandler;
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute()
        {
            return !isExecuting && (canExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    isExecuting = true;
                    await (execute?.Invoke() ?? Task.CompletedTask);
                }
                finally
                {
                    isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            FireAsyncCatchErrors(ExecuteAsync(), errorHandler);
        }

        private static void FireAsyncCatchErrors(Task task, Action<Exception> errorHandler)
        {
            try
            {
                task.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }
    }
}
