using System.Windows.Controls;
using Microsoft.TeamFoundation.Controls.MVVM;

namespace AutoMerge
{
	/// <summary>
	/// Interaction logic for RecentChangesetsView.xaml
	/// </summary>
	public partial class RecentChangesetsSoloView : UserControl, IFocusService
	{
		public RecentChangesetsSoloView()
		{
			InitializeComponent();
		}

		public void SetFocus(string id, params object[] args)
		{
			switch (id)
			{
				case ChangesetFocusableControlNames.AddChangesetByIdLink:
					addChangesetByIdLink.Focus();
					break;
				case ChangesetFocusableControlNames.ChangesetIdTextBox:
					changesetIdTextBox.FocusTextBox();
					changesetIdTextBox.TextBoxControl.SelectionStart = changesetIdTextBox.TextBoxControl.Text.Length;
					break;
				case ChangesetFocusableControlNames.ChangesetList:
					if (changesetList.SelectedItem != null)
					{
						changesetList.UpdateLayout();
						var item = changesetList.ItemContainerGenerator.ContainerFromIndex(changesetList.SelectedIndex);
						((ListBoxItem) item).Focus();
					}
					else
					{
						changesetList.Focus();
					}
					break;
			}
		}
	}
}
