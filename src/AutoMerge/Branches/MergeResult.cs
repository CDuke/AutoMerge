namespace AutoMerge
{
    public enum MergeResult
    {
        CheckIn,
        Merged,
        NothingMerge,
        CheckInFail,
        CheckInEvaluateFail,
        HasConflicts,
        CanNotGetLatest,
        HasLocalChanges,
        UnexpectedFileRestored,
    }
}
