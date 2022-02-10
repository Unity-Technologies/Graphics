namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Base class to filter search databases.
    /// </summary>
    public abstract class SearcherFilter
    {
        /// <summary>
        /// Checks if an item matches the filter.
        /// </summary>
        /// <param name="item">item the check</param>
        /// <returns>true if the item matches the filter, false otherwise.</returns>
        public abstract bool Match(SearcherItem item);
    }
}
