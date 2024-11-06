using MergeNow.Settings;

namespace MergeNow
{
    public class MergeNowSectionMemento
    {
        public MergeNowSectionMemento()
        {
            IsExpanded = true;
        }

        public MergeNowSectionMemento(IMergeNowSettings settings) : this()
        {
            if (settings == null)
            {
                return;
            }

            IsExpanded = settings.StartExpanded;
        }

        public bool IsExpanded { get; set; }
    }
}
