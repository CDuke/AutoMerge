using System;
using System.ComponentModel.Composition;
using AutoMerge.Base;
using AutoMerge.VersionControl;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;

namespace AutoMerge
{
    [TeamExplorerNavigationItem(GuidList.AutoMergeTeamNavigationItemId, 211, TargetPageId = GuidList.AutoMergeTeamPageId)]
    class AutoMergeTeamNavigationItem : TeamExplorerNavigationItemBase
    {
        [ImportingConstructor]
        public AutoMergeTeamNavigationItem(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider)
            : base(serviceProvider, GuidList.AutoMergeTeamPageId, VersionControlProvider.TeamFoundation)
        {
            Text = Resources.AutoMergeTeamPageName;
            Image = Resources.MergeImage;
        }
    }
}
