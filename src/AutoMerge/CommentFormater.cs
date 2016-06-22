using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoMerge.Configuration;

namespace AutoMerge
{
    public class CommentFormater
    {
        private readonly CommentFormat _format;
        private readonly BranchNameMatch[] _aliases;

        public CommentFormater(CommentFormat format, BranchNameMatch[] aliases)
        {
            _format = format;
            _aliases = aliases;
        }

        public string Format(TrackMergeInfo trackMergeInfo, string targetBranch, MergeOption mergeOption)
        {
            var comment = mergeOption == MergeOption.KeepTarget ? _format.DiscardFormat : _format.Format;
            comment = comment
                .Replace("{OriginalBranch}", BranchHelper.GetShortBranchName(trackMergeInfo.OriginaBranch, _aliases))
                .Replace("{OriginalBranchFull}", trackMergeInfo.OriginaBranch)
                .Replace("{SourceBranch}", BranchHelper.GetShortBranchName(trackMergeInfo.SourceBranch, _aliases))
                .Replace("{SourceBranchFull}", trackMergeInfo.SourceBranch)
                .Replace("{TargetBranch}", BranchHelper.GetShortBranchName(targetBranch, _aliases))
                .Replace("{TargetBranchFull}", targetBranch)
                .Replace("{FromOriginalToTarget}", FromOriginalToTarget(trackMergeInfo, targetBranch))
                .Replace("{FromOriginalToTargetFull}", FromOriginalToTargetFull(trackMergeInfo, targetBranch))
                .Replace("{OriginalComment}", trackMergeInfo.OriginalComment)
                .Replace("{SourceComment}", trackMergeInfo.SourceComment)
                .Replace("{SourceChangesetId}", trackMergeInfo.SourceChangesetId.ToString(CultureInfo.InvariantCulture))
                .Replace("{SourceWorkItemIds}", GetWorkItemIds(trackMergeInfo.SourceWorkItemIds));

            return comment;
        }

        private string GetWorkItemIds(List<long> sourceWorkItemIds)
        {
            return string.Join(", ", sourceWorkItemIds.Select(id => id.ToString(CultureInfo.InvariantCulture)));
        }

        private string FromOriginalToTarget(TrackMergeInfo trackMergeInfo, string targetBranch)
        {
            var mergePath = trackMergeInfo.FromOriginalToSourceBranches.Concat(new[] { trackMergeInfo.SourceBranch, targetBranch })
                .Select(fullBranchName => BranchHelper.GetShortBranchName(fullBranchName, _aliases));
            var mergePathString = string.Join(_format.BranchDelimiter, mergePath);
            return mergePathString;
        }

        private string FromOriginalToTargetFull(TrackMergeInfo trackMergeInfo, string targetBranch)
        {
            var mergePath = trackMergeInfo.FromOriginalToSourceBranches.Concat(new[] { trackMergeInfo.SourceBranch, targetBranch });
            var mergePathString = string.Join(_format.BranchDelimiter, mergePath);
            return mergePathString;
        }
    }
}
