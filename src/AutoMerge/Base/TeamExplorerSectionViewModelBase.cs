using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.MVVM;
using TfsTeamExplorerSectionViewModelBase = Microsoft.TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase;

namespace AutoMerge.Base
{
    public abstract class TeamExplorerSectionViewModelBase : TfsTeamExplorerSectionViewModelBase
    {
        private readonly ILogger _logger;
        private static readonly Task _emptyTask = Task.FromResult(0);

        protected ITeamFoundationContext Context { get; private set; }

        protected TeamExplorerSectionViewModelBase(ILogger logger)
        {
            _logger = logger;
        }

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
            ShowBusy();

            try
            {
                base.Initialize(sender, e);
                Context = VersionControlNavigationHelper.GetContext(ServiceProvider);
                await InitializeAsync(sender, e);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }

            HideBusy();
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

//        public new void ShowBusy()
//        {
//            IsBusy = true;
//        }
//
//        public new void HideBusy()
//        {
//            IsBusy = false;
//        }

        protected void SetMvvmFocus(string id, params object[] args)
        {
            var focusService = TryResolveService<IFocusService>();
            if (focusService == null)
                return;
            focusService.SetFocus(id, args);
        }

        protected void Log(string message)
        {
            _logger.Log(message);
        }

        protected void Log(string message, Exception exception)
        {
            _logger.Log(message, exception);
        }
    }
}
