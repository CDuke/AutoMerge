using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMerge.Events;
using AutoMerge.Prism.Command;
using AutoMerge.Prism.Events;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common.Internal;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Shell.Interop;
using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
    public sealed class BranchesViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly IEventAggregator _eventAggregator;
        private ChangesetService _changesetService;
        private Workspace _workspace;

        private ChangesetViewModel _changeset;
        private bool _merging;

        public BranchesViewModel(ILogger logger)
            : base(logger)
        {
            Title = Resources.BrancheSectionName;
            IsVisible = true;
            IsExpanded = true;
            IsBusy = false;

            MergeCommand = new DelegateCommand<MergeMode?>(MergeExecute, m => MergeCanEcexute());
            SelectWorkspaceCommand = new DelegateCommand<Workspace>(SelectWorkspaceExecute);
            OpenSourceControlExplorerCommand = new DelegateCommand(OpenSourceControlExplorerExecute, OpenSourceControlExplorerCanExecute);

            _eventAggregator = EventAggregatorFactory.Get();
            _merging = false;
        }

        public ObservableCollection<MergeInfoViewModel> Branches
        {
            get
            {
                return _branches;
            }
            set
            {
                _branches = value;
                RaisePropertyChanged("Branches");
            }
        }
        private ObservableCollection<MergeInfoViewModel> _branches;

        private MergeInfoViewModel _selectedBranch;

        public MergeInfoViewModel SelectedBranch
        {
            get
            {
                return _selectedBranch;
            }
            set
            {
                _selectedBranch = value;
                RaisePropertyChanged("SelectedBranch");
            }
        }

        public DelegateCommand<MergeMode?> MergeCommand { get; private set; }

        public MergeOption MergeOption
        {
            get { return _mergeOption; }
            set
            {
                _mergeOption = value;
                RaisePropertyChanged("MergeOption");
            }
        }
        private MergeOption _mergeOption;

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                _errorMessage = value;
                RaisePropertyChanged("ErrorMessage");
            }
        }
        private string _errorMessage;

        public Workspace Workspace
        {
            get
            {
                return _workspace;
            }
            set
            {
                _workspace = value;
                RaisePropertyChanged("Workspace");
            }
        }

        private ObservableCollection<Workspace> _workspaces;

        public ObservableCollection<Workspace> Workspaces
        {
            get
            {
                return _workspaces;
            }
            set
            {
                _workspaces = value;
                RaisePropertyChanged("Workspaces");
            }
        }

        private bool _showWorkspaceChooser;
        public bool ShowWorkspaceChooser
        {
            get
            {
                return _showWorkspaceChooser;
            }
            set
            {
                _showWorkspaceChooser = value;
                RaisePropertyChanged("ShowWorkspaceChooser");
            }
        }

        private MergeMode _mergeMode;
        public MergeMode MergeMode
        {
            get
            {
                return _mergeMode;
            }
            set
            {
                _mergeMode = value;
                RaisePropertyChanged("MergeMode");
            }
        }

        private ObservableCollection<MergeMode> _mergeModes;

        public ObservableCollection<MergeMode> MergeModes
        {
            get
            {
                return _mergeModes;
            }
            set
            {
                _mergeModes = value;
                RaisePropertyChanged("MergeModes");
            }
        }

        public DelegateCommand<Workspace> SelectWorkspaceCommand { get; set; }

        public DelegateCommand OpenSourceControlExplorerCommand { get; set; }

        public async override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            Logger.Debug("Start initilize branches section");
            base.Initialize(sender, e);

            var tfs = Context.TeamProjectCollection;
            var versionControl = tfs.GetService<VersionControlServer>();
            SubscribeWorkspaceChanges(versionControl);

            _changesetService = new ChangesetService(versionControl, Context.TeamProjectName);

            _eventAggregator.GetEvent<SelectChangesetEvent>()
                .Subscribe(OnSelectedChangeset);
            _eventAggregator.GetEvent<BranchSelectedChangedEvent>()
                .Subscribe(OnBranchSelectedChanged);

            if (e.Context == null)
            {
                Workspaces =
                    new ObservableCollection<Workspace>(versionControl.QueryWorkspaces(null,
                        tfs.AuthorizedIdentity.UniqueName, Environment.MachineName));
                if (Workspaces.Count > 0)
                {
                    Workspace = WorkspaceHelper.GetWorkspace(versionControl, Workspaces);
                    ShowWorkspaceChooser = Workspaces.Count > 1;
                }
                else
                {
                    Workspace = null;
                }

                MergeModes = new ObservableCollection<MergeMode>
                {
                    MergeMode.Merge,
                    MergeMode.MergeAndCheckIn
                };
                MergeMode = Settings.Instance.LastMergeOperation;

                await RefreshAsync();
            }
            else
            {
                RestoreContext(e);
            }

            Logger.Debug("End initialize branches section");
        }

        private void OnBranchSelectedChanged(MergeInfoViewModel obj)
        {
            MergeCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Refresh the changeset data asynchronously.
        /// </summary>
        protected override async Task RefreshAsync()
        {
            var changeset = _changeset;
            Logger.Debug("Start refresh branches section for changeset {0} ...",
                changeset == null ? "null" : changeset.ChangesetId.ToString(CultureInfo.InvariantCulture));

            string errorMessage = null;
            if (Workspaces.Count == 0)
            {
                errorMessage = "Workspaces not found";
            }
            errorMessage = errorMessage ?? CalculateError(changeset);
            if (changeset == null || !string.IsNullOrEmpty(errorMessage))
            {
                ErrorMessage = errorMessage;
                Branches = new ObservableCollection<MergeInfoViewModel>();
            }
            else
            {
                Logger.Info("Getting branches for changeset {0} ...",
                    changeset.ChangesetId.ToString(CultureInfo.InvariantCulture));
                var branches = await Task.Run(() => GetBranches(Context, changeset));
                Logger.Info("Getting branches end for changeset {0}",
                    changeset.ChangesetId.ToString(CultureInfo.InvariantCulture));

                // Selected changeset in sequence
                if (changeset.ChangesetId == _changeset.ChangesetId)
                {
                    Branches = branches;
                    ErrorMessage = branches.Count <= 1 ? "Target branches not found" : null;
                    MergeCommand.RaiseCanExecuteChanged();
                }
            }
            Logger.Debug("End refresh branches section for changeset {0}",
                changeset == null ? "null" : changeset.ChangesetId.ToString(CultureInfo.InvariantCulture));
        }

        private static string CalculateError(ChangesetViewModel changeset)
        {
            if (changeset == null)
                return "Changeset not selected";

            if (changeset.Branches.IsNullOrEmpty())
                return "Changeset has not branch";

            if (changeset.Branches.Count > 1)
                return string.Format("Changeset has {0} branches. Merge not possible.", changeset.Branches.Count);

            return null;
        }

        private void OnSelectedChangeset(ChangesetViewModel changeset)
        {
            _changeset = changeset;
            Refresh();
        }

        private ObservableCollection<MergeInfoViewModel> GetBranches(ITeamFoundationContext context, ChangesetViewModel changesetViewModel)
        {
            if (context == null)
                return new ObservableCollection<MergeInfoViewModel>();
            var tfs = context.TeamProjectCollection;
            var versionControl = tfs.GetService<VersionControlServer>();

            var result = new ObservableCollection<MergeInfoViewModel>();

            var workspace = _workspace;

            var changesetService = _changesetService;

            var changes = changesetService.GetChanges(changesetViewModel.ChangesetId);

            var sourceTopFolder = CalculateTopFolder(changes);
            var mergesRelationships = GetMergesRelationships(sourceTopFolder, versionControl);

            if (mergesRelationships.Count > 0)
            {
                var sourceBranchIdentifier = changesetViewModel.Branches.Select(b => new ItemIdentifier(b)).First();

                var sourceBranch = sourceBranchIdentifier.Item;

                var trackMerges = versionControl.TrackMerges(new[] { changesetViewModel.ChangesetId },
                    new ItemIdentifier(sourceTopFolder),
                    mergesRelationships.ToArray(),
                    null);

                var changesetVersionSpec = new ChangesetVersionSpec(changesetViewModel.ChangesetId);

                var branchValidator = new BranchValidator(workspace, trackMerges);
                var branchFactory = new BranchFactory(sourceBranch, sourceTopFolder,
                    changesetVersionSpec, branchValidator,
                    _eventAggregator);

                var sourceBranchInfo = versionControl.QueryBranchObjects(sourceBranchIdentifier, RecursionType.None)[0];
                if (sourceBranchInfo.Properties != null && sourceBranchInfo.Properties.ParentBranch != null
                    && !sourceBranchInfo.Properties.ParentBranch.IsDeleted)
                {
                    var targetBranch = sourceBranchInfo.Properties.ParentBranch;
                    var targetPath = GetTargetPath(mergesRelationships, targetBranch);
                    if (targetPath != null)
                    {
                        var mergeInfo = branchFactory.CreateTargetBranchInfo(targetBranch, targetPath);
                        mergeInfo._checked = mergeInfo.ValidationResult == BranchValidationResult.Success;

                        result.Add(mergeInfo);
                    }
                }

                var currentBranchInfo = branchFactory.CreateSourceBranch();
                result.Add(currentBranchInfo);

                if (sourceBranchInfo.ChildBranches != null)
                {
                    var childBranches = sourceBranchInfo.ChildBranches.Where(b => !b.IsDeleted)
                        .Reverse();
                    foreach (var childBranch in childBranches)
                    {
                        var targetBranch = childBranch;
                        var targetPath = GetTargetPath(mergesRelationships, targetBranch);
                        if (targetPath != null)
                        {
                            var mergeInfo = branchFactory.CreateTargetBranchInfo(targetBranch, targetPath);
                            result.Add(mergeInfo);
                        }
                    }
                }

                // Feature branch
                if (mergesRelationships.Count > 0)
                {
                    var changetIds =
                        mergesRelationships.Select(r => r.Version).Cast<ChangesetVersionSpec>().Select(c => c.ChangesetId).ToArray();
                    var branches = _changesetService.GetAssociatedBranches(changetIds);

                    foreach (var mergesRelationship in mergesRelationships)
                    {
                        var targetBranch = branches.FirstOrDefault(b => IsTargetPath(mergesRelationship, b));
                        if (targetBranch != null)
                        {
                            var mergeInfo = branchFactory.CreateTargetBranchInfo(targetBranch, mergesRelationship);
                            result.Add(mergeInfo);
                        }
                    }
                }
            }

            return result;
        }

        private static List<ItemIdentifier> GetMergesRelationships(string sourceTopFolder, VersionControlServer versionControl)
        {
            return versionControl.QueryMergeRelationships(sourceTopFolder)
                .Where(r => !r.IsDeleted)
                .ToList();
        }

        private TrackMergeInfo GetTrackMergeInfo(VersionControlServer versionControl,
            IEnumerable<ExtendedMerge> allTrackMerges,
            string sourcePath)
        {
            var result = new TrackMergeInfo
            {
                FromOriginalToSourceBranches = new List<string>(),
            };
            var trackMerges = allTrackMerges.Where(m => m.TargetItem.Item == sourcePath).ToArray();
            if (!trackMerges.IsNullOrEmpty())
            {
                var changesetIds = trackMerges.Select(t => t.SourceChangeset.ChangesetId).ToArray();
                var mergeSourceBranches = _changesetService.GetAssociatedBranches(changesetIds)
                    .Select(b => b.Item)
                    .Distinct()
                    .ToArray();

                if (mergeSourceBranches.Length == 1)
                {
                    result.FromOriginalToSourceBranches.Add(mergeSourceBranches[0]);

                    var sourceFolder = trackMerges.First().SourceItem.Item.ServerItem;
                    var comment = trackMerges.First().SourceChangeset.Comment;
                    if (trackMerges.Length > 1)
                    {
                        foreach (var merge in trackMerges.Skip(1))
                            sourceFolder = FindShareFolder(sourceFolder, merge.SourceItem.Item.ServerItem);
                        comment = "source changeset has several comments";
                    }

                    var sourceMergesRelationships = versionControl.QueryMergeRelationships(sourceFolder)
                    .Where(r => !r.IsDeleted)
                    .ToArray();

                    var sourceTrackMerges = versionControl.TrackMerges(changesetIds,
                        new ItemIdentifier(sourceFolder),
                        sourceMergesRelationships,
                        null);

                    var sourceTrackMergeInfo = GetTrackMergeInfo(versionControl, sourceTrackMerges, sourceFolder);

                    if (!sourceTrackMergeInfo.FromOriginalToSourceBranches.IsNullOrEmpty())
                    {
                        result.FromOriginalToSourceBranches.AddRange(sourceTrackMergeInfo.FromOriginalToSourceBranches);
                        result.OriginalComment = sourceTrackMergeInfo.OriginalComment;
                        result.OriginaBranch = sourceTrackMergeInfo.OriginaBranch;
                    }
                    else
                    {
                        result.OriginalComment = comment;
                        result.OriginaBranch = mergeSourceBranches[0];
                    }
                }
                else
                {
                    result.FromOriginalToSourceBranches.Add("multi");
                    result.OriginaBranch = "multi";
                    result.OriginalComment = "source changeset has several comments";
                }
            }

            return result;
        }

        private static ItemIdentifier GetTargetPath(ICollection<ItemIdentifier> mergesRelationships, ItemIdentifier targetBranch)
        {
            if (mergesRelationships == null || mergesRelationships.Count == 0)
                return null;
            var targetItem = mergesRelationships.FirstOrDefault(m => IsTargetPath(m, targetBranch));
            if (targetItem != null)
            {
                mergesRelationships.Remove(targetItem);
                return targetItem;
            }

            return null;
        }

        private static bool IsTargetPath(ItemIdentifier mergeRelations, ItemIdentifier branch)
        {
            return mergeRelations.Item.Contains(branch.Item);
        }

        private static string CalculateTopFolder(IList<Change> changes)
        {
            if (changes == null || changes.Count == 0)
                throw new ArgumentNullException("changes");

            string topFolder = null;
            if (changes.Count == 1 &&
                (changes[0].ChangeType.HasFlag(ChangeType.Edit)
                && !changes[0].ChangeType.HasFlag(ChangeType.Add)
                && !changes[0].ChangeType.HasFlag(ChangeType.Branch)))
            {
                topFolder = changes[0].Item.ServerItem;
            }
            else
            {
                foreach (var change in changes)
                {
                    if (topFolder != null)
                    {
                        if (change.Item.ServerItem.Contains(topFolder) && change.Item.ServerItem != topFolder)
                            continue;
                    }

                    var changeFolder = ExtractFolder(change.ChangeType, change.Item);

                    topFolder = FindShareFolder(topFolder, changeFolder);
                }
            }

            if (topFolder != null && topFolder.EndsWith("/"))
            {
                topFolder = topFolder.Substring(0, topFolder.Length - 1);
            }

            return topFolder;
        }

        private static string FindShareFolder(string topFolder, string changeFolder)
        {
            if ((topFolder == null) || topFolder.Contains(changeFolder))
            {
                return changeFolder;
            }
            const string rootFolder = "$/";
            var folder = topFolder;
            while (folder != rootFolder)
            {
                folder = ExtractParentFolder(folder);
                if (folder != null && changeFolder.Contains(folder))
                    break;
            }

            return folder == rootFolder ? folder + "/" : folder;
        }

        private static string ExtractFolder(ChangeType changeType, Item item)
        {
            if (changeType.HasFlag(ChangeType.Add) && item.ItemType == ItemType.Folder)
                return ExtractParentFolder(item.ServerItem);

            return item.ItemType == ItemType.Folder
                ? item.ServerItem
                : ExtractParentFolder(item.ServerItem);
        }

        private static string ExtractParentFolder(string serverItem)
        {
            if (string.IsNullOrWhiteSpace(serverItem))
                throw new ArgumentNullException("serverItem");

            if (serverItem.EndsWith("/"))
                serverItem = serverItem.Substring(0, serverItem.Length - 1);

            var lastPosDelimiter = serverItem.LastIndexOf('/');
            if (lastPosDelimiter < 0)
                throw new InvalidOperationException(string.Format("Folder delimiter for {0} not found", serverItem));

            return serverItem.Substring(0, lastPosDelimiter + 1);
        }

        private async void MergeExecute(MergeMode? mergeMode)
        {
            Logger.Info("Merging start...");
            if (!mergeMode.HasValue)
                return;
            MergeMode = mergeMode.Value;
            Settings.Instance.LastMergeOperation = mergeMode.Value;
            switch (mergeMode)
            {
                case MergeMode.Merge:
                    await MergeAndCheckInExecute(false);
                    break;
                case MergeMode.MergeAndCheckIn:
                    await MergeAndCheckInExecute(true);
                    break;
            }
            Logger.Info("Merging end");
        }

        private async Task MergeAndCheckInExecute(bool checkInIfSuccess)
        {
            try
            {
                IsBusy = true;
                _merging = true;

                MergeCommand.RaiseCanExecuteChanged();

                var result = await Task.Run(() => MergeExecuteInternal(checkInIfSuccess));
                var notifications = new List<Notification>();
                var notCheckedIn = new List<MergeResultModel>(result.Count);
                ClearNotifications();
                foreach (var resultModel in result)
                {
                    var notification = new Notification
                    {
                        NotificationType = NotificationType.Information,
                        Message = string.Empty
                    };
                    var mergePath = string.Format("{0} -> {1}",
                        BranchHelper.GetShortBranchName(resultModel.BranchInfo.SourceBranch),
                        BranchHelper.GetShortBranchName(resultModel.BranchInfo.TargetBranch));
                    switch (resultModel.MergeResult)
                    {
                        case MergeResult.CheckInEvaluateFail:
                            notification.NotificationType = NotificationType.Error;
                            notification.Message = "Check In evaluate failed";
                            break;
                        case MergeResult.CheckInFail:
                            notification.NotificationType = NotificationType.Error;
                            notification.Message = "Check In failed";
                            break;
                        case MergeResult.NothingMerge:
                            notification.NotificationType = NotificationType.Warning;
                            notification.Message = "Nothing merged";
                            break;
                        case MergeResult.HasConflicts:
                            notification.NotificationType = NotificationType.Error;
                            notification.Message = "Has conflicts";
                            notCheckedIn.Add(resultModel);
                            break;
                        case MergeResult.CanNotGetLatest:
                            notification.NotificationType = NotificationType.Error;
                            notification.Message = "Can not get lates";
                            break;
                        case MergeResult.TryRestoreFile:
                            notification.NotificationType = NotificationType.Warning;
                            notification.Message = "Some files were restored";
                            break;
                        case MergeResult.Merged:
                            notification.NotificationType = NotificationType.Information;
                            notification.Message = "Files merge but not chekced in";
                            notCheckedIn.Add(resultModel);
                            break;
                        case MergeResult.CheckIn:
                            var changesetId = resultModel.ChangesetId.Value;
                            notification.NotificationType = NotificationType.Information;
                            notification.Message = string.Format("Merge is successful. [Changeset {0}] created.", changesetId);
                            notification.Command = new DelegateCommand(() => ViewChangesetDetailsExecute(changesetId));
                            break;
                    }
                    notification.Message = string.Format("{0}: {1}", mergePath, notification.Message);
                    if (!string.IsNullOrEmpty(notification.Message))
                        notifications.Add(notification);
                }

                if (notCheckedIn.Count > 0)
                {
                    OpenPendingChanges(notCheckedIn);
                }
                else
                {
                    _eventAggregator.GetEvent<MergeCompleteEvent>().Publish(true);
                }

                foreach (var notification in notifications)
                {
                    ShowNotification(notification.Message, notification.NotificationType, NotificationFlags.RequiresConfirmation, notification.Command);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while merging", ex);
                ClearNotifications();
                ShowNotification(ex.Message, NotificationType.Error);
            }
            finally
            {
                IsBusy = false;
                _merging = false;
                MergeCommand.RaiseCanExecuteChanged();
            }
        }

        private void OpenPendingChanges(ICollection<MergeResultModel> resultModels)
        {
            var comment = string.Empty;
            var pendingChanges = new List<PendingChange>(20);
            // all results must have identical workItems
            var workItemIds = resultModels.First().WorkItemIds;

            var conflictsPath = new List<string>();
            foreach (var resultModel in resultModels)
            {
                if (string.IsNullOrEmpty(comment))
                    comment = resultModel.Comment;
                else
                    comment = comment + ";" + resultModel.Comment;

                if (resultModel.PendingChanges != null && resultModel.PendingChanges.Count > 0)
                    pendingChanges.AddRange(resultModel.PendingChanges);

                if (resultModel.MergeResult == MergeResult.HasConflicts)
                    conflictsPath.Add(resultModel.BranchInfo.TargetPath);
            }

            if (conflictsPath.Count > 0)
                InvokeResolveConflictsPage(_workspace, conflictsPath.ToArray());
            OpenPendingChanges(pendingChanges, workItemIds, comment);
        }

        private void OpenPendingChanges(List<PendingChange> pendingChanges, List<int> workItemIds, string comment)
        {
            var teamExplorer = ServiceProvider.GetService<ITeamExplorer>();
            var pendingChangesPage = (TeamExplorerPageBase)teamExplorer.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
            var model = (IPendingCheckin)pendingChangesPage.Model;
            model.PendingChanges.Comment = comment;
            model.PendingChanges.CheckedPendingChanges = pendingChanges.ToArray();

            if (Workspaces.Count > 1)
            {
                var modelType = model.GetType();
                var workspaceProperty = modelType.GetProperty("Workspace");
                workspaceProperty.SetValue(model, Workspace);
            }

            if (workItemIds != null && workItemIds.Count > 0)
            {
                var modelType = model.GetType();
                var method = modelType.GetMethod("AddWorkItemsByIdAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                var workItemsIdsArray = workItemIds.ToArray();
                method.Invoke(model, new object[] { workItemsIdsArray, 1 /* Add */});
            }
        }

        private List<MergeResultModel> MergeExecuteInternal(bool checkInIfSuccess)
        {
            var result = new List<MergeResultModel>();
            var context = Context;
            var tfs = context.TeamProjectCollection;
            var versionControl = tfs.GetService<VersionControlServer>();

            var workspace = _workspace;

            var changesetId = _changeset.ChangesetId;
            var changesetService = _changesetService;
            var changeset = changesetService.GetChangeset(changesetId);
            var mergeOption = _mergeOption;
            var workItemStore = tfs.GetService<WorkItemStore>();
            var workItemIds = changeset.AssociatedWorkItems != null
                ? changeset.AssociatedWorkItems.Select(w => w.Id).ToList()
                : new List<int>();

            var mergeRelationships = new List<ItemIdentifier>();

            foreach (var change in changeset.Changes)
            {
                if (!change.ChangeType.HasFlag(ChangeType.Add) && change.Item.ItemType == ItemType.File)
                {
                    var changeRelationShips = versionControl.QueryMergeRelationships(change.Item.ServerItem);
                    if (changeRelationShips != null)
                    {
                        mergeRelationships.AddRange(changeRelationShips.Where(r => !r.IsDeleted));
                    }
                }
            }

            var commentFormater = new CommentFormater(Settings.Instance.CommentFormat);
            foreach (var mergeInfo in _branches.Where(b => b.Checked))
            {
                var mergeResultModel = new MergeResultModel
                {
                    BranchInfo = mergeInfo,
                };

                var mergeResult = MergeToBranch(mergeInfo, mergeOption, mergeRelationships, workspace);
                var targetPendingChanges = GetPendingChanges(mergeInfo.TargetPath, workspace);
                if (targetPendingChanges.Count == 0)
                {
                    mergeResult = MergeResult.NothingMerge;
                }
                if (targetPendingChanges.Any(change => change.IsUndelete))
                {
                    mergeResult = MergeResult.TryRestoreFile;
                }
                mergeResultModel.MergeResult = mergeResult;
                mergeResultModel.PendingChanges = targetPendingChanges;
                mergeResultModel.WorkItemIds = workItemIds;

                var trackMergeInfo = GetTrackMergeInfo(mergeInfo, changeset, versionControl);
                var comment = commentFormater.Format(trackMergeInfo, mergeInfo.TargetBranch, mergeOption);
                mergeResultModel.Comment = comment;

                result.Add(mergeResultModel);
                if (checkInIfSuccess && mergeResultModel.MergeResult == MergeResult.Merged)
                {
                    var checkInResult = CheckIn(mergeResultModel.PendingChanges, comment, workspace, workItemIds, changeset.PolicyOverride, workItemStore);
                    mergeResultModel.ChangesetId = checkInResult.ChangesetId;
                    mergeResultModel.MergeResult = checkInResult.CheckinResult;
                }
            }

            return result;
        }

        private TrackMergeInfo GetTrackMergeInfo(MergeInfoViewModel mergeInfo, Changeset changeset, VersionControlServer versionControl)
        {
            var mergesRelationships = GetMergesRelationships(mergeInfo.SourcePath, versionControl);
            var trackMerges = versionControl.TrackMerges(new[] {changeset.ChangesetId},
                new ItemIdentifier(mergeInfo.SourcePath),
                mergesRelationships.ToArray(),
                null);

            var trackMergeInfo = GetTrackMergeInfo(versionControl,
                trackMerges, mergeInfo.SourcePath);
            trackMergeInfo.FromOriginalToSourceBranches.Reverse();
            trackMergeInfo.SourceComment = changeset.Comment;
            trackMergeInfo.SourceBranch = mergeInfo.SourceBranch;
            trackMergeInfo.SourceChangesetId = changeset.ChangesetId;
            trackMergeInfo.SourceWorkItemIds = changeset.AssociatedWorkItems != null
                ? changeset.AssociatedWorkItems.Select(w => (long) w.Id).ToList()
                : new List<long>();
            trackMergeInfo.OriginaBranch = trackMergeInfo.OriginaBranch ?? trackMergeInfo.SourceBranch;
            trackMergeInfo.OriginalComment = trackMergeInfo.OriginalComment ?? trackMergeInfo.SourceComment;
            return trackMergeInfo;
        }

        private static CheckInResult CheckIn(IReadOnlyCollection<PendingChange> targetPendingChanges, string comment,
            Workspace workspace, IReadOnlyCollection<int> workItemIds, PolicyOverrideInfo policyOverride, WorkItemStore workItemStore)
        {
            var result = new CheckInResult();

            // Another user can update workitem. Need re-read before update.
            var workItems = GetWorkItemCheckinInfo(workItemIds, workItemStore);

            var evaluateCheckIn = workspace.EvaluateCheckin2(CheckinEvaluationOptions.All,
                targetPendingChanges,
                comment,
                null,
                workItems);

            var skipPolicyValidate = !policyOverride.PolicyFailures.IsNullOrEmpty();
            if (!CanCheckIn(evaluateCheckIn, skipPolicyValidate))
            {
                result.CheckinResult = MergeResult.CheckInEvaluateFail;
            }

            var changesetId = workspace.CheckIn(targetPendingChanges.ToArray(), null, comment,
                null, workItems, policyOverride);
            if (changesetId > 0)
            {
                result.ChangesetId = changesetId;
                result.CheckinResult = MergeResult.CheckIn;
            }
            else
            {
                result.CheckinResult = MergeResult.CheckInFail;
            }
            return result;
        }

        private static MergeResult MergeToBranch(MergeInfoViewModel mergeInfoeViewModel, MergeOption mergeOption,
            IEnumerable<ItemIdentifier> mergeRelationships, Workspace workspace)
        {
            var source = mergeInfoeViewModel.SourcePath;
            var target = mergeInfoeViewModel.TargetPath;
            var version = mergeInfoeViewModel.ChangesetVersionSpec;

            if (!GetLatest(target, mergeRelationships, workspace))
            {
                return MergeResult.CanNotGetLatest;
            }

            if (!Merge(source, target, version, mergeOption, workspace))
            {
                return MergeResult.HasConflicts;
            }

            return MergeResult.Merged;
        }

        private static List<PendingChange> GetPendingChanges(string target, Workspace workspace)
        {
            var allPendingChanges = workspace.GetPendingChangesEnumerable(target, RecursionType.Full);
            var targetPendingChanges = allPendingChanges
                .Where(p => p.IsMerge && p.ServerItem.Contains(target))
                .ToList();
            return targetPendingChanges;
        }

        private static bool GetLatest(string targetPath, IEnumerable<ItemIdentifier> mergeRelationships, Workspace workspace)
        {
            var getLatestFiles = new List<string>();
            foreach (var mergeRelationship in mergeRelationships)
            {
                if (mergeRelationship.Item.StartsWith(targetPath))
                    getLatestFiles.Add(mergeRelationship.Item);
            }

            var getLatestFilesArray = getLatestFiles.ToArray();
            if (getLatestFilesArray.Length > 0)
            {
                const RecursionType recursionType = RecursionType.None;
                var getLatestResult = workspace.Get(getLatestFilesArray, VersionSpec.Latest, recursionType, GetOptions.None);
                if (!getLatestResult.NoActionNeeded)
                {
                    // HACK.
                    getLatestResult = workspace.Get(getLatestFilesArray, VersionSpec.Latest, recursionType, GetOptions.None);
                    if (!getLatestResult.NoActionNeeded)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Merge(string source, string target, ChangesetVersionSpec version, MergeOption mergeOption,
            Workspace workspace)
        {
            var mergeOptions = ToTfsMergeOptions(mergeOption);
            var status = workspace.Merge(source, target, version, version, LockLevel.None, RecursionType.Full, mergeOptions);

            if (HasConflicts(status))
            {
                var conflicts = AutoResolveConflicts(workspace, target, mergeOption);
                if (!conflicts.IsNullOrEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        private static MergeOptionsEx ToTfsMergeOptions(MergeOption mergeOption)
        {
            switch (mergeOption)
            {
                case MergeOption.KeepTarget:
                    return MergeOptionsEx.AlwaysAcceptMine;
                default:
                    return MergeOptionsEx.None;
            }
        }

        public bool MergeCanEcexute()
        {
            return !_merging && _branches != null && _branches.Any(b => b.Checked);
        }

        private static bool HasConflicts(GetStatus mergeStatus)
        {
            return !mergeStatus.NoActionNeeded && mergeStatus.NumConflicts > 0;
        }

        private static bool ManualResolveConflicts(Workspace workspace, Conflict[] conflicts)
        {
            if (conflicts.IsNullOrEmpty())
                return true;

            foreach (var conflict in conflicts)
            {
                if (workspace.MergeContent(conflict, true))
                {
                    conflict.Resolution = Resolution.AcceptMerge;
                    workspace.ResolveConflict(conflict);
                }
                if (!conflict.IsResolved)
                {
                    return false;
                }
            }

            return true;
        }

        private static Conflict[] AutoResolveConflicts(Workspace workspace, string targetPath, MergeOption mergeOption)
        {
            var targetPaths = new[] {targetPath};
            var conflicts = workspace.QueryConflicts(targetPaths, true);
            if (conflicts.IsNullOrEmpty())
                return null;

            foreach (var conflict in conflicts)
            {
                TryResolve(workspace, conflict, mergeOption);
            }

            conflicts = workspace.QueryConflicts(targetPaths, true);
            if (conflicts.IsNullOrEmpty())
                return null;

            workspace.AutoResolveValidConflicts(conflicts, AutoResolveOptions.AllSilent);

            return workspace.QueryConflicts(targetPaths, true);
        }

        private static void TryResolve(Workspace workspace, Conflict conflict, MergeOption mergeOption)
        {
            if (mergeOption == MergeOption.KeepTarget)
            {
                conflict.Resolution = Resolution.AcceptYours;
                //conflict.IsResolved = true;
                workspace.ResolveConflict(conflict);
            }
            if (mergeOption == MergeOption.OverwriteTarget)
            {
                conflict.Resolution = Resolution.AcceptTheirs;
                //conflict.IsResolved = true;
                workspace.ResolveConflict(conflict);
            }
        }


        private static WorkItemCheckinInfo[] GetWorkItemCheckinInfo(IReadOnlyCollection<int> workItemIds, WorkItemStore workItemStore)
        {

            var result = new List<WorkItemCheckinInfo>(workItemIds.Count);
            foreach (var workItemId in workItemIds)
            {
                var workItem = workItemStore.GetWorkItem(workItemId);
                var workItemCheckinInfo = new WorkItemCheckinInfo(workItem, WorkItemCheckinAction.Associate);
                result.Add(workItemCheckinInfo);
            }

            return result.ToArray();
        }

        private static bool CanCheckIn(CheckinEvaluationResult checkinEvaluationResult, bool skipPolicy)
        {
            var result = checkinEvaluationResult.Conflicts.IsNullOrEmpty()
                && checkinEvaluationResult.NoteFailures.IsNullOrEmpty()
                && checkinEvaluationResult.PolicyEvaluationException == null;

            if (!skipPolicy)
                result &= checkinEvaluationResult.PolicyFailures.IsNullOrEmpty();
            return result;
        }

        private static void InvokeResolveConflictsPage(Workspace workspace, string[] targetPath)
        {
            var versionControlAssembly = Assembly.Load("Microsoft.VisualStudio.TeamFoundation.VersionControl");
            if (versionControlAssembly == null)
                return;

            var rcMgr = versionControlAssembly.GetType("Microsoft.VisualStudio.TeamFoundation.VersionControl.ResolveConflictsManager");
            if (rcMgr == null)
                return;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var mi = rcMgr.GetMethod("Initialize", flags);
            var instantiatedType = Activator.CreateInstance(rcMgr, flags, null, null, null);
            mi.Invoke(instantiatedType, null);

            var resolveConflictsMethod = rcMgr.GetMethod("ResolveConflicts", BindingFlags.NonPublic | BindingFlags.Instance);
            resolveConflictsMethod.Invoke(instantiatedType,
                new object[] { workspace, targetPath, true, false });
        }

        private void SelectWorkspaceExecute(Workspace workspace)
        {
            Workspace = workspace;
            Refresh();
        }

        private void SubscribeWorkspaceChanges(VersionControlServer versionControlServer)
        {
            versionControlServer.CreatedWorkspace += RefreshWorkspaces;
            versionControlServer.UpdatedWorkspace += RefreshWorkspaces;
            versionControlServer.DeletedWorkspace += RefreshWorkspaces;
        }

        private void RefreshWorkspaces(object sender, WorkspaceEventArgs e)
        {
            var tfs = Context.TeamProjectCollection;
            var versionControl = tfs.GetService<VersionControlServer>();

            Workspaces = new ObservableCollection<Workspace>(versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName));
            if (Workspaces.Count > 0)
            {
                Workspace = Workspaces[0];
                ShowWorkspaceChooser = Workspaces.Count > 1;
            }
            else
            {
                Workspace = null;
            }
            Refresh();
        }

        private void OpenSourceControlExplorerExecute()
        {
            // Using HACK.
            // Get any service which contain DTE object.

            var s = ServiceProvider.GetService<SVsSourceControl>() as SourceControl2;
            if (s != null)
            {
                dynamic ext = s.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");
                if (ext != null)
                {
                    var explorer = ext.Explorer;
                    if (explorer != null)
                    {
                        explorer.Navigate(SelectedBranch.TargetPath);
                    }
                }
            }
        }

        private bool OpenSourceControlExplorerCanExecute()
        {
            return SelectedBranch != null && !string.IsNullOrEmpty(SelectedBranch.TargetPath);
        }

        private void ViewChangesetDetailsExecute(int changesetId)
        {
            TeamExplorerUtils.Instance.NavigateToPage(TeamExplorerPageIds.ChangesetDetails, ServiceProvider, changesetId);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<SelectChangesetEvent>().Unsubscribe(OnSelectedChangeset);
                _eventAggregator.GetEvent<BranchSelectedChangedEvent>().Unsubscribe(OnBranchSelectedChanged);
            }

            var tfs = Context.TeamProjectCollection;
            if (tfs != null)
            {
                var versionControl = tfs.GetService<VersionControlServer>();
                if (versionControl != null)
                {
                    versionControl.CreatedWorkspace -= RefreshWorkspaces;
                    versionControl.UpdatedWorkspace -= RefreshWorkspaces;
                    versionControl.DeletedWorkspace -= RefreshWorkspaces;
                }
            }
        }

        public override void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            var context = new BranchesViewModelContext
            {
                Branches = Branches,
                Changeset = _changeset,
                ErrorMessage = ErrorMessage,
                MergeMode = MergeMode,
                MergeModes = MergeModes,
                MergeOption = MergeOption,
                SelectedBranch = SelectedBranch,
                ShowWorkspaceChooser = ShowWorkspaceChooser,
                Workspace = Workspace,
                Workspaces = Workspaces
            };

            e.Context = context;
        }

        private void RestoreContext(SectionInitializeEventArgs e)
        {
            var context = (BranchesViewModelContext)e.Context;
            _changeset = context.Changeset;
            Branches = context.Branches;
            ErrorMessage = context.ErrorMessage;
            MergeMode = context.MergeMode;
            MergeModes = context.MergeModes;
            MergeOption = context.MergeOption;
            SelectedBranch = context.SelectedBranch;
            ShowWorkspaceChooser = context.ShowWorkspaceChooser;
            Workspace = context.Workspace;
            Workspaces = context.Workspaces;
        }
    }
}
