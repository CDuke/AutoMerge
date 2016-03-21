using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoMerge
{
    public class CommentFormater
    {
        private readonly CommentFormat _format;

        public CommentFormater(CommentFormat format)
        {
            _format = format;
        }

        public string Format(TrackMergeInfo trackMergeInfo, string targetBranch, MergeOption mergeOption)
        {
            var comment = mergeOption == MergeOption.KeepTarget ? _format.DiscardFormat : _format.Format;
            comment = comment
                .Replace("{OriginalBranch}", BranchHelper.GetShortBranchName(trackMergeInfo.OriginaBranch))
                .Replace("{OriginalBranchFull}", trackMergeInfo.OriginaBranch)
                .Replace("{SourceBranch}", BranchHelper.GetShortBranchName(trackMergeInfo.SourceBranch))
                .Replace("{SourceBranchFull}", trackMergeInfo.SourceBranch)
                .Replace("{TargetBranch}", BranchHelper.GetShortBranchName(targetBranch))
                .Replace("{TargetBranchFull}", targetBranch)
                .Replace("{FromOriginalToTarget}", FromOriginalToTarget(trackMergeInfo, targetBranch))
                .Replace("{FromOriginalToTargetFull}", FromOriginalToTargetFull(trackMergeInfo, targetBranch))
                .Replace("{OriginalComment}", trackMergeInfo.OriginalComment)
                .Replace("{SourceComment}", trackMergeInfo.SourceComment)
                .Replace("{SourceChangesetId}", trackMergeInfo.SourceChangesetId.ToString(CultureInfo.InvariantCulture))
                .Replace("{SourceWorkItemIds}", GetWorkItemIds(trackMergeInfo.SourceWorkItemIds))
                .Replace("{SourceWorkItemTitles}", GetWorkItemTitles(trackMergeInfo.SourceWorkItemTitles));

            return comment;
        }

        private static string GetWorkItemIds(List<long> sourceWorkItemIds)
        {
            return string.Join(", ", sourceWorkItemIds.Select(id => id.ToString(CultureInfo.InvariantCulture)));
        }

        private static string GetWorkItemTitles(List<string> sourceWorkItemTitles)
        {
            return string.Join("; ", sourceWorkItemTitles);
        }

        private string FromOriginalToTarget(TrackMergeInfo trackMergeInfo, string targetBranch)
        {
            var mergePath = trackMergeInfo.FromOriginalToSourceBranches.Concat(new[] { trackMergeInfo.SourceBranch, targetBranch })
                .Select(fullBranchName => BranchHelper.GetShortBranchName(fullBranchName));
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
