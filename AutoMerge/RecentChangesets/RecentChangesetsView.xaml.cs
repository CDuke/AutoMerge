using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoMerge
{
	/// <summary>
	/// Interaction logic for RecentChangesetsView.xaml
	/// </summary>
	public partial class RecentChangesetsView : UserControl
	{
		public RecentChangesetsView()
		{
			InitializeComponent();
		}

		/// <summary> 
		/// Parent section. 
		/// </summary> 
		public RecentChangesetSection ParentSection
		{
			get { return (RecentChangesetSection)GetValue(ParentSectionProperty); }
			set { SetValue(ParentSectionProperty, value); }
		}
		public static readonly DependencyProperty ParentSectionProperty =
			DependencyProperty.Register("ParentSection", typeof(RecentChangesetSection), typeof(RecentChangesetsView));

		private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ParentSection.ViewChangesetDetailsCommand.Execute(null);
		}

		private void List_KeyDown(object sender, KeyEventArgs e)
		{
			ParentSection.ViewChangesetDetailsCommand.Execute(null);
		}
	}
}
