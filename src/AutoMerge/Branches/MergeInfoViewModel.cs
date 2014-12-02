using System;
using System.Collections.Generic;
using AutoMerge.Events;
using AutoMerge.Prism.Events;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class MergeInfoViewModel
	{
		private readonly IEventAggregator _eventAggregator;
		internal bool _checked;

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
				_eventAggregator.GetEvent<BranchSelectedChangedEvent>().Publish(this);
			}
		}

		public string SourcePath { get; set; }

		public string TargetPath { get; set; }

		public string SourceBranch { get; set; }

		public string TargetBranch { get; set; }

		public ChangesetVersionSpec ChangesetVersionSpec { get; set; }


		public string DisplayBranchName
		{
			get
			{
				return BranchHelper.GetShortBranchName(TargetBranch);
			}
		}

		public BranchValidationResult ValidationResult { get; set; }

		public string ValidationMessage { get; set; }

		public bool IsSourceBranch
		{
			get
			{
				return string.Equals(SourceBranch, TargetBranch, StringComparison.OrdinalIgnoreCase);
			}
		}
	}
}
