using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMerge.Prism.Command;
using Microsoft.TeamFoundation;

namespace AutoMerge.RecentChangesets.Solo
{
    public class RecentChangesetsSoloViewModel : RecentChangesetsViewModel
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

        public override async Task<List<ChangesetViewModel>> GetChangesets()
        {
            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider, Settings.Instance.ChangesetCount);
            var userLogin = VersionControlNavigationHelper.GetAuthorizedUser(ServiceProvider);

            return await changesetProvider.GetChangesets(userLogin);
        }

        private void ToggleAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = true;
                InvalidateCommands();
                ResetAddById();
                SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetIdTextBox);
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
                SetMvvmFocus(RecentChangesetFocusableControlNames.AddChangesetByIdLink);
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

        private async void AddChangesetByIdExecute()
        {
            ShowBusy();
            try
            {
                var changesetIds = GeChangesetIdsToAdd(ChangesetIdsText);
                if (changesetIds.Count > 0)
                {
                    var changesetProvider = new ChangesetByIdChangesetProvider(ServiceProvider, changesetIds);
                    var changesets = await changesetProvider.GetChangesets(null);

                    if (changesets.Count > 0)
                    {
                        Changesets.Add(changesets[0]);
                        SelectedChangeset = changesets[0];
                        SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetList);
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
    }
}
