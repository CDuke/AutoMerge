using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AutoMerge.Events;
using AutoMerge.Prism.Command;
using AutoMerge.Prism.Events;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Task = System.Threading.Tasks.Task;

using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
    public abstract class RecentChangesetsViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly IEventAggregator _eventAggregator;

        protected RecentChangesetsViewModel(ILogger logger)
            : base(logger)
        {
            Title = BaseTitle;
            IsVisible = true;
            IsExpanded = true;
            IsBusy = false;
            Changesets = new ObservableCollection<ChangesetViewModel>();

            _eventAggregator = EventAggregatorFactory.Get();
            _eventAggregator.GetEvent<MergeCompleteEvent>()
                .Subscribe(OnMergeComplete);

            ViewChangesetDetailsCommand = new DelegateCommand(ViewChangesetDetailsExecute, ViewChangesetDetailsCanExecute);
        }

        public ChangesetViewModel SelectedChangeset
        {
            get
            {
                return _selectedChangeset;
            }
            set
            {
                _selectedChangeset = value;
                RaisePropertyChanged("SelectedChangeset");
                _eventAggregator.GetEvent<SelectChangesetEvent>().Publish(value);
            }
        }
        private ChangesetViewModel _selectedChangeset;

        public ObservableCollection<ChangesetViewModel> Changesets
        {
            get
            {
                return _changesets;
            }
            protected set
            {
                _changesets = value;
                RaisePropertyChanged("Changesets");
            }
        }
        private ObservableCollection<ChangesetViewModel> _changesets;

        

        public DelegateCommand ViewChangesetDetailsCommand { get; private set; }

        private void ViewChangesetDetailsExecute()
        {
            var changesetId = SelectedChangeset.ChangesetId;
            TeamExplorerUtils.Instance.NavigateToPage(TeamExplorerPageIds.ChangesetDetails, ServiceProvider, changesetId);
        }

        private bool ViewChangesetDetailsCanExecute()
        {
            return SelectedChangeset != null;
        }

        private async void OnMergeComplete(bool obj)
        {
            await RefreshAsync();
        }

        protected override async Task InitializeAsync(object sender, SectionInitializeEventArgs e)
        {
            if (e.Context == null)
            {
                await RefreshAsync();
            }
            else
            {
                RestoreContext(e);
            }
        }

        protected override async Task RefreshAsync()
        {
            Changesets.Clear();

            Logger.Info("Getting changesets ...");
            var changesets = await GetChangesets();
            Logger.Info("Getting changesets end");

            Changesets = new ObservableCollection<ChangesetViewModel>(changesets);
            UpdateTitle();

            if (Changesets.Count > 0)
            {
                if (SelectedChangeset == null || SelectedChangeset.ChangesetId != Changesets[0].ChangesetId)
                    SelectedChangeset = Changesets[0];
            }
        }

        public abstract Task<List<ChangesetViewModel>> GetChangesets();

        protected void UpdateTitle()
        {
            Title = Changesets.Count > 0
                ? string.Format("{0} ({1})", BaseTitle, Changesets.Count)
                : BaseTitle;
        }

        protected virtual void InvalidateCommands()
        {
            ViewChangesetDetailsCommand.RaiseCanExecuteChanged();
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventAggregator.GetEvent<MergeCompleteEvent>().Unsubscribe(OnMergeComplete);
        }

        protected abstract void RestoreContext(SectionInitializeEventArgs e);        

        protected override void OnContextChanged(object sender, ContextChangedEventArgs e)
        {
            Refresh();
        }

        protected abstract string BaseTitle { get; }
    }
}
