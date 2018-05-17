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


        public Change[] GetChanges(int changesetId)
        {
            return _versionControlServer.GetChangesForChangeset(changesetId, false, int.MaxValue, null, null, null, true);
        }

        public List<ItemIdentifier> GetAssociatedBranches(params int[] changesetId)
        {
            var branches = _versionControlServer.QueryBranchObjectOwnership(changesetId);

            return branches.Select(b => b.RootItem).ToList();
        }


        public ICollection<Changeset> GetMergeCandidates(string sourceBranch, string targetBranch)
        {
            var dummy = _versionControlServer.GetMergeCandidates(sourceBranch, targetBranch, RecursionType.Full);

            List<Changeset> result = new List<Changeset>();

            foreach (MergeCandidate mc in dummy)
            {
                result.Add(mc.Changeset);
            }

            return result;
        }

        public IEnumerable<TeamProject> ListTfsProjects()
        {
            var result = _versionControlServer.GetAllTeamProjects(true);
            return result;
        }


        public IEnumerable<BranchObject> ListBranches(string projectName)
        {                
            var dummy =  _versionControlServer.QueryRootBranchObjects(RecursionType.Full);

            var result = new List<BranchObject>();
            foreach(var bo in dummy)
            {
                var ro = bo.Properties.RootItem;

                System.Diagnostics.Debug.WriteLine(ro.Item);

                if (!ro.IsDeleted && ro.Item.Replace(@"$/", "").StartsWith(projectName + @"/"))
                    result.Add(bo);
            }

            return result;
        }


    }
}
