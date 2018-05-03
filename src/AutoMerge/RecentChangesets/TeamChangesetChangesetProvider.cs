using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMerge
{
    public class TeamChangesetChangesetProvider : ChangesetProviderBase
    {
        private readonly string _sourceBranch;
        private readonly string _targetBranch;


        public TeamChangesetChangesetProvider(IServiceProvider serviceProvider, string sourceBranch, string targetBranch) : base(serviceProvider)
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
                    var projectName = ProjectNameHelper.GetProjectName(ServiceProvider);
                    var tfsChangesets = changesetService.GetMergeCandidates(_sourceBranch, _targetBranch);

                    changesets = tfsChangesets
                        .Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
                        .ToList();
                
            }

            return changesets;
        }

    }
}
