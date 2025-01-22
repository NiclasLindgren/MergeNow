namespace MergeNow.Settings
{
    public interface IMergeNowSettings
    {
        /// <summary>
        /// Specify a merge comment format.
        /// e.g. Merge {MergeFromTo}, c{ChangesetNumber}, {ChangesetComment}
        /// </summary>
        string CommentFormat { get; }

        /// <summary>
        /// Delimeter used for {MergeFromTo} special tag in 'Comment Format' setting.
        /// </summary>
        string MergeDelimeter { get; }

        /// <summary>
        /// Expand Merge Now section when Pending Changes page is opened for the first time.
        /// </summary>
        bool StartExpanded { get; }
    }
}
