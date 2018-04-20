using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMerge
{
    public class TeamChangesetChangesetProvider : ChangesetProviderBase
    {
        private readonly int _maxChangesetCount;
        private readonly string _branchLocation;

        public TeamChangesetChangesetProvider(IServiceProvider serviceProvider, string branchLocation,  int maxChangesetCount) : base(serviceProvider)
        {
            _maxChangesetCount = maxChangesetCount;
            _branchLocation = branchLocation;
        }

        protected override List<ChangesetViewModel> GetChangesetsInternal()
        {
            var changesets = new List<ChangesetViewModel>();

                var changesetService = GetChangesetService();

                if (changesetService != null)
                {                    
                    var projectName = GetProjectName();
                    var tfsChangesets = changesetService.GetMergeCandidates("$/Test/Branches/B01", "$/Test/Main");

                    //var tfsChangesets = changesetService.GetTeamChangesets(projectName, _branchLocation, _maxChangesetCount);

                    changesets = tfsChangesets
                        .Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
                        .ToList();
                
            }

            return changesets;
        }

    }
}
