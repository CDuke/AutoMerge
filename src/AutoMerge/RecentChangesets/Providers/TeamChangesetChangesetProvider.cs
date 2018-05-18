using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMerge.Branches;
using Microsoft.TeamFoundation.VersionControl.Client;


namespace AutoMerge
{
    public class TeamChangesetChangesetProvider : ChangesetProviderBase
    {
        private string _sourceBranch;
        private string _targetBranch;


        public TeamChangesetChangesetProvider(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public List<TeamProject> ListProjects()
        {
            List<TeamProject> result = new List<TeamProject>();

            var changesetService = GetChangesetService();

            if (changesetService != null)
            {
                result.AddRange(changesetService.ListTfsProjects());
            }
            
            return result;
        }

        public List<Branch> ListBranches(string projectName)
        {
            var result = new List<Branch>();

            var changesetService = GetChangesetService();

            if (changesetService != null)
            {
                var branches = changesetService.ListBranches(projectName);

                foreach (var branchObject in branches)
                {
                    var branch = new Branch();

                    branch.Name = branchObject.Properties.RootItem.Item;
                    branch.Branches = branchObject.ChildBranches.Where(x => !x.IsDeleted).Select(x => x.Item).ToList();

                    if (branchObject.Properties.ParentBranch != null)
                    {
                        branch.Branches.Add(branchObject.Properties.ParentBranch.Item);
                    }

                    result.Add(branch);
                }
            }

            return result;

        }

        public void SetSourceAndTargetBranch(string sourceBranch, string targetBranch)
        {
            _sourceBranch = sourceBranch;
            _targetBranch = targetBranch;
        }

        protected override List<ChangesetViewModel> GetChangesetsInternal()
        {
            var changesets = new List<ChangesetViewModel>();

            var changesetService = GetChangesetService();

            if (changesetService != null)
            {
                var tfsChangesets = changesetService.GetMergeCandidates(_sourceBranch, _targetBranch);

                changesets = tfsChangesets
                    .Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
                    .ToList();
            }

            return changesets;
        }

    }
}
