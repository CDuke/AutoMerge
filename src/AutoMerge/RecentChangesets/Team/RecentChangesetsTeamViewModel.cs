using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
    public class RecentChangesetsTeamViewModel : RecentChangesetsViewModel
    {
        public RecentChangesetsTeamViewModel(ILogger logger) : base(logger)
        {
            SelectedChangesets = new ObservableCollection<ChangesetViewModel>();
            SourcesBranches = new ObservableCollection<string>();
            TargetBranches = new ObservableCollection<string>();
        }

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

                RefreshAsync();
            }
        }

        public ObservableCollection<ChangesetViewModel> SelectedChangesets { get; }

        protected override Task InitializeAsync(object sender, SectionInitializeEventArgs e)
        {
            //Find all sources branches.
            SourcesBranches.Add("$/Test/Branches/B01");

            return base.InitializeAsync(sender, e);
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

        public void InitializeTargetBranches()
        {
            TargetBranches.Clear();

            //Find all possible target branches
            TargetBranches.Add("$/Test/Main");
        }

        protected override string BaseTitle => "Project name: " + new TeamChangesetChangesetProvider(ServiceProvider, "$/Test/Branches/B01", "$/Test/Main").GetProjectName();
    }
}
