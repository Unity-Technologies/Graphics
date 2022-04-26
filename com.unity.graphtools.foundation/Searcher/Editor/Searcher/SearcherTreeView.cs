using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Searcher TreeView element to display <see cref="SearcherItem"/> in a collapsible hierarchy.
    /// </summary>
    class SearcherTreeView : ListView
    {
        public event Action<IReadOnlyList<ISearcherTreeItemView>> OnModelViewSelectionChange;

        public SearcherResultsViewMode ViewMode { get; set; }

        const string k_FavoriteCategoryStyleName = "favorite-category";
        const string k_FavoriteCategoryHelp = "Contains all the items marked as favorites for this search context.\n" +
                                    "You can add or remove favorites by clicking the star icon on each search item.";

        public const string itemClassName = "unity-item-library-item";
        public const string customItemClassName = "item-library-custom-item";
        public const string itemNameClassName = itemClassName + "__name-label";
        public const string itemPathClassName = itemClassName + "__path-label";
        public const string itemCategoryClassName = itemClassName + "--category";
        public const string CategoryIconSuffix = "__icon";
        public const string itemCategoryIconClassName = itemClassName + CategoryIconSuffix;
        public const string collapseButtonClassName = itemClassName + "__collapse-button";
        public const string collapseButtonCollapsedClassName = collapseButtonClassName + "--collapsed";
        public const string favoriteButtonClassName = itemClassName + "__favorite-button";
        public const string favoriteButtonFavoriteClassName = "favorite";

        const int k_IndentDepthFactor = 15;
        const string k_EntryName = "smartSearchItem";
        const string k_FavoriteButtonname = "favoriteButton";

        Searcher m_Searcher;
        Action<SearcherItem> m_SelectionCallback;
        HashSet<SearcherItem> m_MultiSelectSelection;
        Dictionary<SearcherItem, Toggle> m_SearchItemToVisualToggle;
        SearcherCategoryView m_FavoriteCategoryView;
        List<SearcherItem> m_Results;
        readonly VisualTreeAsset m_ItemTemplate;

        ISearcherCategoryView m_ResultsHierarchy;
        List<ISearcherTreeItemView> m_VisibleItems;
        Stack<ISearcherTreeItemView> m_RootItems;

        SearcherItem m_LastFavoriteClicked;

        double m_LastFavoriteClickTime;

        public SearcherTreeView()
        {
            m_MultiSelectSelection = new HashSet<SearcherItem>();
            m_SearchItemToVisualToggle = new Dictionary<SearcherItem, Toggle>();
            m_FavoriteCategoryView = new SearcherCategoryView("Favorites", null, k_FavoriteCategoryHelp, k_FavoriteCategoryStyleName);
            m_VisibleItems = new List<ISearcherTreeItemView>();
            m_RootItems = new Stack<ISearcherTreeItemView>();

            const string tpl = "Packages/com.unity.graphtools.foundation/Searcher/Editor/Templates/SearcherItem.uxml";
            m_ItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(tpl);
            if (m_ItemTemplate == null)
            {
                Debug.Log("Failed to load template " + tpl);
            }

            bindItem = Bind;
            unbindItem = UnBind;
            makeItem = MakeItem;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

#if UNITY_2022_2_OR_NEWER
            itemsChosen += _ => OnItemChosen();
            selectionChanged += _ => OnSelectionChanged();
#elif UNITY_2020_1_OR_NEWER
            onItemsChosen += obj => OnItemSelected((obj.FirstOrDefault() as ISearcherItemView)?.SearcherItem);
            onSelectionChange += OnSelectionChanged;
#else
            onItemChosen += obj => OnItemSelected((obj as ISearcherItemView)?.SearcherItem);
            onSelectionChanged += OnSelectionChanged;
#endif
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        public void Setup(Searcher searcher, Action<SearcherItem> selectionCallback)
        {
            m_Searcher = searcher;
            m_SelectionCallback = selectionCallback;

            // Add a single dummy SearcherItem to warn users that data is not ready to display yet
            m_VisibleItems = new List<ISearcherTreeItemView> { new PlaceHolderItemView() };
            RefreshListView();
        }

        void RegenerateVisibleResults()
        {
            m_VisibleItems.Clear();

            m_RootItems.Clear();

            for (int i = m_ResultsHierarchy.Items.Count - 1; i >= 0; i--)
            {
                m_RootItems.Push(m_ResultsHierarchy.Items[i]);
            }
            for (int i = m_ResultsHierarchy.SubCategories.Count - 1; i >= 0; i--)
            {
                m_RootItems.Push(m_ResultsHierarchy.SubCategories[i]);
            }

            if (ViewMode == SearcherResultsViewMode.Hierarchy)
            {
                m_FavoriteCategoryView.ClearItems();
                foreach (var favoriteItem in m_Searcher.CurrentFavorites.Where(f => m_Results.Contains(f)))
                {
                    m_FavoriteCategoryView.AddItem(new SearcherItemView(m_FavoriteCategoryView, favoriteItem));
                }

                m_RootItems.Push(m_FavoriteCategoryView);
            }

            while (m_RootItems.Count > 0)
            {
                var item = m_RootItems.Pop();
                m_VisibleItems.Add(item);
                if (item is ISearcherCategoryView category && !category.Collapsed)
                {
                    for (int i = category.Items.Count - 1; i >= 0; i--)
                    {
                        m_RootItems.Push(category.Items[i]);
                    }
                    for (int i = category.SubCategories.Count - 1; i >= 0; i--)
                    {
                        m_RootItems.Push(category.SubCategories[i]);
                    }
                }
            }

            RefreshListView();
        }

        internal void SetResults(IEnumerable<SearcherItem> results)
        {
            var firstItemWasSelected = selectedIndex == 0;

            m_Results = results.ToList();

            m_ResultsHierarchy = SearcherCategoryView.BuildViewModels(m_Results, ViewMode, m_Searcher.CategoryPathStyleNames);

            RegenerateVisibleResults();

            SelectItemInListView(0);

            // force selection callback if first viewmodel was already selected
            if (firstItemWasSelected)
                OnModelViewSelectionChange?.Invoke(m_VisibleItems.Take(1).ToList());
        }

        void OnKeyDownEvent(KeyDownEvent evt)
        {
            var categoryView = selectedItem as ISearcherCategoryView;

            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    OnItemChosen();
                    break;
                case KeyCode.LeftArrow:
                    if (categoryView != null)
                        Collapse(categoryView);
                    break;
                case KeyCode.RightArrow:
                    if (categoryView != null)
                        Expand(categoryView);
                    break;
                case KeyCode.UpArrow:
                    SelectItemInListView(selectedIndex - 1);
                    break;
                case KeyCode.DownArrow:
                    SelectItemInListView(selectedIndex + 1);
                    break;
                case KeyCode.PageUp:
                    SelectItemInListView(0);
                    break;
                case KeyCode.PageDown:
                    SelectItemInListView(itemsSource.Count - 1);
                    break;
            }
        }

        void SelectItemInListView(int index)
        {
            if (index >= 0 && index < itemsSource.Count)
            {
                selectedIndex = index;
                ScrollToItem(index);
            }
        }

        void RefreshListView()
        {
            itemsSource = m_VisibleItems;
            m_SearchItemToVisualToggle.Clear();
#if UNITY_2021_2_OR_NEWER
            Rebuild();
#else
            Refresh();
#endif
        }

        /// <summary>
        /// Prepares a <see cref="VisualElement"/> to be (re-)used as a list item.
        /// </summary>
        /// <param name="target">The <see cref="VisualElement"/> to bind.</param>
        /// <param name="index">Index of the item in the items list.</param>
        void Bind(VisualElement target, int index)
        {
            var item = m_VisibleItems[index];
            target.AddToClassList(itemClassName);
            var categoryView = item as ISearcherCategoryView;
            var itemView = item as ISearcherItemView;
            target.EnableInClassList(itemCategoryClassName, categoryView != null);
            if (!string.IsNullOrEmpty(item.StyleName))
                target.AddToClassList(GetItemCustomClassName(item));

            var indent = target.Q<VisualElement>("itemIndent");
            indent.style.width = item.Depth * k_IndentDepthFactor;

            var expander = target.Q<VisualElement>("itemChildExpander");

            var icon = expander.Query("expanderIcon").First();
            var iconElement = target.Q<VisualElement>("itemIconVisualElement");

            if (categoryView != null)
            {
                icon.AddToClassList(collapseButtonClassName);
                icon.EnableInClassList(collapseButtonCollapsedClassName, categoryView.Collapsed);
            }

            iconElement.AddToClassList(itemCategoryIconClassName);

            if (!string.IsNullOrEmpty(item.StyleName))
                iconElement.AddToClassList(GetItemCustomClassName(item) + CategoryIconSuffix);


            var nameLabelsContainer = target.Q<VisualElement>("labelsContainer");
            nameLabelsContainer.Clear();

            var nameLabel = new Label(item.Name);
            nameLabel.AddToClassList(itemNameClassName);
            nameLabelsContainer.Add(nameLabel);
            // TODO VladN: support highlight for parts of the string?
            // Highlight was disabled because it was inconsistent with fuzzy search
            // and with searching allowing to match item path (e.g. 'Debug/Log message' will be matched by DbgLM)
            // We need to figure out if there's a good way to highlight results.
            //    SearcherHighlighter.HighlightTextIndices(nameLabelsContainer, item.Name, item.lastMatchedIndices);

            if ((item.Parent == m_FavoriteCategoryView || ViewMode == SearcherResultsViewMode.Flat)
                && !string.IsNullOrEmpty(item.Path))
            {
                var pathLabel = new Label("(in " + item.Path + ")");
                pathLabel.AddToClassList(itemPathClassName);
                nameLabelsContainer.Add(pathLabel);
            }

            target.userData = item;
            target.name = k_EntryName;

            var favButton = target.Q(k_FavoriteButtonname);
            if (favButton != null && itemView != null)
            {
                favButton.AddToClassList(favoriteButtonClassName);

                favButton.EnableInClassList(favoriteButtonFavoriteClassName,
                    m_Searcher.IsFavorite(itemView.SearcherItem));

                favButton.RegisterCallback<PointerDownEvent>(ToggleFavorite);
            }

            var selectionToggle = target.Q<Toggle>("itemToggle");
            if (selectionToggle != null)
            {
                if (categoryView != null)
                    selectionToggle.RemoveFromHierarchy();
                else if (itemView != null)
                {
                    var searcherItem = itemView.SearcherItem;
                    selectionToggle.SetValueWithoutNotify(m_MultiSelectSelection.Contains(searcherItem));
                    m_SearchItemToVisualToggle[searcherItem] = selectionToggle;
                }
            }
            target.RegisterCallback<MouseDownEvent>(ExpandOrCollapse);
        }

        /// <summary>
        /// Clears things before a list item <see cref="VisualElement"/> is potentially reused for another item.
        /// </summary>
        /// <param name="target">The <see cref="VisualElement"/> to clean.</param>
        /// <param name="index">Index of the item in the items list.</param>
        void UnBind(VisualElement target, int index)
        {
            target.RemoveFromClassList(itemCategoryClassName);
            RemoveCustomClassIfFound(target);
            target.UnregisterCallback<MouseDownEvent>(ExpandOrCollapse);

            var expander = target.Q<VisualElement>("itemChildExpander");
            var icon = expander.Query("expanderIcon").First();
            var iconElement = target.Q<VisualElement>("itemIconVisualElement");

            icon.RemoveFromClassList(collapseButtonClassName);
            icon.RemoveFromClassList(collapseButtonCollapsedClassName);

            iconElement.RemoveFromClassList(itemCategoryIconClassName);
            RemoveCustomClassIfFound(iconElement);

            var favButton = target.Q(k_FavoriteButtonname);
            if (favButton != null)
            {
                favButton.RemoveFromClassList(favoriteButtonClassName);
                favButton.RemoveFromClassList(favoriteButtonFavoriteClassName);
                favButton.UnregisterCallback<PointerDownEvent>(ToggleFavorite);
            }

            void RemoveCustomClassIfFound(VisualElement visualElement)
            {
                var customClass = visualElement.GetClasses()
                    .FirstOrDefault(c => c.StartsWith(customItemClassName));
                if (customClass != null)
                    visualElement.RemoveFromClassList(customClass);
            }
        }

        static string GetItemCustomClassName(ISearcherTreeItemView item)
        {
            return string.IsNullOrEmpty(item.StyleName) ? null : customItemClassName + "-" + item.StyleName;
        }

        internal void ConfirmMultiselect()
        {
            if (m_MultiSelectSelection.Count == 0)
            {
                m_SelectionCallback(null);
                return;
            }
            foreach (SearcherItem item in m_MultiSelectSelection)
            {
                m_SelectionCallback(item);
            }
        }

        /// <summary>
        /// Clicks on favorite actually can't intercept the click on the list view.
        /// So we keep track off every click on favorites to prevent triggering selection when clicking favorites.
        /// </summary>
        bool SelectionIsInvalidOrAFavoriteClick()
        {
            var selectedSearcherItem = (selectedItem as ISearcherItemView)?.SearcherItem;
            if (EditorApplication.timeSinceStartup - m_LastFavoriteClickTime > .8)
                return false;

            return selectedSearcherItem == null || m_LastFavoriteClicked == selectedSearcherItem;
        }

        void OnSelectionChanged()
        {
            if (SelectionIsInvalidOrAFavoriteClick())
                return;

            if (!selectedItems.Any())
                m_SelectionCallback(null);
            else
                OnModelViewSelectionChange?.Invoke(selectedItems
                    .OfType<ISearcherTreeItemView>()
                    .ToList());
        }

        void OnItemChosen()
        {
            if (SelectionIsInvalidOrAFavoriteClick())
                return;

            var selectedSearcherItem = (selectedItem as ISearcherItemView)?.SearcherItem;
            if (selectedSearcherItem == null)
                m_SelectionCallback(null);
            else if (m_LastFavoriteClicked != selectedSearcherItem || EditorApplication.timeSinceStartup - m_LastFavoriteClickTime > 1.0)
            {
                if (!m_Searcher.Adapter.MultiSelectEnabled)
                {
                    m_SelectionCallback(selectedSearcherItem);
                }
                else
                {
                    ToggleItemForMultiSelect(selectedSearcherItem, !m_MultiSelectSelection.Contains(selectedSearcherItem));
                }
            }
        }

        void ToggleItemForMultiSelect(SearcherItem item, bool selected)
        {
            if (selected)
            {
                m_MultiSelectSelection.Add(item);
            }
            else
            {
                m_MultiSelectSelection.Remove(item);
            }

            if (m_SearchItemToVisualToggle.TryGetValue(item, out var toggle))
            {
                toggle.SetValueWithoutNotify(selected);
            }
        }

        VisualElement MakeItem()
        {
            var item = m_ItemTemplate.CloneTree();
            if (m_Searcher.Adapter.MultiSelectEnabled)
            {
                var selectionToggle = item.Q<Toggle>("itemToggle");
                if (selectionToggle != null)
                {
                    selectionToggle.RegisterValueChangedCallback(changeEvent =>
                    {
                        var searcherItem = item.userData as SearcherItem;
                        ToggleItemForMultiSelect(searcherItem, changeEvent.newValue);
                    });
                }
            }
            return item;
        }

        // ReSharper disable once UnusedMember.Local

        void RefreshListViewOn()
        {
            // TODO: Call ListView.Refresh() when it is fixed.
            // Need this workaround until then.
            // See: https://fogbugz.unity3d.com/f/cases/1027728/
            // And: https://gitlab.internal.unity3d.com/upm-packages/editor/com.unity.searcher/issues/9

            var scrollView = this.Q<ScrollView>();

            var scroller = scrollView?.Q<Scroller>("VerticalScroller");
            if (scroller == null)
                return;

            var oldValue = scroller.value;
            scroller.value = oldValue + 1.0f;
            scroller.value = oldValue - 1.0f;
            scroller.value = oldValue;
        }

        void Expand(ISearcherCategoryView itemView)
        {
            itemView.Collapsed = false;
            RegenerateVisibleResults();
        }

        void Collapse(ISearcherCategoryView itemView)
        {
            itemView.Collapsed = true;
            RegenerateVisibleResults();
        }

        void ToggleFavorite(PointerDownEvent evt)
        {
            // Check that we're clicking on a favorite
            if (!(evt.target is VisualElement target
                  && target.name == k_FavoriteButtonname
                  && target.parent?.parent?.userData is SearcherItemView item))
            {
                return;
            }

            // Prevent ListView from selecting the item under the favorite icon
            evt.StopPropagation();

            var searcherItem = item.SearcherItem;
            var wasFavorite = m_Searcher.IsFavorite(searcherItem);
            m_Searcher.SetFavorite(searcherItem, !wasFavorite);
            m_LastFavoriteClicked = searcherItem;
            m_LastFavoriteClickTime = EditorApplication.timeSinceStartup;
            target.EnableInClassList(favoriteButtonFavoriteClassName, !wasFavorite);

            RegenerateVisibleResults();

            // Compensate list shrinking/growing when we add/remove favorites.
            // Avoids having the selection and item under the mouse cursor to jump around when adding/removing favorites.
            if (!m_FavoriteCategoryView.Collapsed)
            {
                var scrollView = this.Q<ScrollView>();
                var scroller = scrollView?.Q<Scroller>();
                if (scroller != null)
                {
                    bool scrolledBot = scroller.value >= scroller.highValue;
                    if (!(scrolledBot && wasFavorite))
                    {
                        var selectionDelta = wasFavorite ? -1 : 1;
                        selectedIndex += selectionDelta;
#if UNITY_2021_2_OR_NEWER
                        var scrollerDelta = selectionDelta * fixedItemHeight;
#else
                        var scrollerDelta = selectionDelta * itemHeight;
#endif
                        scroller.value += scrollerDelta;
                    }
                }
            }
        }

        void ExpandOrCollapse(MouseDownEvent evt)
        {
            if (!(evt.target is VisualElement target))
                return;

            VisualElement itemElement = target.GetFirstAncestorOfType<TemplateContainer>();
            var expandingItemName = "expanderIcon";
            if (target.name != expandingItemName)
                target = itemElement.Q(expandingItemName);

            if (target == null || !(itemElement?.userData is ISearcherCategoryView item))
                return;

            if (item.Collapsed)
                Expand(item);
            else
                Collapse(item);

            evt.StopImmediatePropagation();
        }

        class PlaceHolderItemView : ISearcherTreeItemView
        {
            public ISearcherCategoryView Parent => null;
            public string StyleName => null;
            public int Depth => 0;
            public string Path => null;
            public string Name => "Indexing databases...";
            public string Help => "The Database is being indexed...";
        }
    }
}
