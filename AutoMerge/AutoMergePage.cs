using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
	[TeamExplorerPage(PageId)]
	public class AutoMergePage : TeamExplorerBasePage
	{
		#region Members
 
		public const string PageId = "3B582638-5F12-4715-8719-5E5777AB4581";
 
		#endregion
 
		/// <summary>
		/// Constructor.
		/// </summary>
		public AutoMergePage()
		{ 
			Title = Resources.AutoMergePageName;
			PageContent = new AutoMergePageView();
		}
	}
}