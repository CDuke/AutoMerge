using System;
using System.Windows;

namespace AutoMerge
{
	public class MergeInfoModel
	{
		public bool Checked { get; set; }

		public string SourceBranch { get; set; }

		public string TargetBranch { get; set; }

		public BranchValidationResult ValidationResult { get; set; }

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

		public Visibility GetCheckBoxVisibility
		{
			get
			{
				if (IsCurrentBranch)
					return Visibility.Collapsed;

				if (!IsCurrentBranch && ValidationResult == BranchValidationResult.Success)
					return Visibility.Visible;

				return Visibility.Collapsed;
			}
		}

		public Visibility ImageVisibility
		{
			get
			{
				if (IsCurrentBranch)
					return Visibility.Collapsed;

				if (!IsCurrentBranch && ValidationResult != BranchValidationResult.Success)
					return Visibility.Visible;

				return Visibility.Collapsed;

			}
		}
	}
}