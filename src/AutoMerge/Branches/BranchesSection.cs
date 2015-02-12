using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
    [TeamExplorerSection(GuidList.BranchesSectionId, GuidList.AutoMergePageId, 20)]
    public class BranchesSection : TeamExplorerSectionBase
    {
        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return new BranchesView();
        }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            var viewModel = base.CreateViewModel(e) ?? new BranchesViewModel(new VsLogger(ServiceProvider));

            return viewModel;
        }
    }
}
