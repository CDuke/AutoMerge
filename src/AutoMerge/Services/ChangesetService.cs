using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class ChangesetService
	{
		private readonly VersionControlServer _versionControlServer;

		public ChangesetService(VersionControlServer versionControlServer)
		{
			_versionControlServer = versionControlServer;
		}

		public ICollection<Changeset> GetUserChangesets(string teamProjectName, string userName, int count)
		{
			var path = "$/" + teamProjectName;
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

        

        public ICollection<Changeset> GetMergeCandidates(string sourceBranch, string targetBranch)
        {
            var smos = _versionControlServer.GetMergeCandidates(sourceBranch, targetBranch, RecursionType.Full);

            List<Changeset> result = new List<Changeset>();

            foreach (MergeCandidate mc in smos)
            {
                result.Add(mc.Changeset);
            }

            return result;
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
