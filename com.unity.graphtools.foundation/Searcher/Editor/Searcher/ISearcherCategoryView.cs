using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// View model for Searcher Categories.
    /// </summary>
    [PublicAPI]
    public interface ISearcherCategoryView : ISearcherTreeItemView
    {
        /// <summary>
        /// Whether this Category should appear collapsed or expanded.
        /// </summary>
        public bool Collapsed { get; set; }

        /// <summary>
        /// Categories to display as children of this one.
        /// </summary>
        public IReadOnlyList<ISearcherCategoryView> SubCategories { get; }

        /// <summary>
        /// Items to display as children of this view.
        /// </summary>
        public IReadOnlyList<ISearcherItemView> Items { get; }

        /// <summary>
        /// Add a <see cref="ISearcherItemView"/> as a child of this category.
        /// </summary>
        /// <param name="item">The item to add as a child.</param>
        public void AddItem(ISearcherItemView item);

        /// <summary>
        /// Remove every non-category item under this category.
        /// </summary>
        public void ClearItems();

        /// <summary>
        /// Add a <see cref="ISearcherCategoryView"/> as a child of this category.
        /// </summary>
        /// <param name="category">The category to add as a child.</param>
        public void AddSubCategory(ISearcherCategoryView category);

        /// <summary>
        /// Remove every subcategory under this category.
        /// </summary>
        public void ClearSubCategories();
    }
}
