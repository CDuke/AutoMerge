using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class ChangesetService
	{
		private readonly VersionControlServer _versionControlServer;
		private readonly string _teamProjectName;
		

		public ChangesetService(VersionControlServer versionControlServer, string teamProjectName)
		{
			_versionControlServer = versionControlServer;
			_teamProjectName = teamProjectName;
		}

		public ICollection<Changeset> GetUserChangesets(string userName, int count)
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
			var changeset = _versionControlServer.GetChangeset(changesetId, true, false);

			return changeset;
		}

		public Change[] GetChanges(int changesetId)
		{
			return _versionControlServer.GetChangesForChangeset(changesetId, false, int.MaxValue, null, null, null, false);
		}

		public List<ItemIdentifier> GetAssociatedBranches(int changesetId)
		{
			var branches = _versionControlServer.QueryBranchObjectOwnership(new[] { changesetId });

			return branches.Select(b => b.RootItem).ToList();
		}
	}
}