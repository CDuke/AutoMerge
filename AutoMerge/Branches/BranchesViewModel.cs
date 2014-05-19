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
		private Workspace _workspace;

		private ChangesetViewModel _changeset;

		public BranchesViewModel()
		{
			Title = Resources.BrancheSectionName;
			IsVisible = true;
			IsExpanded = true;
			IsBusy = false;

			MergeAndCheckInCommand = new DelegateCommand(MergeAndCheckInExecute, MergeCanEcexute);
			MergeWithoutCheckInCommand = new DelegateCommand(MergeWithoutCheckInExecute, MergeCanEcexute);

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

		public DelegateCommand MergeAndCheckInCommand { get; private set; }

		public DelegateCommand MergeWithoutCheckInCommand { get; set; }

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

		public async override void Initialize(object sender, SectionInitializeEventArgs e)
		{
			base.Initialize(sender, e);

			var tfs = Context.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();
			_changesetService = new ChangesetService(versionControl, Context.TeamProjectName);
			_workspace = versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName)[0];

			await RefreshAsync();

			_eventAggregator.GetEvent<SelectChangesetEvent>()
				.Subscribe(OnSelectedChangeset);
			_eventAggregator.GetEvent<BranchSelectedChangedEvent>()
				.Subscribe(OnBranchSelectedChanged);
		}

		private void OnBranchSelectedChanged(MergeInfoViewModel obj)
		{
			MergeAndCheckInCommand.RaiseCanExecuteChanged();
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

			var branches = await Task.Run(() => GetBranches(Context, changeset));

			// Selected changeset in sequence
			if (changeset.ChangesetId == _changeset.ChangesetId)
			{
				Branches = branches;
				ErrorMessage = branches.Count <= 1 ? "Target branches not found" : null;
				MergeAndCheckInCommand.RaiseCanExecuteChanged();
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
			var mergesRelationships = versionControl.QueryMergeRelationships(sourceTopFolder)
				.Where(r => !r.IsDeleted)
				.ToList();

			if (mergesRelationships.Count > 0)
			{
				var trackMerges = versionControl.TrackMerges(new[] {changesetViewModel.ChangesetId},
					new ItemIdentifier(sourceTopFolder),
					mergesRelationships.ToArray(),
					null);

				var trackMergeInfo = GetTrackMergeInfo(versionControl,
					trackMerges, sourceTopFolder);
				trackMergeInfo.SourceBranches.Reverse();
				if (trackMergeInfo.SourceBranches.IsNullOrEmpty())
				{
					trackMergeInfo.SourceComment = changesetViewModel.Comment;
				}

				var changesetVersionSpec = new ChangesetVersionSpec(changesetViewModel.ChangesetId);
				var sourceBranchIdentifier = changesetViewModel.Branches.Select(b => new ItemIdentifier(b)).First();
				var sourceBranchInfo = versionControl.QueryBranchObjects(sourceBranchIdentifier, RecursionType.None)[0];
				if (sourceBranchInfo.Properties != null && sourceBranchInfo.Properties.ParentBranch != null
				    && !sourceBranchInfo.Properties.ParentBranch.IsDeleted)
				{
					var targetBranch = sourceBranchInfo.Properties.ParentBranch.Item;
					var sourceBranch = sourceBranchIdentifier.Item;
					var targetPath = GetTargetPath(mergesRelationships, targetBranch);
					if (targetPath != null)
					{
						var mergeInfo = new MergeInfoViewModel(_eventAggregator)
						{
							SourceBranch = sourceBranch,
							TargetBranch = targetBranch,
							SourcePath = sourceTopFolder,
							TargetPath = targetPath,
							ChangesetVersionSpec = changesetVersionSpec,
							FileMergeInfos = new List<FileMergeInfo>(changes.Count()),
							ValidationResult = BranchValidationResult.Success,
							Comment = EvaluateComment(trackMergeInfo, sourceBranch, targetBranch)
						};

						mergeInfo.ValidationResult = ValidateItem(workspace, mergeInfo, trackMerges);
						mergeInfo.ValidationMessage = ToMessage(mergeInfo.ValidationResult);

						mergeInfo.Checked = mergeInfo.ValidationResult == BranchValidationResult.Success;

						result.Add(mergeInfo);
					}
				}

				var currentBranchInfo = new MergeInfoViewModel(_eventAggregator)
				{
					SourceBranch = sourceBranchIdentifier.Item,
					TargetBranch = sourceBranchIdentifier.Item,
					SourcePath = sourceTopFolder,
					TargetPath = sourceTopFolder,
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
						var targetPath = GetTargetPath(mergesRelationships, targetBranch);
						if (targetPath != null)
						{
							var mergeInfo = new MergeInfoViewModel(_eventAggregator)
							{
								SourceBranch = sourceBranch,
								TargetBranch = targetBranch,
								SourcePath = sourceTopFolder,
								TargetPath = targetPath,
								ChangesetVersionSpec = changesetVersionSpec,
								FileMergeInfos = new List<FileMergeInfo>(changes.Count()),
								ValidationResult = BranchValidationResult.Success,
								Comment = EvaluateComment(trackMergeInfo, sourceBranch, targetBranch)
							};

							mergeInfo.ValidationResult = ValidateItem(workspace, mergeInfo, trackMerges);
							mergeInfo.ValidationMessage = ToMessage(mergeInfo.ValidationResult);

							result.Add(mergeInfo);
						}
					}
				}
			}

			return result;
		}

		private static string EvaluateComment(TrackMergeInfo trackMergeInfo, string sourceBranch, string targetBranch)
		{
			var mergePath = trackMergeInfo.SourceBranches.Concat(new[] {sourceBranch, targetBranch})
				.Select(GetShortBranchName);
			var mergePathString = string.Join(" -> ", mergePath);
			return string.Format("MERGE {0} ({1})", mergePathString, trackMergeInfo.SourceComment);
		}

		private TrackMergeInfo GetTrackMergeInfo(VersionControlServer versionControl,
			IEnumerable<ExtendedMerge> allTrackMerges,
			string targetPath)
		{
			var result = new TrackMergeInfo
			{
				SourceBranches = new List<string>()
			};
			var trackMerges = allTrackMerges.Where(m => m.TargetItem.Item == targetPath).ToArray();
			if (!trackMerges.IsNullOrEmpty())
			{
				var changesetIds = trackMerges.Select(t => t.SourceChangeset.ChangesetId).ToArray();
				var mergeSourceBranches = _changesetService.GetAssociatedBranches(changesetIds)
					.Select(b => b.Item)
					.Distinct()
					.ToArray();

				if (mergeSourceBranches.Length == 1)
				{
					result.SourceBranches.Add(mergeSourceBranches[0]);

					var sourceItem = trackMerges.First().SourceItem.Item.ServerItem;
					var comment = trackMerges.First().SourceChangeset.Comment;
					if (trackMerges.Length > 1)
					{
						sourceItem = trackMerges.Skip(1)
							.Aggregate(sourceItem, (current, trackMerge) => FindShareFolder(current, trackMerge.SourceItem.Item.ServerItem));
						comment = "source changeset has several comments";
					}

					var sourceMergesRelationships = versionControl.QueryMergeRelationships(sourceItem)
					.Where(r => !r.IsDeleted)
					.ToList();

					var sourceTrackMerges = versionControl.TrackMerges(changesetIds,
						new ItemIdentifier(sourceItem),
						sourceMergesRelationships.ToArray(),
						null);

					var sourceTrackMergeInfo = GetTrackMergeInfo(versionControl, sourceTrackMerges, sourceItem);

					if (!sourceTrackMergeInfo.SourceBranches.IsNullOrEmpty())
					{
						result.SourceBranches.AddRange(sourceTrackMergeInfo.SourceBranches);
						result.SourceComment = sourceTrackMergeInfo.SourceComment;
					}
					else
					{
						result.SourceComment = comment;
					}
				}
				else
				{
					result.SourceBranches.Add("multi");
					result.SourceComment = "source changeset has several comments";
				}
			}

			return result;
		}

		private static string GetTargetPath(ICollection<ItemIdentifier> mergesRelationships, string targetBranch)
		{
			if (mergesRelationships == null || mergesRelationships.Count == 0)
				return null;

			return mergesRelationships.Select(m => m.Item).FirstOrDefault(p => p.Contains(targetBranch));
		}

		private static string CalculateTopFolder(ICollection<Change> changes)
		{
			if (changes == null || changes.Count == 0)
				throw new ArgumentNullException("changes");

			string topFolder = null;
			foreach (var change in changes)
			{
				if (topFolder != null)
				{
					if (change.Item.ServerItem.Contains(topFolder) && change.Item.ServerItem != topFolder)
						continue;
				}

				var changeFolder = ExtractFolder(change.ChangeType, change.Item);

				if ((topFolder == null) || (topFolder != null && topFolder.Contains(changeFolder)))
				{
					topFolder = changeFolder;
					continue;
				}

				topFolder = FindShareFolder(topFolder, changeFolder);
			}

			if (topFolder != null && topFolder.EndsWith("/"))
			{
				topFolder = topFolder.Substring(0, topFolder.Length - 1);
			}

			return topFolder;
		}

		private static string FindShareFolder(string topFolder, string changeFolder)
		{
			var folder = topFolder;
			while (folder != "$")
			{
				folder = ExtractFolder(folder);
				if (folder != null && changeFolder.Contains(folder))
					break;
			}

			return folder == "$" ? folder + "/" : folder;
		}

		private static string ExtractFolder(ChangeType changeType, Item item)
		{
			if (changeType.HasFlag(ChangeType.Add) && item.ItemType == ItemType.Folder)
				return ExtractFolder(item.ServerItem);

			return item.ItemType == ItemType.Folder
				? item.ServerItem
				: ExtractFolder(item.ServerItem);
		}

		private static string ExtractFolder(string serverItem)
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
					return "Folder has local changes. Check-in or undo it";
				case BranchValidationResult.NoAccess:
					return "You have not rights for edit";
				default:
					return "Unknown error";
			}
		}

		private static BranchValidationResult ValidateItem(Workspace workspace, MergeInfoViewModel mergeInfoViewModel, IEnumerable<ExtendedMerge> trackMerges)
		{
			var result = BranchValidationResult.Success;

			if (result == BranchValidationResult.Success)
			{
				var isMerged = IsMerged(mergeInfoViewModel.SourcePath, mergeInfoViewModel.TargetPath, trackMerges);
				if (isMerged)
					result = BranchValidationResult.AlreadyMerged;
			}

			if (result == BranchValidationResult.Success)
			{
				var userHasAccess = UserHasAccess(workspace.VersionControlServer, mergeInfoViewModel.TargetPath);
				if (!userHasAccess)
					result = BranchValidationResult.NoAccess;
			}

			if (result == BranchValidationResult.Success)
			{
				var isMapped = IsMapped(workspace, mergeInfoViewModel.TargetPath);
				if (!isMapped)
					result = BranchValidationResult.BranchNotMapped;
			}

			if (result == BranchValidationResult.Success)
			{
				var hasLocalChanges = HasLocalChanges(workspace, mergeInfoViewModel.TargetPath);
				if (hasLocalChanges)
					result = BranchValidationResult.ItemHasLocalChanges;
			}
			return result;
		}

		private static bool HasLocalChanges(Workspace workspace, string targetPath)
		{
			return workspace.GetPendingChangesEnumerable(targetPath, RecursionType.Full).Any();
		}

		private static bool UserHasAccess(VersionControlServer versionControlServer, string targetPath)
		{
			var permissions = versionControlServer.GetEffectivePermissions(versionControlServer.AuthorizedUser, targetPath);

			if (permissions == null || permissions.Length < 4)
				return false;

			return permissions.Contains("Read")
				&& permissions.Contains("PendChange")
				&& permissions.Contains("Checkin")
				&& permissions.Contains("Merge");
		}

		private static bool IsMerged(string sourcePath, string targetPath, IEnumerable<ExtendedMerge> trackMerges)
		{
			if (trackMerges == null)
				return false;

			return trackMerges.Any(m => (m.TargetItem.Item == sourcePath && m.SourceItem.Item.ServerItem == targetPath)
				|| (m.TargetItem.Item == targetPath && m.SourceItem.Item.ServerItem == sourcePath));
		}


		private static bool IsMapped(Workspace workspace, string targetItem)
		{
			return workspace.IsServerPathMapped(targetItem);
		}

		public async void MergeAndCheckInExecute()
		{
			try
			{
				IsBusy = true;

				var result = await Task.Run(() => MergeExecuteInternal(true, true));

				ClearNotifications();
				foreach (var resultModel in result)
				{
					var notificationType = NotificationType.Information;
					var message = string.Empty;
					var mergePath = string.Format("{0} -> {1}",
						resultModel.BranchInfo.SourceBranch,
						resultModel.BranchInfo.TargetBranch);
					switch (resultModel.MergeResult)
					{
						case MergeResult.CheckInEvaluateFail:
							notificationType = NotificationType.Error;
							message = "Check In evaluate failed";
							break;
						case MergeResult.CheckInFail:
							notificationType = NotificationType.Error;
							message = "Check In failed";
							break;
						case MergeResult.NothingMerge:
							notificationType = NotificationType.Warning;
							message = "Nothing merged";
							break;
						case MergeResult.UnresolvedConflicts:
							notificationType = NotificationType.Error;
							message = "Unresolved conflicts";
							break;
						case MergeResult.CanNotGetLatest:
							notificationType = NotificationType.Error;
							message = "Can not get lates";
							break;
						case MergeResult.Success:
							notificationType = NotificationType.Information;
							message = "Merge is successful";
							break;
					}
					message = string.Format("{0}: {1}", mergePath, message);
					if (!string.IsNullOrEmpty(message))
						ShowNotification(message, notificationType);
				}

				_eventAggregator.GetEvent<MergeCompleteEvent>().Publish(true);
			}
			catch (Exception ex)
			{
				ClearNotifications();
				ShowNotification(ex.Message, NotificationType.Error);
			}
			finally
			{
				IsBusy = false;
			}
		}

		public async void MergeWithoutCheckInExecute()
		{
			try
			{
				IsBusy = true;

				var result = await Task.Run(() => MergeExecuteInternal(false, false));

				ClearNotifications();
				var notCheckedIn = new List<MergeResultModel>(result.Count);
				foreach (var resultModel in result)
				{
					var notificationType = NotificationType.Information;
					var message = string.Empty;
					var mergePath = string.Format("{0} -> {1}",
						resultModel.BranchInfo.SourceBranch,
						resultModel.BranchInfo.TargetBranch);
					switch (resultModel.MergeResult)
					{
						case MergeResult.NothingMerge:
							notificationType = NotificationType.Warning;
							message = "Nothing merged";
							break;
						case MergeResult.UnresolvedConflicts:
							notificationType = NotificationType.Error;
							message = "Unresolved conflicts";
							break;
						case MergeResult.CanNotGetLatest:
							notificationType = NotificationType.Error;
							message = "Can not get lates";
							break;
						case MergeResult.NotCheckIn:
							message = "Files merge but not chekced in";
							notCheckedIn.Add(resultModel);
							break;
					}
					message = string.Format("{0}: {1}", mergePath, message);
					if (!string.IsNullOrEmpty(message))
						ShowNotification(message, notificationType);
				}

				if (notCheckedIn.Count > 0)
				{
					OpenPendingChanges(notCheckedIn);
				}
			}
			catch (Exception ex)
			{
				ClearNotifications();
				ShowNotification(ex.Message, NotificationType.Error);
			}
			finally
			{
				IsBusy = false;
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
					comment = resultModel.BranchInfo.Comment;
				else
					comment = comment + ";" + resultModel.BranchInfo.Comment;
				
				if (resultModel.PendingChanges != null && resultModel.PendingChanges.Count > 0)
					pendingChanges.AddRange(resultModel.PendingChanges);

				if (resultModel.HasConflicts)
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
			if (workItemIds != null && workItemIds.Count > 0)
			{
				var modelType = model.GetType();
				var method = modelType.GetMethod("AddWorkItemsByIdAsync",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				var workItemsIdsArray = workItemIds.ToArray();
				method.Invoke(model, new object[] { workItemsIdsArray, 1 /* Add */});
			}
		}

		private List<MergeResultModel> MergeExecuteInternal(bool checkInAfterMerge, bool resolveConficts)
		{
			var result = new List<MergeResultModel>();
			var context = Context;
			var tfs = context.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();

			var workspace = _workspace;

			var changesetService = _changesetService;
			var changeset = changesetService.GetChangeset(_changeset.ChangesetId);
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

			foreach (var mergeInfo in _branches.Where(b => b.Checked))
			{
				var mergeResultModel = MergeToBranch(mergeInfo, mergeOption, mergeRelationships, resolveConficts, workspace);
				mergeResultModel.WorkItemIds = workItemIds;
				result.Add(mergeResultModel);

				if (!checkInAfterMerge)
				{
					mergeResultModel.MergeResult = MergeResult.NotCheckIn;
				}

				if (mergeResultModel.MergeResult != MergeResult.Success)
				{
					continue;
				}

				var checkInResult = CheckIn(mergeResultModel.PendingChanges, mergeInfo, workspace, workItemIds, changeset.PolicyOverride, workItemStore);
				mergeResultModel.ChangesetId = checkInResult.ChangesetId;
				mergeResultModel.MergeResult = checkInResult.CheckinResult;

				if (mergeResultModel.MergeResult == MergeResult.Success)
				{
					mergeResultModel.CheckedIn = true;
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

		private static MergeResultModel MergeToBranch(MergeInfoViewModel mergeInfoeViewModel, MergeOption mergeOption,
			IEnumerable<ItemIdentifier> mergeRelationships, bool resolveConflict, Workspace workspace)
		{
			var result = new MergeResultModel
			{
				BranchInfo = mergeInfoeViewModel,
			};

			var mergeOptions = ToTfsMergeOptions(mergeOption);

			var source = mergeInfoeViewModel.SourcePath;
			var target = mergeInfoeViewModel.TargetPath;
			var version = mergeInfoeViewModel.ChangesetVersionSpec;

			var getLatestFiles = new List<string>();
			foreach (var mergeRelationship in mergeRelationships)
			{
				if (mergeRelationship.Item.StartsWith(mergeInfoeViewModel.TargetPath))
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
						result.MergeResult = MergeResult.CanNotGetLatest;
						result.Message = "Can not get latest";
						return result;
					}
				}
			}

			var status = workspace.Merge(source, target, version, version, LockLevel.None, RecursionType.Full, mergeOptions);

			if (HasConflicts(status))
			{
				var conflicts = AutoResolveConflicts(workspace, target);
				if (!conflicts.IsNullOrEmpty())
				{
					if (resolveConflict)
					{
						var resolved = ManualResolveConflicts(workspace, conflicts);
						if (!resolved)
						{
							result.MergeResult = MergeResult.UnresolvedConflicts;
							result.Message = "Unresolved conflicts";
							return result;
						}
					}
					else
					{
						result.HasConflicts = true;
					}
				}
			}

			var allPendingChanges = workspace.GetPendingChangesEnumerable(target, RecursionType.Full);
			var targetPendingChanges = allPendingChanges
				.Where(p => p.IsMerge && p.ServerItem.Contains(target))
				.ToList();

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

		private static Conflict[] AutoResolveConflicts(Workspace workspace, string targetPath)
		{
			var conflicts = workspace.QueryConflicts(new[] {targetPath}, true);
			if (conflicts.IsNullOrEmpty())
				return null;

			workspace.AutoResolveValidConflicts(conflicts, AutoResolveOptions.AllSilent);

			return workspace.QueryConflicts(new[] { targetPath }, true);
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
	}
}