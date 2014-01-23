using System.Collections.Generic;
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
					userName, null, null, MaxChangesets, true, true))
				{
					result.Add(changeset);
				}

				return result;
			});
		}
	}
}