using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMerge
{
    public class BranchTeamService
    {
        private readonly Workspace _workspace;
        private readonly ITeamExplorer _teamExplorer;
        private readonly ChangesetService _changesetService;

        public BranchTeamService(TfsTeamProjectCollection tfs, ITeamExplorer teamExplorer)
        {
            var versionControlServer = tfs.GetService<VersionControlServer>();

            _workspace = WorkspaceHelper.GetWorkspace(versionControlServer, WorkspaceHelper.GetWorkspaces(versionControlServer, tfs));
            _teamExplorer = teamExplorer;
            _changesetService = new ChangesetService(versionControlServer);
        }

        public void MergeBranches(string source, string target, int from, int to)
        {
            _workspace.Merge(source, target, new ChangesetVersionSpec(from), new ChangesetVersionSpec(to), LockLevel.None, RecursionType.Full, MergeOptions.None);
        }

        public void AddWorkItemsAndNavigate(IEnumerable<int> changesetIds)
        {
            var workItemIds = new List<int>();

            foreach(var changesetId in changesetIds)
            {
                var changeSet = _changesetService.GetChangeset(changesetId);
                workItemIds.AddRange(changeSet.AssociatedWorkItems?.Select(x => x.Id) ?? new List<int>());
            }

            var pendingChangePage = (TeamExplorerPageBase) _teamExplorer.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
            var pendingChangeModel = (IPendingCheckin) pendingChangePage.Model;

            var modelType = pendingChangeModel.GetType();
            var method = modelType.GetMethod("AddWorkItemsByIdAsync", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            method.Invoke(pendingChangeModel, new object[] { workItemIds.ToArray(), 1 });
        }
    }
}
