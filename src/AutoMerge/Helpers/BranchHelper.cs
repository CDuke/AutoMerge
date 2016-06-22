using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMerge.Configuration;

namespace AutoMerge
{
	internal static class BranchHelper
	{
        public static string GetShortBranchName(string branchFullName, BranchNameMatch[] aliases)
        {

            foreach (var branchNameMatch in (aliases ?? new BranchNameMatch[0])
                .Concat(new[] { new BranchNameMatch { match = "/([^/]+)$", alias = "$1" } })) // default match
            {
                var regex = new Regex(branchNameMatch.match);
                var match = regex.Match(branchFullName);
                if (match.Success)
                {
                    return match.Result(branchNameMatch.alias); //return first match
                }
            }

            return branchFullName; // return full name if nothing matched
        }

        public static string GetDisplayBranchName(List<string> branches, BranchNameMatch[] aliases)
		{
			if (branches == null || branches.Count == 0)
				return string.Empty;

			if (branches.Count == 1)
			{
				return GetShortBranchName(branches[0], aliases);
			}
			return "multi";
		}
	}
}
