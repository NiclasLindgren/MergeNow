using System.Threading.Tasks;

namespace MergeNow.Mvvm.Commands
{
    public interface IAsyncCommand : IBaseCommand
    {
        Task ExecuteAsync();

        bool CanExecute();
    }
}
