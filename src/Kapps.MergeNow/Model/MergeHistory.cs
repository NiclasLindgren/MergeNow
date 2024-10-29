using System.Collections;
using System.Collections.Generic;

namespace MergeNow.Model
{
    public class MergeHistory: IEnumerable<KeyValuePair<string, List<string>>>
    {
        private Dictionary<string, List<string>> Items { get; } = new Dictionary<string, List<string>>();

        public void Add(IEnumerable<string> sourceBranches, string targetBranch)
        {
            if (sourceBranches == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                return;
            }

            foreach (string sourceBranch in sourceBranches)
            {
                if (sourceBranch == null)
                {
                    continue;
                }

                var key = sourceBranch.ToLower();

                if (!Items.ContainsKey(key))
                {
                    Items[key] = new List<string>();
                }

                var value = targetBranch.ToLower();

                if (!Items[key].Contains(value))
                {
                    Items[key].Add(value);
                }
            }
        }

        public void Clear()
        {
            Items.Clear();
        }

        public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}
