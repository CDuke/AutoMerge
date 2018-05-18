using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMerge
{
    public class RecentChangesetsSoloViewModelContext : ChangesetsViewModelContext
    {
        public bool ShowAddByIdChangeset { get; set; }

        public string ChangesetIdsText { get; set; }
    }
}
