using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class FileMergeInfo
	{
		public string SourceFile { get; set; }

		public string TargetFile { get; set; }

		public ChangesetVersionSpec ChangesetVersionSpec { get; set; }
	}
}