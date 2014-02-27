using System;
using System.ComponentModel.Composition;
using AutoMerge.Base;
using AutoMerge.VersionControl;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;

namespace AutoMerge
{
	[TeamExplorerNavigationItem(GuidList.AutoMergeNavigationItemId, 210, TargetPageId = GuidList.AutoMergePageId)]
	public class AutoMergeNavigationItem : PageNavigationItemBase
	{

		[ImportingConstructor]
		public AutoMergeNavigationItem(
			[Import(typeof(SVsServiceProvider))]
			IServiceProvider serviceProvider)
			: base(serviceProvider, GuidList.AutoMergePageId, VersionControlProvider.TeamFoundation)
		{
			Text = Resources.AutoMergePageName;
			Image = Resources.MergeImage;
		}
	}
}