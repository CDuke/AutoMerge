using System.Collections.ObjectModel;

namespace AutoMerge
{
    public class ChangesetsViewModelContext
    {
        public ObservableCollection<ChangesetViewModel> Changesets { get; set; }        

        public string Title { get; set; }

        public ChangesetViewModel SelectedChangeset { get; set; }
    }

}
