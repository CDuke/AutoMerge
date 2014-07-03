using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutoMerge.Events;
using Microsoft.Practices.Prism;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Task = System.Threading.Tasks.Task;

using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
    public class RecentChangesetViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly string _baseTitle;
        private readonly IEventAggregator _eventAggregator;

        public RecentChangesetViewModel(ILogger logger)
            : base(logger)
        {
            Title = Resources.RecentChangesetSectionName;
            IsVisible = true;
            IsExpanded = true;
            IsBusy = false;
            Changesets = new ObservableCollection<ChangesetViewModel>();
            _baseTitle = Title;

            _eventAggregator = EventAggregatorFactory.Get();
            _eventAggregator.GetEvent<MergeCompleteEvent>()
                .Subscribe(OnMergeComplete);

            ViewChangesetDetailsCommand = new DelegateCommand(ViewChangesetDetailsExecute, ViewChangesetDetailsCanExecute);
            ToggleAddByIdCommand = new DelegateCommand(ToggleAddByIdExecute, ToggleAddByIdCanExecute);
            CancelAddChangesetByIdCommand = new DelegateCommand(CancelAddByIdExecute);
            AddChangesetByIdCommand = new DelegateCommand(AddChangesetByIdExecute, AddChangesetByIdCanExecute);
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

        public bool ShowAddByIdChangeset
        {
            get
            {
                return _showAddByIdChangeset;
            }
            set
            {
                _showAddByIdChangeset = value;
                RaisePropertyChanged("ShowAddByIdChangeset");
            }
        }
        private bool _showAddByIdChangeset;

        public string ChangesetIdsText
        {
            get
            {
                return _changesetIdsText;
            }
            set
            {
                _changesetIdsText = value;
                RaisePropertyChanged("ChangesetIdsText");
                InvalidateCommands();
            }
        }
        private string _changesetIdsText;

        public DelegateCommand ViewChangesetDetailsCommand { get; private set; }

        public DelegateCommand ToggleAddByIdCommand { get; private set; }

        public DelegateCommand AddChangesetByIdCommand { get; private set; }

        public DelegateCommand CancelAddChangesetByIdCommand { get; private set; }

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
            await RefreshAsync();
        }

        protected override async Task RefreshAsync()
        {
            Changesets.Clear();

            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider);
            var userLogin = VersionControlNavigationHelper.GetAuthorizedUser(ServiceProvider);
            
            Log("Getting changesets starting...");
            var changesets = await changesetProvider.GetChangesets(userLogin);
            Log("Getting changesets end");

            Changesets = new ObservableCollection<ChangesetViewModel>(changesets);
            UpdateTitle();

            if (Changesets.Count > 0)
                SelectedChangeset = Changesets[0];
        }

        private void UpdateTitle()
        {
            Title = Changesets.Count > 0
                ? string.Format("{0} ({1})", _baseTitle, Changesets.Count)
                : _baseTitle;
        }

        private void ToggleAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = true;
                InvalidateCommands();
                ResetAddById();
                SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetIdTextBox);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                throw;
            }
        }

        private bool ToggleAddByIdCanExecute()
        {
            return !ShowAddByIdChangeset;
        }

        private void CancelAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = false;
                InvalidateCommands();
                SetMvvmFocus(RecentChangesetFocusableControlNames.AddChangesetByIdLink);
                ResetAddById();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ResetAddById()
        {
            ChangesetIdsText = string.Empty;
        }

        private async void AddChangesetByIdExecute()
        {
            ShowBusy();
            try
            {
                var changesetIds = GeChangesetIdsToAdd(ChangesetIdsText);
                if (changesetIds.Count > 0)
                {
                    var changesetProvider = new ChangesetByIdChangesetProvider(ServiceProvider, changesetIds);
                    var changesets = await changesetProvider.GetChangesets(null);

                    if (changesets.Count > 0)
                    {
                        Changesets.AddRange(changesets);
                        SelectedChangeset = changesets[0];
                        SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetList);
                        UpdateTitle();
                    }
                    ShowAddByIdChangeset = false;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            HideBusy();
        }

        private bool AddChangesetByIdCanExecute()
        {
            try
            {
                return GeChangesetIdsToAdd(ChangesetIdsText).Count > 0;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                TeamFoundationTrace.TraceException(ex);
            }
            return false;
        }

        private static List<int> GeChangesetIdsToAdd(string text)
        {
            var list = new List<int>();
            var idsStrArray = string.IsNullOrEmpty(text) ? new string[0] : text.Split(new char[2] { ',', ';' });
            if (idsStrArray.Length > 0)
            {
                foreach (var idStr in idsStrArray)
                {
                    int result;
                    if (int.TryParse(idStr.Trim(), out result) && result > 0)
                        list.Add(result);
                }
            }
            return list;
        }

        private void InvalidateCommands()
        {
            ViewChangesetDetailsCommand.RaiseCanExecuteChanged();
            ToggleAddByIdCommand.RaiseCanExecuteChanged();
            CancelAddChangesetByIdCommand.RaiseCanExecuteChanged();
            AddChangesetByIdCommand.RaiseCanExecuteChanged();
        }
    }
}
