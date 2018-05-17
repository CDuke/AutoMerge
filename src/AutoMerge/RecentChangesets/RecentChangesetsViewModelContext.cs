using System.Collections.ObjectModel;

namespace AutoMerge
{
    public class RecentChangesetsViewModelContext
    {
        public ObservableCollection<ChangesetViewModel> Changesets { get; set; }        

        public string Title { get; set; }

        public ChangesetViewModel SelectedChangeset { get; set; }
    }

    public class RecentChangesetsTeamViewModelContext : RecentChangesetsViewModelContext
    {
        public string SourceBranch { get; set; }

        public string TargetBranch { get; set; }

        public string SelectedProjectName { get; set; }
    }

    public class RecentChangesetsSoloViewModelContext : RecentChangesetsViewModelContext
    {
        public bool ShowAddByIdChangeset { get; set; }

        public string ChangesetIdsText { get; set; }
    }
}
