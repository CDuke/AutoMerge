using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.ComponentModelHost;

namespace AutoMerge
{
    public abstract class ChangesetProviderBase : IChangesetProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<ChangesetService> _changesetService;
        private readonly Settings _settings;

        protected ChangesetProviderBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _changesetService = new Lazy<ChangesetService>(InitChangesetService);

            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof (SComponentModel));
            _settings = componentModel.DefaultExportProvider.GetExportedValue<Settings>();
        }

        public Task<List<ChangesetViewModel>> GetChangesets(string userLogin)
        {
            return Task.Run(() => GetChangesetsInternal(userLogin));
        }

        protected abstract List<ChangesetViewModel> GetChangesetsInternal(string userLogin);

        protected ChangesetViewModel ToChangesetViewModel(Changeset tfsChangeset, ChangesetService changesetService)
        {
            var branches = changesetService.GetAssociatedBranches(tfsChangeset.ChangesetId)
                .Select(i => i.Item)
                .ToList();
            var changesetViewModel = new ChangesetViewModel
            {
                ChangesetId = tfsChangeset.ChangesetId,
                Comment = tfsChangeset.Comment,
                Branches = branches,
                DisplayBranchName = BranchHelper.GetDisplayBranchName(branches, _settings.BranchNameMatches)
            };

            return changesetViewModel;
        }

        protected ChangesetService GetChangesetService()
        {
            return _changesetService.Value;
        }

        private ChangesetService InitChangesetService()
        {
            var context = VersionControlNavigationHelper.GetTeamFoundationContext(_serviceProvider);
            if (context != null && VersionControlNavigationHelper.IsConnectedToTfsCollectionAndProject(context))
            {
                var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
                if (vcs != null)
                {
                    return new ChangesetService(vcs);
                }
            }
            return null;
        }

        protected string GetProjectName()
        {
            var context = VersionControlNavigationHelper.GetTeamFoundationContext(_serviceProvider);
            if (context != null)
            {
                return context.TeamProjectName;
            }
            return null;
        }
    }
}
