using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMerge.Base;
using AutoMerge.Events;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;

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
					branches = GetBranchesAsync(_changeset);
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

		private ObservableCollection<MergeInfoModel> GetBranchesAsync(Changeset changeset)
		{
			var tfs = CurrentContext.TeamProjectCollection;
			var versionControl = tfs.GetService<VersionControlServer>();

			var sourceBranches = versionControl.QueryBranchObjectOwnership(new []{changeset.ChangesetId});

			var sourceBranchIdentifier = sourceBranches[0].RootItem;
			var sourceBranchInfo = versionControl.QueryBranchObjects(sourceBranchIdentifier, RecursionType.None)[0];

			var result = new ObservableCollection<MergeInfoModel>();
			if (sourceBranchInfo.ChildBranches != null)
			{
				foreach (var childBranch in sourceBranchInfo.ChildBranches)
				{
					var mergeInfo = new MergeInfoModel
					{
						SourceBranch = sourceBranchIdentifier.Item,
						TargetBranch = childBranch.Item
					};

					result.Add(mergeInfo);
				}
			}

			if (sourceBranchInfo.Properties != null && sourceBranchInfo.Properties.ParentBranch != null)
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

		private void DoMerge()
		{
			
		}

		private bool CanMerge()
		{
			return true;
			//return _branches.Any(b => b.Checked);
		}

/*		private static string GetBranchName(ItemIdentifier branch)
		{
			var pos = branch.Item.LastIndexOf('/');
			var name = branch.Item.Substring(pos + 1);
			return name;
		}*/
	}
}