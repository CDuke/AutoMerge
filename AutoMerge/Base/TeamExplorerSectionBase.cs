using Microsoft.TeamFoundation.Controls;
using TfsTeamExplorerSectionBase = Microsoft.TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionBase;

namespace AutoMerge.Base
{
	public abstract class TeamExplorerSectionBase : TfsTeamExplorerSectionBase
	{
		protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
		{
			if (e.Context != null)
				return e.Context as ITeamExplorerSection;

			return null;
		}

		protected override void InitializeViewModel(SectionInitializeEventArgs e)
		{
			if (e.Context == null)
				base.InitializeViewModel(e);
		}

		public override void SaveContext(object sender, SectionSaveContextEventArgs e)
		{
			e.Context = ViewModel;
		}
	}
}