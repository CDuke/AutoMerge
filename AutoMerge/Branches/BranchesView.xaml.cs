using System.Windows;
using System.Windows.Controls;

namespace AutoMerge
{
	/// <summary>
	/// Interaction logic for BranchesView.xaml
	/// </summary>
	public partial class BranchesView : UserControl
	{
		public BranchesView()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Parent section.
		/// </summary>
		public BranchesSection ParentSection
		{
			get { return (BranchesSection)GetValue(ParentSectionProperty); }
			set { SetValue(ParentSectionProperty, value); }
		}
		public static readonly DependencyProperty ParentSectionProperty =
			DependencyProperty.Register("ParentSection", typeof(BranchesSection), typeof(BranchesView));

		private void Merge(object sender, RoutedEventArgs e)
		{
			ParentSection.MergeCommand.Execute(null);
		}
	}
}
