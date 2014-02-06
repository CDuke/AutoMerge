using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class ChangesetService
	{
		private readonly VersionControlServer _versionControlServer;
		private readonly string _teamProjectName;
		private const int MaxChangesets = 10;

		public ChangesetService(VersionControlServer versionControlServer, string teamProjectName)
		{
			_versionControlServer = versionControlServer;
			_teamProjectName = teamProjectName;
		}

		public async Task<ICollection<Changeset>> GetUserChangesets(string userName)
		{
			return await Task.Run(() =>
			{
				var result = new List<Changeset>();
				var path = "$/" + _teamProjectName;
				foreach (Changeset changeset in _versionControlServer.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
					userName, null, null, MaxChangesets, false, true))
				{
					result.Add(changeset);
				}

				return result;
			});
		}

		public Changeset GetChanget(int changesetId)
		{
			var changeset = _versionControlServer.GetChangeset(changesetId, true, false);

			return changeset;
		}

		public List<ItemIdentifier> GetAssociatedBranches(int changesetId)
		{
			var branches = _versionControlServer.QueryBranchObjectOwnership(new[] { changesetId });

			return branches.Select(b => b.RootItem).ToList();
		}
	}
}