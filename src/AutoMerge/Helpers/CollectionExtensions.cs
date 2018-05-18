using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AutoMerge.Helpers
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this Collection<T> list, IEnumerable<T> itemsToAdd)
        {
            foreach(var itemToAdd in itemsToAdd)
            {
                list.Add(itemToAdd);
            }
        }
    }
}
