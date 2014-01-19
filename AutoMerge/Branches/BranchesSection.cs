using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMerge.Base;
using AutoMerge.Events;
using AutoMerge.Helpers;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace AutoMerge
{
	[TeamExplorerSection(SectionId, AutoMergePage.PageId, 20)]
	public class BranchesSection : TeamExplorerBaseSection
	{
		private readonly IEventAggregator _eventAggregator;
		public const string SectionId = "36BF6F52-F4AC-44A0-9985-817B2A65B3B0";

		private Changeset _changeset;

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

			MergeCommand = new DelegateCommand(DoMerge, CanMerge);

			_eventAggregator = EventAggregatorFactory.Get();
		}

		/// <summary>
		/// Get the view.
		/// </summary>
		protected BranchesView View
		{
			get { return SectionContent as BranchesView; }
		}

		public ObservableCollection<MergeInfoModel> Branches
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
		private ObservableCollection<MergeInfoModel> _branches;

		public ICommand MergeCommand { get; private set; }

		public override void Initialize(object sender, SectionInitializeEventArgs e)
		{
			base.Initialize(sender, e);

			_eventAggregator.GetEvent<SelectChangesetEvent>()
				.Subscribe(OnSelectedChangeset);
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
			try
			{
				// Set our busy flag and clear the previous data
				IsBusy = true;

				if (_changeset == null)
					return;

				var branches = new ObservableCollection<MergeInfoModel>();
				await Task.Run(() =>
				{
					branches = GetBranches(_changeset);
				});
				Branches = branches;
			}
			catch (Exception ex)
			{
				ShowNotification(ex.Message, NotificationType.Error);
			}
			finally
			{
				// Always clear our busy flag when done 
				IsBusy = false;
			}
		}

		private void OnSelectedChangeset(Changeset changeset)
		{
			_changeset = changeset;
			Refresh();
		}

		private ObservableCollection<MergeInfoModel> GetBranches(Changeset changeset)
		{
			var tfs = CurrentContext.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();

			var sourceBranches = versionControl.QueryBranchObjectOwnership(new []{changeset.ChangesetId});

			var sourceBranchIdentifier = sourceBranches[0].RootItem;
			var sourceBranchInfo = versionControl.QueryBranchObjects(sourceBranchIdentifier, RecursionType.None)[0];

			var result = new ObservableCollection<MergeInfoModel>();
			if (sourceBranchInfo.ChildBranches != null)
			{
				foreach (var childBranch in sourceBranchInfo.ChildBranches.Where(b => !b.IsDeleted))
				{
					var mergeInfo = new MergeInfoModel
					{
						SourceBranch = sourceBranchIdentifier.Item,
						TargetBranch = childBranch.Item
					};

					result.Add(mergeInfo);
				}
			}

			if (sourceBranchInfo.Properties != null && sourceBranchInfo.Properties.ParentBranch != null
				&& !sourceBranchInfo.Properties.ParentBranch.IsDeleted)
			{
				var mergeInfo = new MergeInfoModel
				{
					SourceBranch = sourceBranchIdentifier.Item,
					TargetBranch = sourceBranchInfo.Properties.ParentBranch.Item
				};

				result.Add(mergeInfo);
			}

			return result;
		}

		public void DoMerge()
		{
			var tfs = CurrentContext.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();
			
			var workspace = versionControl.QueryWorkspaces(null, tfs.AuthorizedIdentity.UniqueName, Environment.MachineName)[0];

			var workItemStore = tfs.GetService<WorkItemStore>();
			var workItems = GetWorkItemCheckinInfo(_changeset, workItemStore);

			var sourceChanges = versionControl.GetChangeset(_changeset.ChangesetId, true, false).Changes;
			var somethingMerged = false;
			foreach (var mergeInfo in _branches.Where(b => b.Checked))
			{
				var conflicts = new List<string>();
				var allTargetsFiles = new HashSet<string>();
				foreach (var change in sourceChanges)
				{
					var source = change.Item.ServerItem;
					var target = source.Replace(mergeInfo.SourceBranch, mergeInfo.TargetBranch);
					allTargetsFiles.Add(target);

					var status = workspace.Merge(source, target, null, null, LockLevel.None, RecursionType.None, MergeOptions.None);

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
						ShowNotification("Auto merge stoped, because conflict not resolved", NotificationType.Warning);
						break;
					}
				}

				var allPendingChanges = workspace.GetPendingChangesEnumerable();
				var targetPendingChanges = new List<PendingChange>();
				foreach (var pendingChange in allPendingChanges)
				{
					if (!allTargetsFiles.Contains(pendingChange.ServerItem))
						continue;

					targetPendingChanges.Add(pendingChange);
				}

				if (targetPendingChanges.Count > 0)
				{
					var comment = EvaluateComment(_changeset.Comment, mergeInfo.SourceBranch, mergeInfo.TargetBranch);
					var evaluateCheckIn = workspace.EvaluateCheckin2(CheckinEvaluationOptions.All, targetPendingChanges, comment, null, workItems);
					if (CanCheckIn(evaluateCheckIn))
					{
						var changesetId = 1;
						//var changesetId = workspace.CheckIn(targetPendingChanges.ToArray(), comment, null, workItems, null);
						if (changesetId <= 0)
						{
							ShowNotification("Check In fail", NotificationType.Error);
						}
						else
						{
							somethingMerged = true;
						}
					}
					else
					{
						ShowNotification("Check In evaluate failed", NotificationType.Error);
					}
				}

				var resultMessage = somethingMerged ? "Merge success" : "Nothing merged";
				ShowNotification(resultMessage, NotificationType.Information);
			}
		}

		public bool CanMerge()
		{
			return true;
			//return _branches.Any(b => b.Checked);
		}

		private static bool HasConflicts(GetStatus mergeStatus)
		{
			return !mergeStatus.NoActionNeeded && mergeStatus.NumConflicts > 0;
		}

		private bool ResolveConflict(Workspace workspace, string[] targetPath)
		{
			var conflicts = workspace.QueryConflicts(targetPath, false);
			if (conflicts.IsNullOrEmpty())
				return true;

			foreach (var conflict in conflicts)
			{
				if (workspace.MergeContent(conflict, true))
				{
					conflict.Resolution = Resolution.AcceptMerge;
					workspace.ResolveConflict(conflict);
				}
				if (conflict.IsResolved)
				{
					workspace.PendEdit(conflict.TargetLocalItem);
					File.Copy(conflict.MergedFileName, conflict.TargetLocalItem, true);
				}
				else
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

		private bool CanCheckIn(CheckinEvaluationResult checkinEvaluationResult)
		{
			return checkinEvaluationResult.Conflicts.IsNullOrEmpty()
				&& checkinEvaluationResult.NoteFailures.IsNullOrEmpty()
				&& checkinEvaluationResult.PolicyFailures.IsNullOrEmpty()
				&& checkinEvaluationResult.PolicyEvaluationException == null;
		}
	}
}