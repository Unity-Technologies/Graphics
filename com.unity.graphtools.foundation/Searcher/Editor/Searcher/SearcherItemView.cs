namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// View model for a <see cref="SearcherItem"/> in the searcher Tree View.
    /// <remarks>Basic implementation of <see cref="ISearcherItemView"/>.</remarks>
    /// </summary>
    public class SearcherItemView: ISearcherItemView
    {
        /// <inheritdoc />
        public string Name => SearcherItem.Name;

        /// <inheritdoc />
        public ISearcherCategoryView Parent { get; }

        /// <inheritdoc />
        public SearcherItem SearcherItem { get; }

        /// <inheritdoc />
        public int Depth
        {
            get
            {
                if (m_Depth == -1)
                {
                    m_Depth = this.GetDepth();
                }
                return m_Depth;
            }
        }

        /// <inheritdoc />
        public string Path => SearcherItem.CategoryPath;

        /// <inheritdoc />
        public string Help => SearcherItem.Help;

        /// <inheritdoc />
        public string StyleName => SearcherItem.StyleName;

        int m_Depth = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearcherItemView"/> class.
        /// </summary>
        /// <param name="parent">Category under which to display this item. Can be <c>null</c>.</param>
        /// <param name="searcherItem">The SearcherItem represented by this view.</param>
        public SearcherItemView(ISearcherCategoryView parent, SearcherItem searcherItem)
        {
            Parent = parent;
            SearcherItem = searcherItem;
        }
    }
}
