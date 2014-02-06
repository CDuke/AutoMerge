namespace AutoMerge
{
	internal static class BranchHelper
	{
		public static string GetShortBranchName(string branchFullName)
		{
			var pos = branchFullName.LastIndexOf('/');
			var name = branchFullName.Substring(pos + 1);
			return name;
		}
	}
}