using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Default implementation for <see cref="ISearcherAdapter"/>.
    /// </summary>
    [PublicAPI]
    public class SearcherAdapter : ISearcherAdapter
    {
        public static string DefaultToolName => "UnknownSearcherTool";

        protected const string k_DetailsTitleClassName = "unity-label__searcher-details-title";
        protected const string k_DetailsSubTitleClassName = "unity-label__searcher-details-subtitle";
        protected const string k_DetailsTextClassName = "unity-label__searcher-details-text";

        /// <inheritdoc />
        public virtual string Title { get; }

        /// <inheritdoc />
        public virtual string SearcherName { get; }

        /// <inheritdoc />
        public virtual bool HasDetailsPanel => true;

        /// <inheritdoc />
        public virtual bool MultiSelectEnabled => false;

        /// <inheritdoc />
        public virtual float InitialSplitterDetailRatio => 1.0f;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> CategoryPathStyleNames { get; set; }

        /// <inheritdoc />
        public string CustomStyleSheetPath { get; set; }

        protected Label m_DetailsTitleLabel;

        protected Label m_DetailsTextLabel;

        public SearcherAdapter(string title, string toolName = null)
        {
            Title = title;
            SearcherName = string.IsNullOrEmpty(toolName)? DefaultToolName : toolName;
        }

        /// <summary>
        /// Creates a Title for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a title in the details panel.</returns>
        protected static Label MakeDetailsTitleLabel(string text = null)
        {
            var titleLabel = new Label(text);
            titleLabel.AddToClassList(k_DetailsTitleClassName);
            return titleLabel;
        }

        /// <summary>
        /// Creates a sub-title for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a sub-title in the details panel.</returns>
        protected static Label MakeDetailsSubTitleLabel(string text = null)
        {
            var titleLabel = new Label(text);
            titleLabel.AddToClassList(k_DetailsSubTitleClassName);
            return titleLabel;
        }

        /// <summary>
        /// Creates some Text label for the Details section
        /// </summary>
        /// <returns>A <see cref="Label"/> with uss class for a text in the details panel.</returns>
        protected static Label MakeDetailsTextLabel(string text = null)
        {
            var textLabel = new Label(text);
            textLabel.AddToClassList(k_DetailsTextClassName);
            return textLabel;
        }

        /// <inheritdoc />
        public virtual void InitDetailsPanel(VisualElement detailsPanel)
        {
            m_DetailsTitleLabel = MakeDetailsTitleLabel();
            detailsPanel.Add(m_DetailsTitleLabel);

            m_DetailsTextLabel = MakeDetailsTextLabel();
            if (m_DetailsTextLabel != null)
            {
#if UNITY_2021_2_OR_NEWER
                m_DetailsTextLabel.enableRichText = true;
#endif
                detailsPanel.Add(m_DetailsTextLabel);
            }
        }

        /// <inheritdoc />
        public virtual void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
        }

        /// <inheritdoc />
        public virtual void UpdateDetailsPanel(SearcherItem searcherItem)
        {
            if (m_DetailsTitleLabel != null)
                m_DetailsTitleLabel.text = searcherItem?.Name ?? "";
            if (m_DetailsTextLabel != null)
                m_DetailsTextLabel.text = searcherItem?.Help ?? "";
        }
    }
}
