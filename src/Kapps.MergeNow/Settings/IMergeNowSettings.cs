namespace MergeNow.Settings
{
    /// <summary>
    /// Append merge comment to existing comment on Pending Changes view. Otherwise replace the comment.
    /// </summary>
    public interface IMergeNowSettings
    {
        bool AppendComment { get; }
    }
}
