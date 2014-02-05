using System;
using System.Windows;
using AutoMerge.Events;
using Microsoft.Practices.Prism.Events;

namespace AutoMerge
{
	public class MergeInfoViewModel
	{
		private readonly IEventAggregator _eventAggregator;
		private bool _checked;

		public MergeInfoViewModel(IEventAggregator eventAggregator)
		{
			_eventAggregator = eventAggregator;
		}

		public bool Checked
		{
			get
			{
				return _checked;
			}
			set
			{
				_checked = value;
				_eventAggregator.GetEvent<BranchSelectedChanged>().Publish(this);
			}
		}

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