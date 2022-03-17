namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// View model for a <see cref="SearcherItem"/> in the Searcher Tree View.
    /// </summary>
    public interface ISearcherItemView : ISearcherTreeItemView
    {
        /// <summary>
        /// The <see cref="SearcherItem"/> represented by this view.
        /// </summary>
        public SearcherItem SearcherItem { get; }
    }
}
