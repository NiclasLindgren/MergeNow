using System.Collections.Generic;

namespace MergeNow.Services
{
    public interface IMergeNowService
    {
        IEnumerable<string> GetTargetBranches(string changeset);

        void Merge(string changeset, string targetBranch);
    }
}
