using System.Windows.Controls;
using Microsoft.TeamFoundation.Controls.MVVM;

namespace AutoMerge
{
	/// <summary>
	/// Interaction logic for RecentChangesetsView.xaml
	/// </summary>
	public partial class RecentChangesetsTeamView : UserControl
	{
		public RecentChangesetsTeamView()
		{
			InitializeComponent();
		}

		//public void SetFocus(string id, params object[] args)
		//{
		//	switch (id)
		//	{
		//		case RecentChangesetFocusableControlNames.AddChangesetByIdLink:
		//			addChangesetByIdLink.Focus();
		//			break;
		//		case RecentChangesetFocusableControlNames.ChangesetIdTextBox:
		//			changesetIdTextBox.FocusTextBox();
		//			changesetIdTextBox.TextBoxControl.SelectionStart = changesetIdTextBox.TextBoxControl.Text.Length;
		//			break;
		//		case RecentChangesetFocusableControlNames.ChangesetList:
		//			if (changesetList.SelectedItem != null)
		//			{
		//				changesetList.UpdateLayout();
		//				var item = changesetList.ItemContainerGenerator.ContainerFromIndex(changesetList.SelectedIndex);
		//				((ListBoxItem) item).Focus();
		//			}
		//			else
		//			{
		//				changesetList.Focus();
		//			}
		//			break;
		//	}
		//}
	}
}
