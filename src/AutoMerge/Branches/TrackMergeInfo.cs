using System.Collections.Generic;

namespace AutoMerge
{
	public class TrackMergeInfo
	{
	    public string OriginaBranch { get; set; }

		public string OriginalComment { get; set; }

		public List<string> FromOriginalToSourceBranches { get; set; }

	    public string SourceBranch { get; set; }

	    public string SourceComment { get; set; }

        public long SourceChangesetId { get; set; }

        public List<long> SourceWorkItemIds { get; set; }
	}
}
