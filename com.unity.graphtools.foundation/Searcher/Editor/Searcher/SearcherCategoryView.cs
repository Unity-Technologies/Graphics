using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Way to display elements in a list view.
    /// </summary>
    public enum SearcherResultsViewMode
    {
        /// <summary>
        /// Elements are displayed one after the other.
        /// </summary>
        Flat,

        /// <summary>
        /// Elements follow a hierarchy.
        /// </summary>
        Hierarchy
    }

    /// <summary>
    /// View model for a Category in the searcher Tree View.
    /// <remarks>Basic implementation of <see cref="ISearcherCategoryView"/>.</remarks>
    /// </summary>
    public class SearcherCategoryView: ISearcherCategoryView
    {
        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public ISearcherCategoryView Parent { get; }

        /// <inheritdoc />
        public bool Collapsed { get; set; }

        /// <inheritdoc />
        public string StyleName { get; }

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
        public string Path => m_Path ??= this.GetPath();

        /// <inheritdoc />
        public string Help { get; }

        /// <inheritdoc />
        public IReadOnlyList<ISearcherCategoryView> SubCategories => m_SubCategories;

        /// <inheritdoc />
        public IReadOnlyList<ISearcherItemView> Items => m_Items;

        int m_Depth = -1;

        string m_Path;

        List<ISearcherTreeItemView> m_Children;

        List<ISearcherCategoryView> m_SubCategories;

        List<ISearcherItemView> m_Items;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearcherCategoryView"/> class.
        /// </summary>
        /// <param name="name">Name of the category.</param>
        /// <param name="parent">Category under which to display this category. Can be <c>null</c>.</param>
        /// <param name="help">Help text explaining what this category is.</param>
        /// <param name="styleName">Custom name used to generate USS styles when creating UI for this item.</param>
        public SearcherCategoryView(string name, ISearcherCategoryView parent = null, string help = null, string styleName = null)
        {
            Parent = parent;
            Name = name;
            Help = help;
            StyleName = styleName;

            m_SubCategories = new List<ISearcherCategoryView>();
            m_Items = new List<ISearcherItemView>();
        }

        /// <inheritdoc />
        public void ClearItems()
        {
            m_Items.Clear();
        }

        /// <inheritdoc />
        public void AddItem(ISearcherItemView itemView)
        {
            m_Items.Add(itemView);
        }

        /// <inheritdoc />
        public void AddSubCategory(ISearcherCategoryView category)
        {
            m_SubCategories.Add(category);
        }

        /// <inheritdoc />
        public void ClearSubCategories()
        {
            m_SubCategories.Clear();
        }

        /// <summary>
        /// Creates a <see cref="SearcherCategoryView"/> populated with view models for several <see cref="SearcherItem"/>.
        /// </summary>
        /// <param name="items">The list of <see cref="SearcherItem"/> to build the view model from.</param>
        /// <param name="viewMode">If set to <see cref="SearcherResultsViewMode.Hierarchy"/>, builds category view models following items <see cref="SearcherItem.CategoryPath"/>.
        ///     Otherwise, items are displayed one by one at the same level.</param>
        /// <param name="categoryPathStyleNames">Style names to apply to categories</param>
        /// <returns>A root <see cref="SearcherCategoryView"/> with view models representing each searcher items.</returns>
        public static SearcherCategoryView BuildViewModels(IEnumerable<SearcherItem> items,
            SearcherResultsViewMode viewMode, IReadOnlyDictionary<string, string> categoryPathStyleNames = null)
        {
            var rootCategory = new SearcherCategoryView("Root");
            foreach (var searcherItem in items)
            {
                ISearcherCategoryView parentCategory = null;
                if (viewMode == SearcherResultsViewMode.Hierarchy && !string.IsNullOrEmpty(searcherItem.CategoryPath))
                    parentCategory = RetrieveOrCreatePath(searcherItem, rootCategory, categoryPathStyleNames);

                var itemView = new SearcherItemView(parentCategory, searcherItem);

                if (parentCategory == null)
                    rootCategory.AddItem(itemView);
                else
                    parentCategory.AddItem(itemView);
            }

            return rootCategory;
        }

        static ISearcherCategoryView RetrieveOrCreatePath(SearcherItem searcherItem,
            ISearcherCategoryView rootCategory, IReadOnlyDictionary<string, string> categoryPathStyleNames)
        {
            var pathParts = searcherItem.GetParentCategories();
            if (pathParts.Length == 0)
                return null;

            var potentialCategories = rootCategory.SubCategories;
            ISearcherCategoryView parentCategory = null;
            int i = 0;

            for (;i < pathParts.Length && potentialCategories.Count > 0; ++i)
            {
                var foundCat = potentialCategories.FirstOrDefault(c => c.Name == pathParts[i]);
                if (foundCat == null)
                    break;
                parentCategory = foundCat;
                potentialCategories = foundCat.SubCategories;
            }

            var currentPath = pathParts[0];
            for (int j = 1 ; j < i && j < pathParts.Length; j++)
                currentPath += SearcherItem.CategorySeparator + pathParts[j];

            for (; i < pathParts.Length; i++)
            {
                string styleName = null;
                categoryPathStyleNames?.TryGetValue(currentPath, out styleName);
                var newCategory = new SearcherCategoryView(pathParts[i], parentCategory, styleName: styleName);
                if (i + 1 < pathParts.Length)
                    currentPath += SearcherItem.CategorySeparator + pathParts[i + 1];
                parentCategory?.AddSubCategory(newCategory);
                parentCategory = newCategory;
                if (i == 0)
                    rootCategory.AddSubCategory(parentCategory);
            }
            return parentCategory;
        }
    }
}
