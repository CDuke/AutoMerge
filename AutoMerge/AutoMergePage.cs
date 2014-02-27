using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

namespace AutoMerge
{
	[TeamExplorerPage(GuidList.AutoMergePageId)]
	public class AutoMergePage : TeamExplorerPageBase
	{

		/// <summary>
		/// Constructor.
		/// </summary>
		public AutoMergePage()
		{ 
			Title = Resources.AutoMergePageName;
		}
	}
}