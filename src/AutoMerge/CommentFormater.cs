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

        public string Format(TrackMergeInfo trackMergeInfo, string targetBranch)
        {
            var comment = _format.Format;
            comment = comment
                .Replace("{OriginalBranch}", GetShortBranchName(trackMergeInfo.OriginaBranch))
                .Replace("{OriginalBranchFull}", trackMergeInfo.OriginaBranch)
                .Replace("{SourceBranch}", GetShortBranchName(trackMergeInfo.SourceBranch))
                .Replace("{SourceBranchFull}", trackMergeInfo.SourceBranch)
                .Replace("{TargetBranch}", GetShortBranchName(targetBranch))
                .Replace("{TargetBranchFull}", targetBranch)
                .Replace("{FromOriginalToTarget}", FromOriginalToTarget(trackMergeInfo, targetBranch))
                .Replace("{FromOriginalToTargetFull}", FromOriginalToTargetFull(trackMergeInfo, targetBranch))
                .Replace("{OriginalComment}", trackMergeInfo.OriginalComment)
                .Replace("{SourceComment}", trackMergeInfo.SourceComment);

            return comment;
        }

        private string FromOriginalToTarget(TrackMergeInfo trackMergeInfo, string targetBranch)
        {
            var mergePath = trackMergeInfo.FromOriginalToSourceBranches.Concat(new[] { trackMergeInfo.SourceBranch, targetBranch })
                .Select(GetShortBranchName);
            var mergePathString = string.Join(_format.BranchDelimiter, mergePath);
            return mergePathString;
        }

        private string FromOriginalToTargetFull(TrackMergeInfo trackMergeInfo, string targetBranch)
        {
            var mergePath = trackMergeInfo.FromOriginalToSourceBranches.Concat(new[] { trackMergeInfo.SourceBranch, targetBranch });
            var mergePathString = string.Join(_format.BranchDelimiter, mergePath);
            return mergePathString;
        }

        private static string GetShortBranchName(string fullBranchName)
        {
            var pos = fullBranchName.LastIndexOf('/');
            var shortName = fullBranchName.Substring(pos + 1);
            return shortName;
        }
    }
}
