using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
    public class MergeRelation
    {
        public string Item { get; set; }

        public string Source { get; set; }

        public string Target { get; set; }

        public ItemType TargetItemType { get; set; }

        public string GetLatesPath { get; set; }
    }
}
