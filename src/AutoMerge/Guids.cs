// Guids.cs
// MUST match guids.h
using System;

namespace AutoMerge
{
	public static class GuidList
	{
		public const string guidAutoMergePkgString = "f05bac3e-6794-4a9e-9ee7-1b8a200778ee";
		public const string guidAutoMergeCmdSetString = "550e8690-9fae-46d1-8ff7-d6d0edf9449c";

		public static readonly Guid guidAutoMergeCmdSet = new Guid(guidAutoMergeCmdSetString);

		public const string AutoMergeNavigationItemId = "02A9D8B3-287B-4C55-83E7-7BFDB435546D";
	    public const string AutoMergeTeamNavigationItemId = "3439f733-0177-4db4-b12b-85f19a4ac78a";
        public const string AutoMergePageId = "3B582638-5F12-4715-8719-5E5777AB4581";
	    public const string AutoMergeTeamPageId = "246ccc66-d988-44d4-8d1e-e84ee846acd5";
        public const string RecentChangesetsSectionId = "8DA59790-3996-465E-A13F-27D64B3C2A9D";
	    public const string RecentChangesetsTeamSectionId = "b7dc6fbe-c3b1-47d8-805c-cce8c3dbedfb";

        public const string BranchesSectionId = "36BF6F52-F4AC-44A0-9985-817B2A65B3B0";
	};
}
