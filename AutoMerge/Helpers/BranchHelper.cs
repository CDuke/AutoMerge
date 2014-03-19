using System.Collections.Generic;

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

		public static string GetDisplayBranchName(List<string> branches)
		{
			if (branches == null || branches.Count == 0)
				return string.Empty;

			if (branches.Count == 1)
			{
				return GetShortBranchName(branches[0]);
			}
			return "multi";
		}
	}
}