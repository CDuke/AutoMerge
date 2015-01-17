using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common.Internal;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.MVVM;
using TfsTeamExplorerSectionViewModelBase = Microsoft.TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase;

namespace AutoMerge.Base
{
    public abstract class TeamExplorerSectionViewModelBase : TfsTeamExplorerSectionViewModelBase
    {
        private readonly ILogger _logger;
        private static readonly Task _emptyTask = Task.FromResult(0);

        private ITeamFoundationContextManager TfsContextManager { get; set; }
        protected ITeamFoundationContext Context { get; private set; }

        protected TeamExplorerSectionViewModelBase(ILogger logger)
        {
            _logger = logger;
        }

        protected ILogger Logger { get { return _logger; } }

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
                if (ServiceProvider != null)
                {
                    TfsContextManager = ServiceProvider.GetService<ITeamFoundationContextManager>();
                    if (TfsContextManager != null)
                    {
                        TfsContextManager.ContextChanged -= OnContextChanged;
                        TfsContextManager.ContextChanged += OnContextChanged;
                        var context = TfsContextManager.CurrentContext;
                        Context = context;
                    }
                }
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
                _logger.Error(ex.Message, ex);
            }

            HideBusy();
        }

        protected void SetMvvmFocus(string id, params object[] args)
        {
            var focusService = TryResolveService<IFocusService>();
            if (focusService == null)
                return;
            focusService.SetFocus(id, args);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (TfsContextManager != null)
            {
                TfsContextManager.ContextChanged -= OnContextChanged;
            }
        }

        protected virtual void OnContextChanged(object sender, ContextChangedEventArgs e)
        {

        }
    }
}
