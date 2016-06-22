using System;
using AutoMerge.Configuration;
using AutoMerge.Events;
using AutoMerge.Prism.Events;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class MergeInfoViewModel
	{
		private readonly IEventAggregator _eventAggregator;
		private readonly BranchNameMatch[] _aliases;
		internal bool _checked;

		public MergeInfoViewModel(IEventAggregator eventAggregator, BranchNameMatch[] aliases)
		{
			_eventAggregator = eventAggregator;
			_aliases = aliases;
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
                return BranchHelper.GetShortBranchName(TargetBranch, _aliases);
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

        public Configuration.BranchNameMatch[] Aliases { get; set; }
    }
}
