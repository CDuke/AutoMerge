using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public List<string> ListBranches(string projectName)
        {
            List<string> result = new List<string>();

            result.Add("Test/Main");
            result.Add("ROL-Omgeving/Main");


            var changesetService = GetChangesetService();

            if (changesetService != null)
            {
                changesetService.ListBranches("jos");

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

                ///dummy calls for testing the functionality
                var prj = changesetService.ListTfsProjects();
                var s = changesetService.ListBranches("Test");

                var tfsChangesets = changesetService.GetMergeCandidates(_sourceBranch, _targetBranch);

                changesets = tfsChangesets
                    .Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
                    .ToList();

            }

            return changesets;
        }

    }
}
