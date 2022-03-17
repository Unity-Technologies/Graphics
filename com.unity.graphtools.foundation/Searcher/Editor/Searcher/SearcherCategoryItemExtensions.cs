namespace UnityEditor.GraphToolsFoundation.Searcher
{
    public static class SearcherCategoryItemExtensions
    {
        /// <summary>
        /// Gets the depth of this item by counting its parents.
        /// </summary>
        public static int GetDepth(this ISearcherTreeItemView self) => self.Parent?.GetDepth() + 1 ?? 0;

        /// <summary>
        /// Gets the path of this item by following its parents.
        /// </summary>
        public static string GetPath(this ISearcherTreeItemView self)
        {
            var parentPath = self.Parent?.Path;
            return string.IsNullOrEmpty(parentPath) ? self.Name : parentPath + "/" + self.Name;
        }
    }
}
