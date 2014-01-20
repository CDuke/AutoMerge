using System;

namespace AutoMerge
{
	public class MergeInfoModel
	{
		public bool Checked { get; set; }

		public string SourceBranch { get; set; }

		public string TargetBranch { get; set; }

		public bool CanMerge
		{
			get
			{
				return !string.Equals(SourceBranch, TargetBranch, StringComparison.OrdinalIgnoreCase);
			}
		}

		public bool IsCurrentBranch
		{
			get
			{
				return string.Equals(SourceBranch, TargetBranch, StringComparison.OrdinalIgnoreCase);
			}
		}
	}
}