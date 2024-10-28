namespace MergeNow.Settings
{
    public interface IMergeNowSettings
    {
        /// <summary>
        /// Specify a merge comment format.
        /// e.g. Merge {SourceBranchesShort}->{TargetBranchShort}, c{ChangesetNumber}, {ChangesetComment}
        /// </summary>
        string CommentFormat { get; }
    }
}
