using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
    [TeamExplorerPage(GuidList.AutoMergeTeamPageId)]
    public class AutoMergeTeamPage : TeamExplorerPageBase
    {
        public AutoMergeTeamPage()
        {
            Title = Resources.AutoMergeTeamPageName;
        }
    }
}
