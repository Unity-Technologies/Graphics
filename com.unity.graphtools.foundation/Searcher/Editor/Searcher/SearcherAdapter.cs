using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// The state of a collapsible element.
    /// </summary>
    public enum ItemExpanderState
    {
        Hidden,
        Collapsed,
        Expanded
    }

    /// <summary>
    /// Provides ways to customize the searching interface.
    /// </summary>
    [PublicAPI]
    public interface ISearcherAdapter
    {
        VisualElement MakeItem();
        VisualElement Bind(VisualElement target, SearcherItem item, ItemExpanderState expanderState, string text);

        string Title { get; }

        /// <summary>
        /// Unique human-readable name for the searcher created by this adapter.
        /// <remarks>Used to separate preferences between Searchers.</remarks>
        /// </summary>
        string SearcherName { get; }

        bool HasDetailsPanel { get; }

        bool AddAllChildResults { get; }

        bool MultiSelectEnabled { get; }

        float InitialSplitterDetailRatio { get; }

        void OnSelectionChanged(IEnumerable<SearcherItem> items);

        void InitDetailsPanel(VisualElement detailsPanel);
    }

    /// <summary>
    /// Default implementation for <see cref="ISearcherAdapter"/>.
    /// </summary>
    [PublicAPI]
    public class SearcherAdapter : ISearcherAdapter
    {
        const string k_EntryName = "smartSearchItem";
        protected const string k_DetailsTitleClassName = "unity-label__searcher-details-title";
        protected const string k_DetailsTextClassName = "unity-label__searcher-details-text";

        const int k_IndentDepthFactor = 15;

        readonly VisualTreeAsset m_DefaultItemTemplate;
        public virtual string Title { get; }

        /// <inheritdoc />
        public virtual string SearcherName { get; }
        public virtual bool HasDetailsPanel => true;
        public virtual bool AddAllChildResults => true;
        public virtual bool MultiSelectEnabled => false;

        protected Label m_DetailsTitleLabel;
        protected Label m_DetailsTextLabel;

        public virtual float InitialSplitterDetailRatio => 1.0f;

        public SearcherAdapter(string title, string toolName = null)
        {
            Title = title;
            SearcherName = toolName;
            const string tpl = "Packages/com.unity.graphtools.foundation/Searcher/Editor/Templates/SearcherItem.uxml";
            m_DefaultItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(tpl);
            if (m_DefaultItemTemplate == null)
            {
                Debug.Log("Failed to load template " + tpl);
            }
        }

        public virtual VisualElement MakeItem()
        {
            // Create a visual element hierarchy for this search result.
            var item = m_DefaultItemTemplate.CloneTree();
            return item;
        }

        public virtual VisualElement Bind(VisualElement element, SearcherItem item, ItemExpanderState expanderState, string query)
        {
            var indent = element.Q<VisualElement>("itemIndent");
            indent.style.width = item.Depth * k_IndentDepthFactor;

            var expander = element.Q<VisualElement>("itemChildExpander");

            var icon = expander.Query("expanderIcon").First();
            icon.ClearClassList();

            switch (expanderState)
            {
                case ItemExpanderState.Expanded:
                    icon.AddToClassList("Expanded");
                    break;

                case ItemExpanderState.Collapsed:
                    icon.AddToClassList("Collapsed");
                    break;
            }

            var nameLabelsContainer = element.Q<VisualElement>("labelsContainer");
            nameLabelsContainer.Clear();

            var iconElement = element.Q<VisualElement>("itemIconVisualElement");
            iconElement.style.backgroundImage = item.Icon;
            if (item.Icon == null && item.CollapseEmptyIcon)
            {
                iconElement.style.display = DisplayStyle.None;
            }
            else
            {
                iconElement.style.display = DisplayStyle.Flex;
            }

            nameLabelsContainer.Add(new Label(item.Name));
            // TODO VladN: support highlight for parts of the string?
            // Highlight was disabled because it was inconsistent with fuzzy search
            // and with searching allowing to match item path (e.g. 'Debug/Log message' will be matched by DbgLM)
            // We need to figure out if there's a good way to highlight results.
            //    SearcherHighlighter.HighlightTextIndices(nameLabelsContainer, item.Name, item.lastMatchedIndices);

            element.userData = item;
            element.name = k_EntryName;

            return expander;
        }

        /// <summary>
        /// Creates a Title for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a title in the details panel.</returns>
        protected static Label MakeDetailsTitleLabel()
        {
            var titleLabel = new Label();
            titleLabel.AddToClassList(k_DetailsTitleClassName);
            return titleLabel;
        }

        /// <summary>
        /// Creates some Text label for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a text in the details panel.</returns>
        protected static Label MakeDetailsTextLabel()
        {
            var textLabel = new Label();
            textLabel.AddToClassList(k_DetailsTextClassName);
            return textLabel;
        }

        public virtual void InitDetailsPanel(VisualElement detailsPanel)
        {
            m_DetailsTitleLabel = MakeDetailsTitleLabel();
            detailsPanel.Add(m_DetailsTitleLabel);

            m_DetailsTextLabel = MakeDetailsTextLabel();
            detailsPanel.Add(m_DetailsTextLabel);
        }

        public virtual void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            if (HasDetailsPanel)
            {
                UpdateDetailsPanel(items.FirstOrDefault());
            }
        }

        protected virtual void UpdateDetailsPanel(SearcherItem searcherItem)
        {
            if (m_DetailsTitleLabel != null)
                m_DetailsTitleLabel.text = searcherItem?.Name ?? "No Result";
            if (m_DetailsTextLabel != null)
                m_DetailsTextLabel.text = searcherItem?.Help ?? "";
        }
    }
}
