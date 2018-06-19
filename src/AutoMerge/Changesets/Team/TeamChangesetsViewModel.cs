using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMerge.Branches;
using AutoMerge.Helpers;
using AutoMerge.Prism.Command;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
    public class TeamChangesetsViewModel : ChangesetsViewModel
    {
        private BranchTeamService _branchTeamService;
        private TeamChangesetChangesetProvider _teamChangesetChangesetProvider;
        private List<Branch> _currentBranches;

        public TeamChangesetsViewModel(ILogger logger) : base(logger)
        {
            SelectedChangesets = new ObservableCollection<ChangesetViewModel>();
            SourcesBranches = new ObservableCollection<string>();
            TargetBranches = new ObservableCollection<string>();
            ProjectNames = new ObservableCollection<string>();

            MergeCommand = DelegateCommand.FromAsyncHandler(MergeAsync, CanMerge);
            FetchChangesetsCommand = DelegateCommand.FromAsyncHandler(FetchChangesetsAsync, CanFetchChangesets);
        }

        public DelegateCommand MergeCommand { get; private set; }
        public DelegateCommand FetchChangesetsCommand { get; private set; }

        public ObservableCollection<string> ProjectNames { get; set; }
        public ObservableCollection<string> SourcesBranches { get; set; }
        public ObservableCollection<string> TargetBranches { get; set; }

        private string _selectedProjectName;

        public string SelectedProjectName
        {
            get { return _selectedProjectName; }
            set
            {
                _selectedProjectName = value;
                RaisePropertyChanged("SelectedProjectName");

                _currentBranches = _teamChangesetChangesetProvider.ListBranches(SelectedProjectName);

                Changesets.Clear();
                SourcesBranches.Clear();
                TargetBranches.Clear();
                SourcesBranches.AddRange(_currentBranches.Select(x => x.Name));

                UpdateTitle();
            }
        }

        private string _sourceBranch;

        public string SourceBranch
        {
            get { return _sourceBranch; }
            set
            {
                _sourceBranch = value;
                RaisePropertyChanged("SourceBranch");
                InitializeTargetBranches();

                FetchChangesetsCommand.RaiseCanExecuteChanged();
            }
        }

        private string _targetBranch;

        public string TargetBranch
        {
            get { return _targetBranch; }
            set
            {
                _targetBranch = value;
                RaisePropertyChanged("TargetBranch");

                FetchChangesetsCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<ChangesetViewModel> _selectedChangesets;

        public ObservableCollection<ChangesetViewModel> SelectedChangesets
        {
            get { return _selectedChangesets; }
            set
            {
                _selectedChangesets = value;
                RaisePropertyChanged("SelectedChangesets");

                if (_selectedChangesets != null)
                {
                    _selectedChangesets.CollectionChanged += SelectedChangesets_CollectionChanged;
                }
            }
        }

        private void SelectedChangesets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MergeCommand.RaiseCanExecuteChanged();
        }

        private async Task MergeAsync()
        {
            await SetBusyWhileExecutingAsync(async () =>
            {
                var orderedSelectedChangesets = SelectedChangesets.OrderBy(x => x.ChangesetId).ToList();

                await Task.Run(() => _branchTeamService.MergeBranches(SourceBranch, TargetBranch, orderedSelectedChangesets.First().ChangesetId, orderedSelectedChangesets.Last().ChangesetId));
                _branchTeamService.AddWorkItemsAndNavigate(orderedSelectedChangesets.Select(x => x.ChangesetId));
            });
        }

        private bool CanMerge()
        {
            return SelectedChangesets != null
                && !IsBusy
                && SelectedChangesets.Any()
                && Changesets.Count(x => x.ChangesetId >= SelectedChangesets.Min(y => y.ChangesetId) &&
                                         x.ChangesetId <= SelectedChangesets.Max(y => y.ChangesetId)) == SelectedChangesets.Count;
        }

        private async Task FetchChangesetsAsync()
        {
            await SetBusyWhileExecutingAsync(async () => await GetChangesetAndUpdateTitleAsync());

            MergeCommand.RaiseCanExecuteChanged();
        }

        private bool CanFetchChangesets()
        {
            return SourceBranch != null && TargetBranch != null && !IsBusy;
        }

        protected override Task InitializeAsync(object sender, SectionInitializeEventArgs e)
        {
            _branchTeamService = new BranchTeamService(Context.TeamProjectCollection, (ITeamExplorer)ServiceProvider.GetService(typeof(ITeamExplorer)));
            _teamChangesetChangesetProvider = new TeamChangesetChangesetProvider(ServiceProvider);

            var projectNames = _teamChangesetChangesetProvider.ListProjects();
            projectNames.ForEach(x => ProjectNames.Add(x.Name));

            return base.InitializeAsync(sender, e);
        }

        public void InitializeTargetBranches()
        {
            TargetBranches.Clear();

            if (SourceBranch != null)
            {
                TargetBranches.AddRange(_currentBranches.Single(x => x.Name == SourceBranch).Branches);
            }
        }

        public override async Task<List<ChangesetViewModel>> GetChangesetsAsync()
        {
            _teamChangesetChangesetProvider.SetSourceAndTargetBranch(SourceBranch, TargetBranch);
            return await _teamChangesetChangesetProvider.GetChangesets();
        }

        public override void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            base.SaveContext(sender, e);

            var context = new TeamChangesetsViewModelContext
            {
                SelectedProjectName = SelectedProjectName,
                Changesets = Changesets,
                Title = Title,
                SourceBranch = SourceBranch,
                TargetBranch = TargetBranch
            };

            e.Context = context;
        }

        protected override void RestoreContext(SectionInitializeEventArgs e)
        {
            var context = (TeamChangesetsViewModelContext)e.Context;

            SelectedProjectName = context.SelectedProjectName;
            Changesets = context.Changesets;
            Title = context.Title;
            SourceBranch = context.SourceBranch;
            TargetBranch = context.TargetBranch;
        }

        public override void Loaded(object sender, SectionLoadedEventArgs e)
        {
            base.Loaded(sender, e);

            //manually set to false because apparently HideBusy will set isBusy on false much later...
            IsBusy = false;

            MergeCommand.RaiseCanExecuteChanged();
            FetchChangesetsCommand.RaiseCanExecuteChanged();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_selectedChangesets != null)
            {
                _selectedChangesets.CollectionChanged -= SelectedChangesets_CollectionChanged;
            }
        }

        protected override string BaseTitle
        {
            get
            {
                return "Project name: " + SelectedProjectName;
            }
        }

    }
}
