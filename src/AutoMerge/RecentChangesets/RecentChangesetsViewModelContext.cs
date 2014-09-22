using System.Collections.ObjectModel;

namespace AutoMerge
{
    public class RecentChangesetsViewModelContext
    {
        public ChangesetViewModel SelectedChangeset { get; set; }

        public ObservableCollection<ChangesetViewModel> Changesets { get; set; }

        public bool ShowAddByIdChangeset { get; set; }

        public string ChangesetIdsText { get; set; }

        public string Title { get; set; }
    }
}
