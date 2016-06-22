using System.Collections.Generic;
using AutoMerge.Prism.Events;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class BranchFactory
	{
		private readonly string _sourceBranch;
		private readonly string _sourceFolder;
		private readonly ChangesetVersionSpec _changesetVersion;
		private readonly BranchValidator _branchValidator;
		private readonly IEventAggregator _eventAggregator;
        private readonly Settings _settings;

	    public BranchFactory(string sourceBranch,
			string sourceFolder,
			ChangesetVersionSpec changesetVersion,
			BranchValidator branchValidator,
			IEventAggregator eventAggregator,
            Settings settings)
		{
			_sourceBranch = sourceBranch;
			_sourceFolder = sourceFolder;
			_changesetVersion = changesetVersion;
			_branchValidator = branchValidator;
			_eventAggregator = eventAggregator;
	        _settings = settings;
		}

		public MergeInfoViewModel CreateTargetBranchInfo(ItemIdentifier targetBranch, ItemIdentifier targetPath)
		{
			return CreateBranch(targetBranch.Item, targetPath.Item);
		}

		public MergeInfoViewModel CreateSourceBranch()
		{
			return CreateBranch(_sourceBranch, _sourceFolder);
		}

		private MergeInfoViewModel CreateBranch(string targetBranch, string targetPath)
		{
			var mergeInfo = new MergeInfoViewModel(_eventAggregator, _settings.BranchNameMatches)
			{
				SourceBranch = _sourceBranch,
				TargetBranch = targetBranch,
				SourcePath = _sourceFolder,
				TargetPath = targetPath,
				ChangesetVersionSpec = _changesetVersion,
				ValidationResult = BranchValidationResult.Success,
                Aliases = _settings.BranchNameMatches
			};

			if (_sourceBranch != targetBranch)
			{
				mergeInfo = _branchValidator.Validate(mergeInfo);
			}

			return mergeInfo;
		}
	}
}
