using System.Windows.Input;

namespace MergeNow.Mvvm.Commands
{
    public interface IBaseCommand: ICommand
    {
        void RaiseCanExecuteChanged();
    }
}
