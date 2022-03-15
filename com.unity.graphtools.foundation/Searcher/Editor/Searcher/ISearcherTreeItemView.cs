namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// View model for any item in the searcher tree view.
    /// </summary>
    public interface ISearcherTreeItemView
    {
        /// <summary>
        /// Parent of this item in the hierarchy.
        /// </summary>
        public ISearcherCategoryView Parent { get; }

        /// <summary>
        /// Custom name used to generate USS styles when creating UI for this item.
        /// </summary>
        public string StyleName { get; }

        /// <summary>
        /// Depth of this item in the hierarchy.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Path in the hierarchy of items.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Name of the Item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Help content to display about this item.
        /// </summary>
        public string Help { get; }
    }
}
