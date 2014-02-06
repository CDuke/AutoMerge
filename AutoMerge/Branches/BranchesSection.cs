using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMerge.Base;
using AutoMerge.Events;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace AutoMerge
{
	[TeamExplorerSection(SectionId, AutoMergePage.PageId, 20)]
	public class BranchesSection : TeamExplorerBaseSection
	{
		private enum MergeResult
		{
			Success,
			NothingMerge,
			CheckInFail,
			CheckInEvaluateFail,
			UnresolvedConflicts,
			PartialSuccess
		}

		private enum CheckInResult
		{
			Success,
			NothingMerge,
			CheckInFail,
			CheckInEvaluateFail
		}

		private class BranchContext
		{
			public int ChangesetId { get; set; }

			public ObservableCollection<MergeInfoViewModel> Branches { get; set; }
		}

		private readonly IEventAggregator _eventAggregator;
		public const string SectionId = "36BF6F52-F4AC-44A0-9985-817B2A65B3B0";

		private int _changesetId;

		/// <summary>
		/// Constructor.
		/// </summary>
		public BranchesSection()
		{
			Title = Resources.BrancheSectionName;
			IsVisible = true;
			IsExpanded = true;
			IsBusy = false;
			SectionContent = new BranchesView();
			View.ParentSection = this;

			MergeCommand = new DelegateCommand(MergeExecute, MergeCanEcexute);

			_eventAggregator = EventAggregatorFactory.Get();
		}

		/// <summary>
		/// Get the view.
		/// </summary>
		protected BranchesView View
		{
			get { return SectionContent as BranchesView; }
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
				RaisePropertyChanged(() => Branches);
			}
		}
		private ObservableCollection<MergeInfoViewModel> _branches;

		public bool MergeEnabled
		{
			get
			{
				return _mergeEnabled;
			}
			set
			{
				_mergeEnabled = value;
				RaisePropertyChanged(() => MergeEnabled);
			}
		}
		private bool _mergeEnabled;

		public ICommand MergeCommand { get; private set; }

		public async override void Initialize(object sender, SectionInitializeEventArgs e)
		{
			base.Initialize(sender, e);

			if (e.Context != null && e.Context is BranchContext)
			{
				// Restore the context instead of refreshing
				var context = (BranchContext)e.Context;
				_changesetId = context.ChangesetId;
				Branches = context.Branches;
			}
			else
			{
				// Kick off the initial refresh
				await RefreshAsync();
			}

			_eventAggregator.GetEvent<SelectChangesetEvent>()
				.Subscribe(OnSelectedChangeset);
			_eventAggregator.GetEvent<BranchSelectedChangedEvent>()
				.Subscribe(OnBranchSelectedChanged);
		}

		private void OnBranchSelectedChanged(MergeInfoViewModel obj)
		{
			ValidateMergeEnabled();
		}

		private void ValidateMergeEnabled()
		{
			MergeEnabled = _branches.Any(b => b.Checked);
		}

		/// <summary>
		/// Refresh override.
		/// </summary>
		public async override void Refresh()
		{
			base.Refresh();
			await RefreshAsync();
		}

		/// <summary>
		/// Refresh the changeset data asynchronously.
		/// </summary>
		private async Task RefreshAsync()
		{
			var changesetId = _changesetId;
			try
			{
				// Set our busy flag and clear the previous data
				IsBusy = true;
				MergeEnabled = false;
				if (changesetId <= 0)
				{
					Branches = new ObservableCollection<MergeInfoViewModel>();
					return;
				}

				var branches = await Task.Run(() => GetBranches(changesetId));

				// Selected changeset in sequence
				if (changesetId == _changesetId)
				{
					Branches = branches;
					ValidateMergeEnabled();
				}

			}
			catch (Exception ex)
			{
				ShowNotification(ex.Message, NotificationType.Error);
			}
			finally
			{
				IsBusy = false;
			}
		}

		private void OnSelectedChangeset(int changesetId)
		{
			_changesetId = changesetId;
			Refresh();
		}

		private ObservableCollection<MergeInfoViewModel> GetBranches(int changesetId)
		{
			var tfs = CurrentContext.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();

			var sourceBranches = versionControl.QueryBranchObjectOwnership(new[] { changesetId });

			var result = new ObservableCollection<MergeInfoViewModel>();
			if (sourceBranches.Length == 0)
				return result;

			var workspace = versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName)[0];

			var changesetService = new ChangesetService(versionControl, CurrentContext.TeamProjectName);
			var sourceBranchIdentifier = changesetService.GetAssociatedBranches(changesetId)[0];
			var sourceBranchInfo = versionControl.QueryBranchObjects(sourceBranchIdentifier, RecursionType.None)[0];

			var changeset = changesetService.GetChanget(changesetId);

			if (sourceBranchInfo.Properties != null && sourceBranchInfo.Properties.ParentBranch != null
				&& !sourceBranchInfo.Properties.ParentBranch.IsDeleted)
			{
				var parentBranch = sourceBranchInfo.Properties.ParentBranch.Item;
				var mergeInfo = new MergeInfoViewModel(_eventAggregator)
				{
					SourceBranch = sourceBranchIdentifier.Item,
					TargetBranch = parentBranch,
				};

				mergeInfo.ValidationResult = ValidateBranch(workspace, sourceBranchIdentifier.Item, parentBranch, changeset.Changes, changeset.ChangesetId);
				mergeInfo.Checked = mergeInfo.ValidationResult == BranchValidationResult.Success;

				result.Add(mergeInfo);
			}

			var currentBranchInfo = new MergeInfoViewModel(_eventAggregator)
			{
				SourceBranch = sourceBranchIdentifier.Item,
				TargetBranch = sourceBranchIdentifier.Item,
				ValidationResult = BranchValidationResult.Success
			};
			result.Add(currentBranchInfo);

			if (sourceBranchInfo.ChildBranches != null)
			{
				var childBranches = sourceBranchInfo.ChildBranches.Where(b => !b.IsDeleted)
					.Reverse();
				foreach (var childBranch in childBranches)
				{
					var mergeInfo = new MergeInfoViewModel(_eventAggregator)
					{
						SourceBranch = sourceBranchIdentifier.Item,
						TargetBranch = childBranch.Item
					};

					mergeInfo.ValidationResult = ValidateBranch(workspace, sourceBranchIdentifier.Item, childBranch.Item, changeset.Changes, changeset.ChangesetId);

					result.Add(mergeInfo);
				}
			}

			return result;
		}

		private static BranchValidationResult ValidateBranch(Workspace workspace, string sourceBranch, string targetBranch, Change[] changes, int changesetId)
		{
			var result = BranchValidationResult.Success;
			if (result == BranchValidationResult.Success)
			{
				var isMapped = IsMapped(workspace, sourceBranch, targetBranch, changes);
				if (!isMapped)
					result = BranchValidationResult.BranchNotMapped;
			}

			if (result == BranchValidationResult.Success)
			{
				var hasLocalChanges = HaskLocalChanges(workspace, sourceBranch, targetBranch, changes);
				if (hasLocalChanges)
					result = BranchValidationResult.ItemHasLocalChanges;
			}

			if (result == BranchValidationResult.Success)
			{
				var isMerge = IsMerged(workspace.VersionControlServer, sourceBranch, targetBranch, changes, changesetId);
				if (isMerge)
					result = BranchValidationResult.AlreadyMerged;
			}

			return result;
		}

		private static bool IsMerged(VersionControlServer versionControlServer, string sourceBranch, string targetBranch, Change[] changes, int changesetId)
		{
			foreach (var change in changes)
			{
				var source = change.Item.ServerItem;
				var target = source.Replace(sourceBranch, targetBranch);

				var mergeCandidates = versionControlServer.GetMergeCandidates(new ItemSpec(source, RecursionType.None), target);
				if (mergeCandidates.Any(m => m.Changeset.ChangesetId == changesetId))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsMapped(Workspace workspace, string sourceBranch, string targetBranch, IEnumerable<Change> changes)
		{
			var targetItems = changes
				.Select(c => c.Item.ServerItem)
				.Select(path => path.Replace(sourceBranch, targetBranch));

			foreach (var targetItem in targetItems)
			{
				var workingFolder = workspace.TryGetWorkingFolderForServerItem(targetItem);
				if (workingFolder == null)
					return false;
			}

			return true;
		}

		private static bool HaskLocalChanges(Workspace workspace, string sourceBranch, string targetBranch, IEnumerable<Change> changes)
		{
			var itemSpecs = changes
				.Select(c => c.Item.ServerItem)
				.Select(path => path.Replace(sourceBranch, targetBranch))
				.Select(path => new ItemSpec(path, RecursionType.None))
				.ToArray();

			var pendingChanges = workspace.GetPendingChangesEnumerable(itemSpecs);

			return pendingChanges.Any();
		}

		public async void MergeExecute()
		{
			try
			{
				IsBusy = true;

				var result = await Task.Run(() =>MergeExecuteInternal());

				switch (result)
				{
					case MergeResult.CheckInEvaluateFail:
						ShowNotification("Check In evaluate failed", NotificationType.Error);
						break;
					case MergeResult.CheckInFail:
						ShowNotification("Check In  failed", NotificationType.Error);
						break;
					case MergeResult.NothingMerge:
						ShowNotification("Nothing merged", NotificationType.Warning);
						break;
					case MergeResult.PartialSuccess:
						ShowNotification("Partial success", NotificationType.Error);
						break;
					case MergeResult.UnresolvedConflicts:
						ShowNotification("Unresolved conflicts", NotificationType.Error);
						break;
					case MergeResult.Success:
						ShowNotification("Merge success", NotificationType.Information);
						break;
				}
				_eventAggregator.GetEvent<MergeCompleteEvent>().Publish(true);
			}
			catch (Exception ex)
			{
				ShowNotification(ex.Message, NotificationType.Error);
			}
			finally
			{
				IsBusy = false;
			}
		}

		private MergeResult MergeExecuteInternal()
		{
			var result = MergeResult.NothingMerge;
			var tfs = CurrentContext.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();
			
			var workspace = versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName)[0];

			var changesetService = new ChangesetService(versionControl, CurrentContext.TeamProjectName);
			var changeset = changesetService.GetChanget(_changesetId);
			var workItemStore = tfs.GetService<WorkItemStore>();
			var versionSpec = new ChangesetVersionSpec(changeset.ChangesetId);

			var sourceChanges = changeset.Changes;
			foreach (var mergeInfo in _branches.Where(b => b.Checked))
			{
				List<PendingChange> targetPendingChanges;
				if (!MergeToBranch(mergeInfo.SourceBranch, mergeInfo.TargetBranch, sourceChanges, versionSpec, false, workspace, out targetPendingChanges))
				{
					return result == MergeResult.Success ? MergeResult.PartialSuccess : MergeResult.UnresolvedConflicts;
				}

				// Another user can update workitem. Need re-read before update.
				// TODO: maybe move to workspace.CheckIn operation
				var workItems = GetWorkItemCheckinInfo(changeset, workItemStore);
				var checkInResult = CheckIn(targetPendingChanges, mergeInfo, workspace, workItems, changeset.Comment);
				switch (checkInResult)
				{
					case CheckInResult.CheckInEvaluateFail:
						return MergeResult.CheckInEvaluateFail;
					case CheckInResult.CheckInFail:
						return result == MergeResult.Success ? MergeResult.PartialSuccess : MergeResult.CheckInFail;
					case CheckInResult.Success:
						result = MergeResult.Success;
						break;
				}
			}

			return result;
		}

		private static CheckInResult CheckIn(IReadOnlyCollection<PendingChange> targetPendingChanges, MergeInfoViewModel mergeInfoView, Workspace workspace,
			WorkItemCheckinInfo[] workItems, string sourceComment)
		{
			if (targetPendingChanges.Count == 0)
				return CheckInResult.NothingMerge;

			var comment = EvaluateComment(sourceComment, mergeInfoView.SourceBranch, mergeInfoView.TargetBranch);
			var evaluateCheckIn = workspace.EvaluateCheckin2(CheckinEvaluationOptions.All,
				targetPendingChanges,
				comment,
				null,
				workItems);

			if (!CanCheckIn(evaluateCheckIn))
				return CheckInResult.CheckInEvaluateFail;

			//var changesetId = 1;
			var changesetId = workspace.CheckIn(targetPendingChanges.ToArray(), comment, null, workItems, null);
			return changesetId <= 0 ? CheckInResult.CheckInFail : CheckInResult.Success;
		}

		private static bool MergeToBranch(string sourceBranch, string targetBranch, IEnumerable<Change> sourceChanges, VersionSpec version, bool discard,
			Workspace workspace, out List<PendingChange> targetPendingChanges)
		{
			var conflicts = new List<string>();
			var allTargetsFiles = new HashSet<string>();
			targetPendingChanges = null;
			foreach (var change in sourceChanges)
			{
				var source = change.Item.ServerItem;
				var target = source.Replace(sourceBranch, targetBranch);
				allTargetsFiles.Add(target);

				var getLatestResult = workspace.Get(new[] {target}, VersionSpec.Latest, RecursionType.None, GetOptions.None);
				if (!getLatestResult.NoActionNeeded)
					return false;
				var mergeOptions = discard ? MergeOptions.AlwaysAcceptMine : MergeOptions.None;
				var status = workspace.Merge(source, target, version, version, LockLevel.None, RecursionType.None, mergeOptions);

				if (HasConflicts(status))
				{
					conflicts.Add(target);
				}
			}

			if (conflicts.Count > 0)
			{
				var resolved = ResolveConflict(workspace, conflicts.ToArray());
				if (!resolved)
				{
					return false;
				}
			}

			var allPendingChanges = workspace.GetPendingChangesEnumerable();
			targetPendingChanges = allPendingChanges
				.Where(pendingChange => allTargetsFiles.Contains(pendingChange.ServerItem))
				.ToList();

			return true;
		}

		public bool MergeCanEcexute()
		{
			return true;
			//return _branches.Any(b => b.Checked);
		}

		private static bool HasConflicts(GetStatus mergeStatus)
		{
			return !mergeStatus.NoActionNeeded && mergeStatus.NumConflicts > 0;
		}

		private static bool ResolveConflict(Workspace workspace, string[] targetPath)
		{
			var conflicts = workspace.QueryConflicts(targetPath, false);
			if (conflicts.IsNullOrEmpty())
				return true;

			workspace.AutoResolveValidConflicts(conflicts, AutoResolveOptions.AllSilent);

			conflicts = workspace.QueryConflicts(targetPath, false);
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

		private static string EvaluateComment(string sourceComment, string sourceBranch, string targetBranch)
		{
			if (string.IsNullOrWhiteSpace(sourceComment))
				return null;

			var targetShortBranchName = GetShortBranchName(targetBranch);
			string comment;
			if (sourceComment.StartsWith("MERGE "))
			{
				var originalCommentStartPos = sourceComment.IndexOf('(');
				var mergeComment = sourceComment.Substring(0, originalCommentStartPos);
				string originaComment;
				if (originalCommentStartPos + 1 < sourceComment.Length)
					originaComment = sourceComment.Substring(originalCommentStartPos + 1, sourceComment.Length - originalCommentStartPos - 2);
				else
					originaComment = string.Empty;
				comment = string.Format("{0} -> {1} ({2})", mergeComment, targetShortBranchName, originaComment);
			}
			else
			{
				var sourceShortBranchName = GetShortBranchName(sourceBranch);
				comment = string.Format("MERGE {0} -> {1} ({2})", sourceShortBranchName, targetShortBranchName, sourceComment);
			}

			return comment;
		}

		private static string GetShortBranchName(string fullBranchName)
		{
			var pos = fullBranchName.LastIndexOf('/');
			var shortName = fullBranchName.Substring(pos + 1);
			return shortName;
		}

		private static WorkItemCheckinInfo[] GetWorkItemCheckinInfo(Changeset changeset, WorkItemStore workItemStore)
		{
			if (changeset.WorkItems == null)
				return null;

			var result = new List<WorkItemCheckinInfo>(changeset.WorkItems.Length);
			foreach (var associatedWorkItem in changeset.AssociatedWorkItems)
			{
				var workItem = workItemStore.GetWorkItem(associatedWorkItem.Id);
				var workItemCheckinInfo = new WorkItemCheckinInfo(workItem, WorkItemCheckinAction.Associate);
				result.Add(workItemCheckinInfo);
			}

			return result.ToArray();
		}

		private static bool CanCheckIn(CheckinEvaluationResult checkinEvaluationResult)
		{
			return checkinEvaluationResult.Conflicts.IsNullOrEmpty()
				&& checkinEvaluationResult.NoteFailures.IsNullOrEmpty()
				&& checkinEvaluationResult.PolicyFailures.IsNullOrEmpty()
				&& checkinEvaluationResult.PolicyEvaluationException == null;
		}

		public override void SaveContext(object sender, SectionSaveContextEventArgs e)
		{
			base.SaveContext(sender, e);

			_eventAggregator.GetEvent<SelectChangesetEvent>()
				.Unsubscribe(OnSelectedChangeset);

			var context = new BranchContext
			{
				ChangesetId = _changesetId,
				Branches = Branches
			};

			e.Context = context;
		}

		protected override async void ContextChanged(object sender, ContextChangedEventArgs e)
		{
			base.ContextChanged(sender, e);

			// If the team project collection or team project changed, refresh 
			// the data for this section 
			if (e.TeamProjectCollectionChanged || e.TeamProjectChanged)
			{
				await RefreshAsync();
			}
		}
	}
}