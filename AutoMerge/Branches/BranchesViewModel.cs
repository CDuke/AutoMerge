using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMerge.Events;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common.Internal;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
	public class BranchesViewModel : TeamExplorerSectionViewModelBase
	{
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
				var targetBranch = sourceBranchInfo.Properties.ParentBranch.Item;
				var sourceBranch = sourceBranchIdentifier.Item;
				var mergeInfo = new MergeInfoViewModel(_eventAggregator)
				{
					SourceBranch = sourceBranch,
					TargetBranch = targetBranch,
					FileMergeInfos = new List<FileMergeInfo>(changeset.Changes.Count()),
					ValidationResult = BranchValidationResult.Success,
					Comment =  EvaluateComment(changeset.Comment, sourceBranch, targetBranch)
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
					var targetBranch = childBranch.Item;
					var sourceBranch = sourceBranchIdentifier.Item;
					var mergeInfo = new MergeInfoViewModel(_eventAggregator)
					{
						SourceBranch = sourceBranch,
						TargetBranch = targetBranch,
						FileMergeInfos = new List<FileMergeInfo>(changeset.Changes.Count()),
						ValidationResult = BranchValidationResult.Success,
						Comment = EvaluateComment(changeset.Comment, sourceBranch, targetBranch)
					};

					result.Add(mergeInfo);
				}
			}

			var changesetVersionSpec = new ChangesetVersionSpec(changesetId);
			foreach (var change in changeset.Changes)
			{
				var mergesRelationships = versionControl.QueryMergeRelationships(change.Item.ServerItem);
				if (mergesRelationships == null)
					continue;

				foreach (var mergesRelationship in mergesRelationships.Where(r => !r.IsDeleted))
				{
					foreach (var mergeInfoViewModel in result)
					{
						if (mergesRelationship.Item.StartsWith(mergeInfoViewModel.TargetBranch))
						{
							var fileMergeInfo = new FileMergeInfo
							{
								SourceFile = change.Item.ServerItem,
								TargetFile = mergesRelationship.Item,
								ChangesetVersionSpec = changesetVersionSpec
							};
							mergeInfoViewModel.FileMergeInfos.Add(fileMergeInfo);
							if (mergeInfoViewModel.ValidationResult == BranchValidationResult.Success)
							{
								mergeInfoViewModel.ValidationResult = ValidateItem(workspace, fileMergeInfo);
								mergeInfoViewModel.ValidationMessage = ToMessage(mergeInfoViewModel.ValidationResult);
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

		private static string ToMessage(BranchValidationResult validationResult)
		{
			switch (validationResult)
			{
				case BranchValidationResult.Success:
					return null;
				case BranchValidationResult.AlreadyMerged:
					return "Changeset already merged";
				case BranchValidationResult.BranchNotMapped:
					return "Branch not mapped";
				case BranchValidationResult.ItemHasLocalChanges:
					return "Some files have local changes. Commit or undo it";
				default:
					return "Unknown error";
			}
		}

		private static BranchValidationResult ValidateItem(Workspace workspace, FileMergeInfo fileMergeInfo)
		{
			var result = BranchValidationResult.Success;
			if (result == BranchValidationResult.Success)
			{
				var isMapped = IsMapped(workspace, fileMergeInfo.TargetFile);
				if (!isMapped)
					result = BranchValidationResult.BranchNotMapped;
			}

			if (result == BranchValidationResult.Success)
			{
				var hasLocalChanges = HasLocalChanges(workspace, fileMergeInfo.TargetFile);
				if (hasLocalChanges)
					result = BranchValidationResult.ItemHasLocalChanges;
			}

			if (result == BranchValidationResult.Success)
			{
				var isMerged = IsMerged(workspace, fileMergeInfo);
				if (isMerged)
					result = BranchValidationResult.AlreadyMerged;
			}

			return result;
		}


		private static bool IsMerged(Workspace workspace, FileMergeInfo fileMergeInfo)
		{
			return IsMerged(workspace.VersionControlServer,
				fileMergeInfo.SourceFile,
				fileMergeInfo.TargetFile,
				fileMergeInfo.ChangesetVersionSpec.ChangesetId);
		}

		private static bool IsMerged(VersionControlServer versionControlServer, string source, string target, int changesetId)
		{
			var mergeCandidates = versionControlServer.GetMergeCandidates(new ItemSpec(source, RecursionType.None), target);
			return mergeCandidates.All(m => m.Changeset.ChangesetId != changesetId);
		}

		private static bool IsMapped(Workspace workspace, string targetItem)
		{
			return workspace.IsServerPathMapped(targetItem);
		}

		private static bool HasLocalChanges(Workspace workspace, string path)
		{
			//return workspace.GetPendingChangesEnumerable(path).Any();
			return workspace.GetPendingChangesEnumerable().Any(p => ExtractServerPath(p) == path);
		}

		private static string ExtractServerPath(PendingChange pendingChange)
		{
			if (pendingChange.SourceServerItem != null)
				return pendingChange.SourceServerItem;

			return pendingChange.ServerItem;
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

				var sendEvent = true;
				foreach (var resultModel in result)
				{
					switch (resultModel.MergeResult)
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
						case MergeResult.UnresolvedConflicts:
							ShowNotification("Unresolved conflicts", NotificationType.Error);
							break;
						case MergeResult.CanNotGetLatest:
							ShowNotification("Gan not get lates", NotificationType.Error);
							break;
						case MergeResult.Success:
							ShowNotification("Merge is successful");
							break;
						case MergeResult.NotCheckIn:
							sendEvent = false;
							// It's can be only one
							OpenPendingChanges(resultModel);
							break;
					}
				}


				if (sendEvent)
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

		private void OpenPendingChanges(MergeResultModel resultModel)
		{
			var teamExplorer = ServiceProvider.GetService<ITeamExplorer>();
			var pendingChangesPage = (TeamExplorerPageBase)teamExplorer.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
			var model = (IPendingCheckin)pendingChangesPage.Model;
			model.PendingChanges.Comment = resultModel.BranchInfo.Comment;
			if (resultModel.WorkItemIds != null)
			{
				var modelType = model.GetType();
				var method = modelType.GetMethod("AddWorkItemsByIdAsync",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				var workItemsIdsArray = resultModel.WorkItemIds.ToArray();
				method.Invoke(model, new object[] {workItemsIdsArray, 1 /* Add */});
			}
		}

		private List<MergeResultModel> MergeExecuteInternal()
		{
			var result = new List<MergeResultModel>();
			var context = Context;
			var tfs = context.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();
			
			var workspace = versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName)[0];

			var changesetService = _changesetService;
			var changeset = changesetService.GetChangeset(_changeset.ChangesetId);
			var mergeOption = _mergeOption;
			var workItemStore = tfs.GetService<WorkItemStore>();
			var workItemIds = changeset.AssociatedWorkItems != null
				? changeset.AssociatedWorkItems.Select(w => w.Id).ToList()
				: new List<int>();

			foreach (var mergeInfo in _branches.Where(b => b.Checked))
			{
				var mergeResultModel = MergeToBranch(mergeInfo, mergeOption, true, workspace);
				mergeResultModel.WorkItemIds = workItemIds;
				result.Add(mergeResultModel);

				if (!CheckInAfterMerge)
				{
					mergeResultModel.MergeResult = MergeResult.NotCheckIn;
					break;
				}

				if (mergeResultModel.MergeResult != MergeResult.Success)
				{
					break;
				}

				var checkInResult = CheckIn(mergeResultModel.PendingChanges, mergeInfo, workspace, workItemIds, changeset.PolicyOverride, workItemStore);
				mergeResultModel.ChangesetId = checkInResult.ChangesetId;
				mergeResultModel.MergeResult = checkInResult.CheckinResult;

				if (mergeResultModel.MergeResult == MergeResult.Success)
				{
					mergeResultModel.CheckedIn = true;
				}
				else
				{
					break;
				}
			}

			return result;
		}

		private static CheckInResult CheckIn(IReadOnlyCollection<PendingChange> targetPendingChanges, MergeInfoViewModel mergeInfoView,
			Workspace workspace, IReadOnlyCollection<int> workItemIds, PolicyOverrideInfo policyOverride, WorkItemStore workItemStore)
		{
			var result = new CheckInResult();
			if (targetPendingChanges.Count == 0)
			{
				result.CheckinResult = MergeResult.NothingMerge;
				return result;
			}

			// Another user can update workitem. Need re-read before update.
			var workItems = GetWorkItemCheckinInfo(workItemIds, workItemStore);

			var comment = mergeInfoView.Comment;
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
				result.CheckinResult = MergeResult.Success;
			}
			else
			{
				result.CheckinResult = MergeResult.CheckInFail;
			}
			return result;
		}

		private static MergeResultModel MergeToBranch(MergeInfoViewModel mergeInfoeViewModel, MergeOption mergeOption, bool resolveConflict, Workspace workspace)
		{
			var result = new MergeResultModel
			{
				BranchInfo = mergeInfoeViewModel,
			};
			var conflicts = new List<string>();
			var allTargetsFiles = new HashSet<string>();
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
					{
						result.MergeResult = MergeResult.CanNotGetLatest;
						result.Message = "Can not get latest";
						return result;
					}
				}

				var mergeOptions = ToTfsMergeOptions(mergeOption);
				var status = workspace.Merge(source, target, version, version, LockLevel.None, RecursionType.None, mergeOptions);

				if (HasConflicts(status))
				{
					conflicts.Add(target);
				}
			}

			if (conflicts.Count > 0 && resolveConflict)
			{
				var resolved = ResolveConflict(workspace, conflicts.ToArray());
				if (!resolved)
				{
					result.MergeResult = MergeResult.UnresolvedConflicts;
					result.Message = "Unresolved conflicts";
					return result;
				}
			}

			var allPendingChanges = workspace.GetPendingChangesEnumerable(itemSpecs.ToArray());
			var targetPendingChanges = allPendingChanges.ToList();
//				.Where(pendingChange => allTargetsFiles.Contains(pendingChange.ServerItem))
//				.ToList();
			result.MergeResult = MergeResult.Success;
			result.PendingChanges = targetPendingChanges;
			return result;
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
	}
}