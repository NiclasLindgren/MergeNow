using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MergeNow.Services
{
    internal class MergeNowService : IMergeNowService
    {
        private readonly AsyncPackage _asyncPackage;

        public MergeNowService(AsyncPackage pickAndMergePackage)
        {
            _asyncPackage = pickAndMergePackage;
        }

        public async Task<IEnumerable<string>> GetTargetBranchesAsync(string changeset)
        {
            if (!int.TryParse(changeset, out int changesetNumber))
            {
                throw new ArgumentException("Invalid Changeset number.", nameof(changeset));
            }

            await Task.CompletedTask;

            return new[] { "Branch1", "Branch2" };
        }

        public Task MergeAsync(string changeset, string targetBranch)
        {
            throw new NotImplementedException("MergeAsync not yet implemented.");
        }
    }
}
