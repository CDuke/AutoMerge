// Guids.cs
// MUST match guids.h
using System;

namespace AutoMerge
{
	public static class GuidList
	{
		public const string guidAutoMergePkgString = "f05bac3e-6794-4a9e-9ee7-1b8a200778ee";
		public const string guidAutoMergeCmdSetString = "550e8690-9fae-46d1-8ff7-d6d0edf9449c";

		public static readonly Guid ShowAutoMergeCmdSet = new Guid(guidAutoMergeCmdSetString);

		public const string AutoMergeNavigationItemId = "02A9D8B3-287B-4C55-83E7-7BFDB435546D";
		public const string AutoMergePageId = "3B582638-5F12-4715-8719-5E5777AB4581";
	    public static readonly Guid AutoMergePageGuid = new Guid(AutoMergePageId);
		public const string RecentChangesetsSectionId = "8DA59790-3996-465E-A13F-27D64B3C2A9D";

		public const string BranchesSectionId = "36BF6F52-F4AC-44A0-9985-817B2A65B3B0";
	};
}
