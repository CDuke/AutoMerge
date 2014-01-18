using System.Windows;
using System.Windows.Controls;

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
	}
}
