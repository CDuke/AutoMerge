
using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;

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
