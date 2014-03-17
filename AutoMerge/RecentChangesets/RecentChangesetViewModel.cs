using System.Collections.ObjectModel;
using System.Windows.Input;
using AutoMerge.Events;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Task = System.Threading.Tasks.Task;

using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
	public class RecentChangesetViewModel : TeamExplorerSectionViewModelBase
	{
		private readonly string _baseTitle;
		private readonly IEventAggregator _eventAggregator;

		public RecentChangesetViewModel()
		{
			Title = Resources.RecentChangesetSectionName;
			IsVisible = true;
			IsExpanded = true;
			IsBusy = false;
			Changesets = new ObservableCollection<ChangesetViewModel>();
			_baseTitle = Title;

			_eventAggregator = EventAggregatorFactory.Get();
				_eventAggregator.GetEvent<MergeCompleteEvent>()
					.Subscribe(OnMergeComplete);

			ViewChangesetDetailsCommand = new DelegateCommand(ViewChangesetDetailsExecute, ViewChangesetDetailsCanExecute);
		}

		public ChangesetViewModel SelectedChangeset
		{
			get
			{
				return _selectedChangeset;
			}
			set
			{
				_selectedChangeset = value;
				RaisePropertyChanged("SelectedChangeset");
				_eventAggregator.GetEvent<SelectChangesetEvent>().Publish(value);
			}
		}
		private ChangesetViewModel _selectedChangeset;

		/// <summary>
		/// List of changesets.
		/// </summary>
		public ObservableCollection<ChangesetViewModel> Changesets
		{
			get
			{
				return _changesets;
			}
			protected set
			{
				_changesets = value;
				RaisePropertyChanged("Changesets");
			}
		}
		private ObservableCollection<ChangesetViewModel> _changesets;


		public ICommand ViewChangesetDetailsCommand { get; private set; }

		private void ViewChangesetDetailsExecute()
		{
			var changesetId = SelectedChangeset.ChangesetId;
			TeamExplorerUtils.Instance.NavigateToPage(TeamExplorerPageIds.ChangesetDetails, ServiceProvider, changesetId);
		}

		private bool ViewChangesetDetailsCanExecute()
		{
			return SelectedChangeset != null;
		}

		private async void OnMergeComplete(bool obj)
		{
			await RefreshAsync();
		}

		protected override async Task InitializeAsync(object sender, SectionInitializeEventArgs e)
		{
			await RefreshAsync();
		}

		protected override async Task RefreshAsync()
		{
			Changesets.Clear();

			var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider);
			var changesets = await changesetProvider.GetChangesets();

			Changesets = new ObservableCollection<ChangesetViewModel>(changesets);
			Title = Changesets.Count > 0
				? string.Format("{0} ({1})", _baseTitle, Changesets.Count)
				: _baseTitle;

			if (Changesets.Count > 0)
				SelectedChangeset = Changesets[0];
		}
	}
}