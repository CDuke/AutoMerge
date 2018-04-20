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
            var changesetProvider = new TeamChangesetChangesetProvider(ServiceProvider, "$/Test/Branches/B01", "$/Test/Main");

            return await changesetProvider.GetChangesets();
        }

        public ObservableCollection<ChangesetViewModel> SelectedChangesets { get; }
    }
}
