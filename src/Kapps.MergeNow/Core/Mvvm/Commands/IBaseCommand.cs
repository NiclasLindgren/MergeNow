using System.Windows.Input;

namespace MergeNow.Core.Mvvm.Commands
{
    public interface IBaseCommand: ICommand
    {
        void RaiseCanExecuteChanged();
    }
}
