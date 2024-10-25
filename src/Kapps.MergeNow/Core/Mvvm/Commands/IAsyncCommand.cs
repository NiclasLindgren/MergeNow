using System.Threading.Tasks;

namespace MergeNow.Core.Mvvm.Commands
{
    public interface IAsyncCommand : IBaseCommand
    {
        Task ExecuteAsync();

        bool CanExecute();
    }
}
