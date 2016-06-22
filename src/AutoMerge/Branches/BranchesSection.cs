using System.ComponentModel.Composition;
using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.ComponentModelHost;

namespace AutoMerge
{
    [TeamExplorerSection(GuidList.BranchesSectionId, GuidList.AutoMergePageId, 20)]
    public class BranchesSection : TeamExplorerSectionBase
    {
        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return new BranchesView();
        }

        [Import]
        public Settings Settings { get; set; }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            var viewModel = base.CreateViewModel(e) ?? new BranchesViewModel(Settings, new VsLogger(ServiceProvider));

            return viewModel;
        }
    }
}
