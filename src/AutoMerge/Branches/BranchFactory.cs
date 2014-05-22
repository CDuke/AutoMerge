using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class BranchFactory
	{
		private readonly string _sourceBranch;
		private readonly string _sourceFolder;
		private readonly ChangesetVersionSpec _changesetVersion;
		private readonly TrackMergeInfo _trackMergeInfo;
		private readonly BranchValidator _branchValidator;
		private readonly IEventAggregator _eventAggregator;

		public BranchFactory(string sourceBranch,
			string sourceFolder,
			ChangesetVersionSpec changesetVersion,
			TrackMergeInfo trackMergeInfo,
			BranchValidator branchValidator,
			IEventAggregator eventAggregator)
		{
			_sourceBranch = sourceBranch;
			_sourceFolder = sourceFolder;
			_changesetVersion = changesetVersion;
			_trackMergeInfo = trackMergeInfo;
			_branchValidator = branchValidator;
			_eventAggregator = eventAggregator;
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
			var mergeInfo = new MergeInfoViewModel(_eventAggregator)
			{
				SourceBranch = _sourceBranch,
				TargetBranch = targetBranch,
				SourcePath = _sourceFolder,
				TargetPath = targetPath,
				ChangesetVersionSpec = _changesetVersion,
				FileMergeInfos = new List<FileMergeInfo>(0),
				ValidationResult = BranchValidationResult.Success,
			};

			if (_sourceBranch != targetBranch)
			{
				mergeInfo.Comment = EvaluateComment(_trackMergeInfo, _sourceBranch, targetBranch);
				mergeInfo = _branchValidator.Validate(mergeInfo);
			}

			return mergeInfo;
		}

		private static string EvaluateComment(TrackMergeInfo trackMergeInfo, string sourceBranch, string targetBranch)
		{
			var mergePath = trackMergeInfo.SourceBranches.Concat(new[] { sourceBranch, targetBranch })
				.Select(GetShortBranchName);
			var mergePathString = string.Join(" -> ", mergePath);
			return string.Format("MERGE {0} ({1})", mergePathString, trackMergeInfo.SourceComment);
		}

		private static string GetShortBranchName(string fullBranchName)
		{
			var pos = fullBranchName.LastIndexOf('/');
			var shortName = fullBranchName.Substring(pos + 1);
			return shortName;
		}
	}
}