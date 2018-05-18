using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMerge
{
    public class TeamChangesetsViewModelContext : ChangesetsViewModelContext
    {
        public string SourceBranch { get; set; }

        public string TargetBranch { get; set; }

        public string SelectedProjectName { get; set; }
    }
}
