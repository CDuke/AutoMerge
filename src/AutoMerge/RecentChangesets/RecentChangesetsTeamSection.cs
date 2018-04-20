using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;

namespace AutoMerge.RecentChangesets
{
    [TeamExplorerSection(GuidList.RecentChangesetsTeamSectionId, GuidList.AutoMergeTeamPageId, 11)]
    public class RecentChangesetsTeamSection : TeamExplorerSectionBase
    {
        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            return base.CreateViewModel(e) ?? new RecentChangesetsTeamViewModel(new VsLogger(ServiceProvider));
        }

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return new RecentChangesetsTeamView();
        }
    }
}
