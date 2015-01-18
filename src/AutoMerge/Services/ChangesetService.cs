using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class ChangesetService
	{
		private readonly VersionControlServer _versionControlServer;
		private readonly string _teamProjectName;
		

		public ChangesetService(VersionControlServer versionControlServer)
		{
			_versionControlServer = versionControlServer;
		}

		public ICollection<Changeset> GetUserChangesets(string teamProjectName, string userName, int count)
		{
			var path = "$/" + _teamProjectName;
			return _versionControlServer.QueryHistory(path,
				VersionSpec.Latest,
				0,
				RecursionType.Full,
				userName,
				null,
				null,
				count,
				false,
				true)
				.Cast<Changeset>()
				.ToList();
		}

		public Changeset GetChangeset(int changesetId)
		{
			var changeset = _versionControlServer.GetChangeset(changesetId, false, false);

			return changeset;
		}

		public Change[] GetChanges(int changesetId)
		{
			return _versionControlServer.GetChangesForChangeset(changesetId, false, int.MaxValue, null, null, null, true);
		}

		public List<ItemIdentifier> GetAssociatedBranches(params int[] changesetId)
		{
			var branches = _versionControlServer.QueryBranchObjectOwnership(changesetId);

			return branches.Select(b => b.RootItem).ToList();
		}
	}
}