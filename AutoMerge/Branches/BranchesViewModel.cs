using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMerge.Events;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
	public class BranchesViewModel : TeamExplorerSectionViewModelBase
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

		private readonly IEventAggregator _eventAggregator;
		private ChangesetService _changesetService;

		private ChangesetViewModel _changeset;

		/// <summary>
		/// Constructor.
		/// </summary>
		public BranchesViewModel()
		{
			Title = Resources.BrancheSectionName;
			IsVisible = true;
			IsExpanded = true;
			IsBusy = false;
			CheckInAfterMerge = true;

			MergeCommand = new DelegateCommand(MergeExecute, MergeCanEcexute);

			_eventAggregator = EventAggregatorFactory.Get();
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

		public DelegateCommand MergeCommand { get; private set; }

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

		public bool CheckInAfterMerge
		{
			get
			{
				return _checkInAfterMerge;
			}
			set
			{
				_checkInAfterMerge = value;
				RaisePropertyChanged("CheckInAfterMerge");
			}
		}

		private bool _checkInAfterMerge;

		public async override void Initialize(object sender, SectionInitializeEventArgs e)
		{
			base.Initialize(sender, e);

			var tfs = Context.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();
			_changesetService = new ChangesetService(versionControl, Context.TeamProjectName);

			await RefreshAsync();

			_eventAggregator.GetEvent<SelectChangesetEvent>()
				.Subscribe(OnSelectedChangeset);
			_eventAggregator.GetEvent<BranchSelectedChangedEvent>()
				.Subscribe(OnBranchSelectedChanged);
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

			ErrorMessage = CalculateError(_changeset);
			if (changeset == null || !string.IsNullOrEmpty(ErrorMessage))
			{
				Branches = new ObservableCollection<MergeInfoViewModel>();
				return;
			}

			var branches = await Task.Run(() => GetBranches(Context, changeset.ChangesetId));

			// Selected changeset in sequence
			if (changeset.ChangesetId == _changeset.ChangesetId)
			{
				Branches = branches;
				ErrorMessage = branches.Count <= 1 ? "Target branches not found" : null;
				MergeCommand.RaiseCanExecuteChanged();
			}
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

		private ObservableCollection<MergeInfoViewModel> GetBranches(ITeamFoundationContext context, int changesetId)
		{
			if (context == null)
				return new ObservableCollection<MergeInfoViewModel>();
			var tfs = context.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();

			var sourceBranches = versionControl.QueryBranchObjectOwnership(new[] { changesetId });

			var result = new ObservableCollection<MergeInfoViewModel>();
			if (sourceBranches.Length == 0)
				return result;

			var workspace = versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName)[0];

			var changesetService = new ChangesetService(versionControl, context.TeamProjectName);
			var sourceBranchIdentifier = changesetService.GetAssociatedBranches(changesetId)[0];
			var sourceBranchInfo = versionControl.QueryBranchObjects(sourceBranchIdentifier, RecursionType.None)[0];

			var changeset = changesetService.GetChangeset(changesetId);

			if (sourceBranchInfo.Properties != null && sourceBranchInfo.Properties.ParentBranch != null
				&& !sourceBranchInfo.Properties.ParentBranch.IsDeleted)
			{
				var parentBranch = sourceBranchInfo.Properties.ParentBranch.Item;
				var mergeInfo = new MergeInfoViewModel(_eventAggregator)
				{
					SourceBranch = sourceBranchIdentifier.Item,
					TargetBranch = parentBranch,
					FileMergeInfos = new List<FileMergeInfo>(changeset.Changes.Count()),
					ValidationResult = BranchValidationResult.Success
//					ValidationResult =
//						ValidateBranch(workspace, sourceBranchIdentifier.Item, parentBranch, changeset.Changes, changeset.ChangesetId),
				};

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
						TargetBranch = childBranch.Item,
						FileMergeInfos = new List<FileMergeInfo>(changeset.Changes.Count()),
						ValidationResult = BranchValidationResult.Success
					};

					//mergeInfo.ValidationResult = ValidateBranch(workspace, sourceBranchIdentifier.Item, childBranch.Item, changeset.Changes, changeset.ChangesetId);

					result.Add(mergeInfo);
				}
			}

			foreach (var change in changeset.Changes)
			{
				var mergesRelationships = versionControl.QueryMergeRelationships(change.Item.ServerItem);
				if (mergesRelationships == null)
					continue;

				foreach (var mergesRelationship in mergesRelationships)
				{
					foreach (var mergeInfoViewModel in result)
					{
						if (mergesRelationship.Item.StartsWith(mergeInfoViewModel.TargetBranch))
						{
							var fileMergeInfo = new FileMergeInfo
							{
								SourceFile = change.Item.ServerItem,
								TargetFile = mergesRelationship.Item,
								ChangesetVersionSpec = new ChangesetVersionSpec(changesetId)
							};
							mergeInfoViewModel.FileMergeInfos.Add(fileMergeInfo);
							if (mergeInfoViewModel.ValidationResult == BranchValidationResult.Success)
							{
								mergeInfoViewModel.ValidationResult = ValidateItem(workspace, fileMergeInfo.TargetFile, changesetId);
								mergeInfoViewModel.Checked = mergeInfoViewModel.Checked
									&& (mergeInfoViewModel.ValidationResult == BranchValidationResult.Success);
							}
							break;
						}
					}
				}
			}
			

			return result;
		}

		private static BranchValidationResult ValidateItem(Workspace workspace, string targetFile, int changesetId)
		{
			var result = BranchValidationResult.Success;
			if (result == BranchValidationResult.Success)
			{
				var isMapped = IsMapped(workspace, targetFile);
				if (!isMapped)
					result = BranchValidationResult.BranchNotMapped;
			}

			if (result == BranchValidationResult.Success)
			{
				var hasLocalChanges = HasLocalChanges(workspace, targetFile);
				if (hasLocalChanges)
					result = BranchValidationResult.ItemHasLocalChanges;
			}

//			if (result == BranchValidationResult.Success)
//			{
//				var isMerge = IsMerged(workspace.VersionControlServer, sourceBranch, targetBranch, changes, changesetId);
//				if (isMerge)
//					result = BranchValidationResult.AlreadyMerged;
//			}

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
				var hasLocalChanges = HasLocalChanges(workspace, sourceBranch, targetBranch, changes);
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

		private static bool IsMerged(VersionControlServer versionControlServer, string sourceBranch, string targetBranch, IEnumerable<Change> changes, int changesetId)
		{
			foreach (var change in changes)
			{
				var source = change.Item.ServerItem;
				var target = source.Replace(sourceBranch, targetBranch);

				if (!IsMerged(versionControlServer, source, target, changesetId))
					return false;
			}

			return true;
		}

		private static bool IsMerged(VersionControlServer versionControlServer, string source, string target, int changesetId)
		{
			var mergeCandidates = versionControlServer.GetMergeCandidates(new ItemSpec(source, RecursionType.None), target);
			return mergeCandidates.All(m => m.Changeset.ChangesetId != changesetId);
		}

		private static bool IsMapped(Workspace workspace, string sourceBranch, string targetBranch, IEnumerable<Change> changes)
		{
			var targetItems = changes
				.Select(c => c.Item.ServerItem)
				.Select(path => path.Replace(sourceBranch, targetBranch));

			return targetItems.All(targetItem => IsMapped(workspace, targetItem));
		}

		private static bool IsMapped(Workspace workspace, string targetItem)
		{
			return workspace.IsServerPathMapped(targetItem);
		}

		private static bool HasLocalChanges(Workspace workspace, string sourceBranch, string targetBranch, IEnumerable<Change> changes)
		{
			var itemSpecs = changes
				.Select(c => c.Item.ServerItem)
				.Select(path => path.Replace(sourceBranch, targetBranch))
				.Select(path => new ItemSpec(path, RecursionType.None))
				.ToArray();

			return HasLocalChanges(workspace, itemSpecs);
		}

		private static bool HasLocalChanges(Workspace workspace, string path)
		{
			//return workspace.GetPendingChangesEnumerable(path).Any();
			return workspace.GetPendingChangesEnumerable().Any(p => p.SourceServerItem == path);
		}

		private static bool HasLocalChanges(Workspace workspace, ItemSpec[] itemSpecs)
		{
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
						ShowNotification("Check In failed", NotificationType.Error);
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
						ShowNotification("Merge is successful", NotificationType.Information);
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
			var context = Context;
			var tfs = context.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();
			
			var workspace = versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName)[0];

			var changesetService = _changesetService;
			var changeset = changesetService.GetChangeset(_changeset.ChangesetId);
			var mergeOption = _mergeOption;
			var workItemStore = tfs.GetService<WorkItemStore>();

			foreach (var mergeInfo in _branches.Where(b => b.Checked))
			{
				List<PendingChange> targetPendingChanges;
				if (!MergeToBranch(mergeInfo, mergeOption, workspace, out targetPendingChanges))
				{
					return result == MergeResult.Success ? MergeResult.PartialSuccess : MergeResult.UnresolvedConflicts;
				}

				if (!CheckInAfterMerge)
					break;
				// Another user can update workitem. Need re-read before update.
				// TODO: maybe move to workspace.CheckIn operation
				var workItems = GetWorkItemCheckinInfo(changeset, workItemStore);
				var checkInResult = CheckIn(targetPendingChanges, mergeInfo, workspace, workItems, changeset.Comment, changeset.PolicyOverride);
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

		private static CheckInResult CheckIn(IReadOnlyCollection<PendingChange> targetPendingChanges, MergeInfoViewModel mergeInfoView,
			Workspace workspace, WorkItemCheckinInfo[] workItems, string sourceComment, PolicyOverrideInfo policyOverride)
		{
			if (targetPendingChanges.Count == 0)
				return CheckInResult.NothingMerge;

			var comment = EvaluateComment(sourceComment, mergeInfoView.SourceBranch, mergeInfoView.TargetBranch);
			var evaluateCheckIn = workspace.EvaluateCheckin2(CheckinEvaluationOptions.All,
				targetPendingChanges,
				comment,
				null,
				workItems);

			var skipPolicyValidate = !policyOverride.PolicyFailures.IsNullOrEmpty();
			if (!CanCheckIn(evaluateCheckIn, skipPolicyValidate))
				return CheckInResult.CheckInEvaluateFail;

			var changesetId = workspace.CheckIn(targetPendingChanges.ToArray(), null, comment,
				null, workItems, policyOverride);
			return changesetId <= 0 ? CheckInResult.CheckInFail : CheckInResult.Success;
		}

		private bool MergeToBranch(MergeInfoViewModel mergeInfoeViewModel, MergeOption mergeOption, Workspace workspace, out List<PendingChange> targetPendingChanges)
		{
			var conflicts = new List<string>();
			var allTargetsFiles = new HashSet<string>();
			targetPendingChanges = null;
			var itemSpecs = new List<ItemSpec>(mergeInfoeViewModel.FileMergeInfos.Count);
			foreach (var fileMergeInfo in mergeInfoeViewModel.FileMergeInfos)
			{
				var source = fileMergeInfo.SourceFile;
				var target = fileMergeInfo.TargetFile;
				var version = fileMergeInfo.ChangesetVersionSpec;
				itemSpecs.Add(new ItemSpec(target, RecursionType.None));
				allTargetsFiles.Add(fileMergeInfo.TargetFile);

				var getLatestResult = workspace.Get(new[] {target}, VersionSpec.Latest, RecursionType.None, GetOptions.None);
				if (!getLatestResult.NoActionNeeded)
				{
					// HACK.
					getLatestResult = workspace.Get(new[] {target}, VersionSpec.Latest, RecursionType.None, GetOptions.None);
					if (!getLatestResult.NoActionNeeded)
						return false;
				}

				var mergeOptions = ToTfsMergeOptions(mergeOption);
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

			var allPendingChanges = workspace.GetPendingChangesEnumerable(itemSpecs.ToArray());
			targetPendingChanges = allPendingChanges.ToList();
//				.Where(pendingChange => allTargetsFiles.Contains(pendingChange.ServerItem))
//				.ToList();

			return true;
		}

		private static bool MergeToBranch(string sourceBranch, string targetBranch, IEnumerable<Change> sourceChanges, VersionSpec version, MergeOption mergeOption,
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
				{
					// HACK.
					getLatestResult = workspace.Get(new[] { target }, VersionSpec.Latest, RecursionType.None, GetOptions.None);
					if (!getLatestResult.NoActionNeeded)
						return false;
				}

				var mergeOptions = ToTfsMergeOptions(mergeOption);
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

		private static MergeOptions ToTfsMergeOptions(MergeOption mergeOption)
		{
			switch (mergeOption)
			{
				case MergeOption.KeepTarget:
					return MergeOptions.AlwaysAcceptMine;
				case MergeOption.OverwriteTarget:
					return MergeOptions.ForceMerge;
				case MergeOption.ManualResolveConflict:
					return MergeOptions.None;
				default:
					return MergeOptions.None;
			}
		}

		public bool MergeCanEcexute()
		{
			return _branches != null && _branches.Any(b => b.Checked);
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
				var originaComment =
					originalCommentStartPos + 1 < sourceComment.Length
					? sourceComment.Substring(originalCommentStartPos + 1, sourceComment.Length - originalCommentStartPos - 2)
					: string.Empty;
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

		private static bool CanCheckIn(CheckinEvaluationResult checkinEvaluationResult, bool skipPolicy)
		{
			var result = checkinEvaluationResult.Conflicts.IsNullOrEmpty()
				&& checkinEvaluationResult.NoteFailures.IsNullOrEmpty()
				&& checkinEvaluationResult.PolicyEvaluationException == null;

			if (!skipPolicy)
				result &= checkinEvaluationResult.PolicyFailures.IsNullOrEmpty();
			return result;
		}
	}
}