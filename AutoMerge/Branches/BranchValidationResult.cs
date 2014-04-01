namespace AutoMerge
{
	public enum BranchValidationResult
	{
		Undefined,

		Success,
		
		BranchNotMapped,

		ItemHasLocalChanges,

		AlreadyMerged,

		NoAccess
	}
}