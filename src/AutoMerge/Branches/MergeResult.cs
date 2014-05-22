namespace AutoMerge
{
	public enum MergeResult
	{
		Success,
		NothingMerge,
		CheckInFail,
		CheckInEvaluateFail,
		UnresolvedConflicts,
		NotCheckIn,
		CanNotGetLatest
	}
}