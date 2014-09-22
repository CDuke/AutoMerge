using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.MVVM;
using TfsTeamExplorerSectionBase = Microsoft.TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionBase;

namespace AutoMerge.Base
{
	public abstract class TeamExplorerSectionBase : TfsTeamExplorerSectionBase
	{
//		protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
//		{
//			if (e.Context != null)
//				return e.Context as ITeamExplorerSection;
//
//			return null;
//		}
//
//		protected override void InitializeViewModel(SectionInitializeEventArgs e)
//		{
//			if (e.Context == null)
//				base.InitializeViewModel(e);
//		}
//
//		public override void SaveContext(object sender, SectionSaveContextEventArgs e)
//		{
//			e.Context = ViewModel;
//		}
//
//		public override void Initialize(object sender, SectionInitializeEventArgs e)
//		{
//			base.Initialize(sender, e);
//			var viewModelBase = ViewModel as ViewModelBase;
//			if (View != null && viewModelBase != null)
//			{
//				viewModelBase.RegisterService(View);
//			}
//		}
	}
}
