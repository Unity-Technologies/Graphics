using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for <see cref="SearcherItem"/>.
    /// </summary>
    public static class SearcherItemExtensions
    {
        /// <summary>
        /// Finds a <see cref="SearcherItem"/> nested under another one.
        /// </summary>
        /// <param name="item">The root parent item where to search.</param>
        /// <param name="name">The name of the item to find.</param>
        /// <returns>The item if found, null otherwise.</returns>
        [CanBeNull]
        public static SearcherItem Find(this SearcherItem item, string name)
        {
            if (item.Name == name)
                return item;

            foreach (var child in item.Children)
            {
                var found = child.Find(name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
