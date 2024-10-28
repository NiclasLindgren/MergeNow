using Microsoft.TeamFoundation.VersionControl.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MergeNow.Services
{
    public interface IMergeNowService
    {
        Task<bool> IsOnlineAsync();

        Task<Changeset> FindChangesetAsync(string changesetNumber);

        Task<Changeset> BrowseChangesetAsync();

        Task ViewChangesetDetailsAsync(Changeset changeset);

        Task<IEnumerable<string>> GetTargetBranchesAsync(Changeset changeset);

        Task MergeAsync(Changeset changeset, string targetBranch);
    }
}
