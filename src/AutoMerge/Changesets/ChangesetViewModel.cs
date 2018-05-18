using System.Collections.Generic;

namespace AutoMerge
{
	public class ChangesetViewModel
	{
		public int ChangesetId { get; set; }

		public string Comment { get; set; }

		public List<string> Branches { get; set; }

		public string DisplayBranchName
		{
			get { return BranchHelper.GetDisplayBranchName(Branches); }
		}
	}
}