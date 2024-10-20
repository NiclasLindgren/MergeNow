using System.Collections.Generic;
using System.Threading.Tasks;

namespace MergeNow.Services
{
    public interface IMergeNowService
    {
        Task<IEnumerable<string>> GetTargetBranchesAsync(string changeset);

        Task MergeAsync(string changeset, string targetBranch);
    }
}
