using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
    public class BranchesViewModelContext
    {
        public ObservableCollection<MergeInfoViewModel> Branches { get; set; }

        public MergeInfoViewModel SelectedBranch { get; set; }

        public MergeOption MergeOption { get; set; }

        public string ErrorMessage { get; set; }

        public Workspace Workspace { get; set; }

        public ObservableCollection<Workspace> Workspaces { get; set; }

        public bool ShowWorkspaceChooser { get; set; }

        public MergeMode MergeMode { get; set; }

        public ObservableCollection<MergeMode> MergeModes { get; set; }

        public ChangesetViewModel Changeset { get; set; }
    }
}
