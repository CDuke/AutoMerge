using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class MergeResultModel
	{
		public List<PendingChange> PendingChanges { get; set; }

		public MergeResult MergeResult { get; set; }

		public string Message { get; set; }

		public bool CheckedIn { get; set; }

		public MergeInfoViewModel BranchInfo { get; set; }

        public string Comment { get; set; }

		public List<int> WorkItemIds { get; set; }

		public int? ChangesetId { get; set; }

		public bool HasConflicts { get; set; }
	}
}
