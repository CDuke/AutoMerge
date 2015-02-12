using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
    public class MergeResultModel
    {
        public MergeResult MergeResult { get; set; }

        public List<PendingChange> PendingChanges { get; set; }

        public MergeInfoViewModel BranchInfo { get; set; }

        public string Comment { get; set; }

        public List<int> WorkItemIds { get; set; }

        public int SourceChangesetId { get; set; }

        public int? TagetChangesetId { get; set; }
    }
}
