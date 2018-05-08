using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMerge.Prism.Command;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
    public class RecentChangesetsTeamViewModel : RecentChangesetsViewModel
    {
        private BranchTeamService _branchTeamService;
        private string _projectName;

        public RecentChangesetsTeamViewModel(ILogger logger) : base(logger)
        {
            SelectedChangesets = new ObservableCollection<ChangesetViewModel>();
            SourcesBranches = new ObservableCollection<string>();
            TargetBranches = new ObservableCollection<string>();

            MergeCommand = DelegateCommand.FromAsyncHandler(MergeAsync, CanMerge);
            FetchChangesetsCommand = DelegateCommand.FromAsyncHandler(FetchChangesetsAsync, CanFetchChangesets);
        }

        public DelegateCommand MergeCommand { get; }
        public DelegateCommand FetchChangesetsCommand { get; }

        public ObservableCollection<string> SourcesBranches { get; set; }
        public ObservableCollection<string> TargetBranches { get; set; }

        private string _sourceBranch;

        public string SourceBranch
        {
            get { return _sourceBranch; }
            set
            {
                _sourceBranch = value;
                RaisePropertyChanged(nameof(SourceBranch));
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
                RaisePropertyChanged(nameof(TargetBranch));

                FetchChangesetsCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<ChangesetViewModel> _selectedChangesets;

        public ObservableCollection<ChangesetViewModel> SelectedChangesets
        {
            get { return _selectedChangesets; }
            set
            {
                if (_selectedChangesets != null)
                {
                    _selectedChangesets.CollectionChanged -= SelectedChangesets_CollectionChanged;
                }

                _selectedChangesets = value;
                RaisePropertyChanged(nameof(SelectedChangesets));

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
            await SetBusyWhileExecutingAsync(async () => await RefreshAsync());

            //This is needed because HideBusy will set IsBusy on false much later. And the raise can execute checks on this boolean
            IsBusy = false;

            MergeCommand.RaiseCanExecuteChanged();
        }

        private bool CanFetchChangesets()
        {
            return SourceBranch != null && TargetBranch != null && !IsBusy;
        }

        protected override Task InitializeAsync(object sender, SectionInitializeEventArgs e)
        {
            _projectName = ProjectNameHelper.GetProjectName(ServiceProvider);
            //Find all sources branches.
            SourcesBranches.Add("$/Test/Main");

            _branchTeamService = new BranchTeamService(Context.TeamProjectCollection, (ITeamExplorer) ServiceProvider.GetService(typeof(ITeamExplorer)));

            return base.InitializeAsync(sender, e);
        }

        public void InitializeTargetBranches()
        {
            TargetBranches.Clear();

            //Find all possible target branches
            TargetBranches.Add("$/Test/Branches/BO1");
        }

        public override async Task<List<ChangesetViewModel>> GetChangesets()
        {
            if (SourceBranch != null && TargetBranch != null)
            {
                var changesetProvider = new TeamChangesetChangesetProvider(ServiceProvider, SourceBranch, TargetBranch);
                return await changesetProvider.GetChangesets();
            }

            return await Task.FromResult(new List<ChangesetViewModel>());
        }

        public override void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            base.SaveContext(sender, e);

            var context = new RecentChangesetsTeamViewModelContext
            {
                Changesets = Changesets,
                Title = Title,
                SelectedChangeset = SelectedChangeset,
                SelectedChangesets = SelectedChangesets,
                SourceBranch = SourceBranch,
                TargetBranch = TargetBranch
            };

            e.Context = context;
        }

        protected override void RestoreContext(SectionInitializeEventArgs e)
        {
            var context = (RecentChangesetsTeamViewModelContext) e.Context;
            
            Changesets = context.Changesets;
            Title = context.Title;
            SelectedChangesets = context.SelectedChangesets;
            SelectedChangeset = context.SelectedChangeset;
            SourceBranch = context.SourceBranch;
            TargetBranch = context.TargetBranch;

            MergeCommand.RaiseCanExecuteChanged();
        }

        protected override string BaseTitle => "Project name: " + _projectName;
    }
}
