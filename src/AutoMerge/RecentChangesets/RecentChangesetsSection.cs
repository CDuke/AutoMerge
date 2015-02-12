using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
	[TeamExplorerSection(GuidList.RecentChangesetsSectionId, GuidList.AutoMergePageId, 10)]
	public class RecentChangesetsSection : TeamExplorerSectionBase
	{
		protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
		{
			var viewModel = base.CreateViewModel(e) ?? new RecentChangesetsViewModel(new VsLogger(ServiceProvider));

			return viewModel;
		}

		protected override object CreateView(SectionInitializeEventArgs e)
		{
            return new RecentChangesetsView();
		}
	}
}
