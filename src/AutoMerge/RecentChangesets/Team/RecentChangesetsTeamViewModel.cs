using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AutoMerge
{
    public class RecentChangesetsTeamViewModel : RecentChangesetsViewModel
    {
        public RecentChangesetsTeamViewModel(ILogger logger) : base(logger)
        {
            SelectedChangesets = new ObservableCollection<ChangesetViewModel>();
        }

        public override async Task<List<ChangesetViewModel>> GetChangesets()
        {
            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider, Settings.Instance.ChangesetCount);
            var userLogin = VersionControlNavigationHelper.GetAuthorizedUser(ServiceProvider);

            return await changesetProvider.GetChangesets(userLogin);
        }

        public ObservableCollection<ChangesetViewModel> SelectedChangesets { get; }
    }
}
