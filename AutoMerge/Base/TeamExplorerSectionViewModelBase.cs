using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Controls;
using TfsTeamExplorerSectionViewModelBase = Microsoft.TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase;

namespace AutoMerge.Base
{
	public abstract class TeamExplorerSectionViewModelBase : TfsTeamExplorerSectionViewModelBase
	{
		private static readonly Task _emptyTask = Task.FromResult(0);

		protected virtual Task RefreshAsync()
		{
			return _emptyTask;
		}

		protected virtual Task InitializeAsync(object sender, SectionInitializeEventArgs e)
		{
			return _emptyTask;
		}

		public async override void Initialize(object sender, SectionInitializeEventArgs e)
		{
			base.Initialize(sender, e);
			await InitializeAsync(sender, e);
		}

		public async override void Refresh()
		{
			ShowBusy();

			try
			{
				await RefreshAsync();
			}
			catch (Exception ex)
			{
				ShowError(ex.Message);
			}

			HideBusy();
		}

		public new void ShowBusy()
		{
			IsBusy = true;
		}

		public new void HideBusy()
		{
			IsBusy = false;
		}
	}
}