namespace MergeNow.Core.Mvvm.Commands
{
    public class EmptyCommand : BaseCommand, IBaseCommand
    {
        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
        }
    }
}
