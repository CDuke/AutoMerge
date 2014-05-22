using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
	[TeamExplorerSection(GuidList.BranchesSectionId, GuidList.AutoMergePageId, 20)]
	public class BranchesSection : TeamExplorerSectionBase
	{
		private static readonly object _view;

		static BranchesSection()
		{
			_view = new BranchesView();
		}

		protected override object CreateView(SectionInitializeEventArgs e)
		{
			return _view;
		}

		protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
		{
			var viewModel = base.CreateViewModel(e) ?? new BranchesViewModel();

			return viewModel;
		}
	}
}