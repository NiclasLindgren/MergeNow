using System.Collections.Generic;

namespace MergeNow.Services
{
    internal class MergeNowService : IMergeNowService
    {
        public IEnumerable<string> GetTargetBranches(string changeset)
        {
            return new[] { "Branch1", "Branch2" };
        }

        public void Merge(string changeset, string targetBranch)
        {
        }
    }
}
