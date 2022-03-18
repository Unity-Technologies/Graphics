using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for collections of <see cref="SearcherItem"/>s.
    /// </summary>
    [PublicAPI]
    public static class SearcherItemCollectionExtensions
    {
        /// <summary>
        /// Adds a <see cref="SearcherItem"/> in a hierarchy at a certain path.
        /// </summary>
        /// <param name="items">Hierarchy where to add the item.</param>
        /// <param name="item">Item to add.</param>
        /// <param name="path">Path where to add the item.</param>
        [Obsolete("Use SearcherItem constructor with categoryPath instead, SearcherItems aren't used to represent categories anymore.")]
        public static void AddAtPath(this List<SearcherItem> items, SearcherItem item, string path = "")
        {
            item.CategoryPath = path;
            items.Add(item);
        }
    }
}
