using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMerge.Prism.Command;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge.RecentChangesets.Solo
{
    public class RecentChangesetsSoloViewModel : ChangesetsViewModel
    {
        public RecentChangesetsSoloViewModel(ILogger logger) : base(logger)
        {
            ToggleAddByIdCommand = new DelegateCommand(ToggleAddByIdExecute, ToggleAddByIdCanExecute);
            CancelAddChangesetByIdCommand = new DelegateCommand(CancelAddByIdExecute);
            AddChangesetByIdCommand = new DelegateCommand(AddChangesetByIdExecute, AddChangesetByIdCanExecute);
        }

        public DelegateCommand ToggleAddByIdCommand { get; private set; }
        public DelegateCommand CancelAddChangesetByIdCommand { get; private set; }
        public DelegateCommand AddChangesetByIdCommand { get; private set; }

        public bool ShowAddByIdChangeset
        {
            get
            {
                return _showAddByIdChangeset;
            }
            set
            {
                _showAddByIdChangeset = value;
                RaisePropertyChanged("ShowAddByIdChangeset");
            }
        }
        private bool _showAddByIdChangeset;

        public string ChangesetIdsText
        {
            get
            {
                return _changesetIdsText;
            }
            set
            {
                _changesetIdsText = value;
                RaisePropertyChanged("ChangesetIdsText");
                InvalidateCommands();
            }
        }
        private string _changesetIdsText;

        public override async Task<List<ChangesetViewModel>> GetChangesetsAsync()
        {
            var userLogin = VersionControlNavigationHelper.GetAuthorizedUser(ServiceProvider);
            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider, Settings.Instance.ChangesetCount, userLogin);

            return await changesetProvider.GetChangesets();
        }

        private void ToggleAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = true;
                InvalidateCommands();
                ResetAddById();
                SetMvvmFocus(ChangesetFocusableControlNames.ChangesetIdTextBox);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                throw;
            }
        }

        private bool ToggleAddByIdCanExecute()
        {
            return !ShowAddByIdChangeset;
        }

        private void CancelAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = false;
                InvalidateCommands();
                SetMvvmFocus(ChangesetFocusableControlNames.AddChangesetByIdLink);
                ResetAddById();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ResetAddById()
        {
            ChangesetIdsText = string.Empty;
        }

        protected override async Task RefreshAsync()
        {
            await GetChangesetAndUpdateTitleAsync();
        }

        private async void AddChangesetByIdExecute()
        {
            ShowBusy();
            try
            {
                var changesetIds = GeChangesetIdsToAdd(ChangesetIdsText);
                if (changesetIds.Count > 0)
                {
                    var changesetProvider = new ChangesetByIdChangesetProvider(ServiceProvider, changesetIds);
                    var changesets = await changesetProvider.GetChangesets();

                    if (changesets.Count > 0)
                    {
                        Changesets.Add(changesets[0]);
                        SelectedChangeset = changesets[0];
                        SetMvvmFocus(ChangesetFocusableControlNames.ChangesetList);
                        UpdateTitle();
                    }
                    ShowAddByIdChangeset = false;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            HideBusy();
        }

        private bool AddChangesetByIdCanExecute()
        {
            try
            {
                return GeChangesetIdsToAdd(ChangesetIdsText).Count > 0;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                TeamFoundationTrace.TraceException(ex);
            }
            return false;
        }

        private static List<int> GeChangesetIdsToAdd(string text)
        {
            var list = new List<int>();
            var idsStrArray = string.IsNullOrEmpty(text) ? new string[0] : text.Split(new[] { ',', ';' });
            if (idsStrArray.Length > 0)
            {
                foreach (var idStr in idsStrArray)
                {
                    int result;
                    if (int.TryParse(idStr.Trim(), out result) && result > 0)
                        list.Add(result);
                }
            }
            return list;
        }

        protected override void InvalidateCommands()
        {
            base.InvalidateCommands();

            ToggleAddByIdCommand.RaiseCanExecuteChanged();
            CancelAddChangesetByIdCommand.RaiseCanExecuteChanged();
            AddChangesetByIdCommand.RaiseCanExecuteChanged();
        }

        public override void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            base.SaveContext(sender, e);

            var context = new RecentChangesetsSoloViewModelContext
            {
                ChangesetIdsText = ChangesetIdsText,
                Changesets = Changesets,
                SelectedChangeset = SelectedChangeset,
                ShowAddByIdChangeset = ShowAddByIdChangeset,
                Title = Title
            };

            e.Context = context;
        }

        protected override void RestoreContext(SectionInitializeEventArgs e)
        {
            var context = (RecentChangesetsSoloViewModelContext)e.Context;

            ChangesetIdsText = context.ChangesetIdsText;
            Changesets = context.Changesets;
            SelectedChangeset = context.SelectedChangeset;
            ShowAddByIdChangeset = context.ShowAddByIdChangeset;
            Title = context.Title;
        }

        protected override string BaseTitle
        {
            get
            {
                return Resources.RecentChangesetSectionName;
            }
        }
    }
}
