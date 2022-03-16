using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Provides ways to customize the searching interface.
    /// </summary>
    [PublicAPI]
    public interface ISearcherAdapter
    {
        /// <summary>
        /// Name to display when creating a searcher.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Unique human-readable name for the searcher created by this adapter.
        /// </summary>
        /// <remarks>Used to separate preferences between Searchers.</remarks>
        string SearcherName { get; }

        /// <summary>
        /// If <c>true</c>, the Searcher will have a toggleable Details panel.
        /// </summary>
        bool HasDetailsPanel { get; }

        /// <summary>
        /// If <c>true</c>, enables support for multi-selection in the searcher.
        /// </summary>
        bool MultiSelectEnabled { get; }

        /// <summary>
        /// Initial width ratio to use when splitting the main view and the details view.
        /// </summary>
        float InitialSplitterDetailRatio { get; }

        /// <summary>
        /// Associates style names to category paths.
        /// </summary>
        /// <remarks>Allows UI to apply custom styles to certain categories.</remarks>
        IReadOnlyDictionary<string, string> CategoryPathStyleNames { get; }

        /// <summary>
        /// Extra stylesheet(s) to load when displaying the searcher.
        /// </summary>
        /// <remarks>(FILENAME)_dark.uss and (FILENAME)_light.uss will be loaded as well if existing.</remarks>
        string CustomStyleSheetPath { get; }

        /// <summary>
        /// Callback to use when an item is selected but not yet validated in the searcher.
        /// </summary>
        /// <param name="items">List of items being selected. Can be empty.</param>
        void OnSelectionChanged(IEnumerable<SearcherItem> items);

        /// <summary>
        /// Called when the details panel will be redrawn for a <see cref="SearcherItem"/>.
        /// </summary>
        /// <param name="searcherItem">The item that will be displayed in the details view.</param>
        void UpdateDetailsPanel(SearcherItem searcherItem);

        /// <summary>
        /// Called once when the details panel gets initialized during the searcher view creation (<see cref="SearcherControl"/>).
        /// </summary>
        /// <param name="detailsPanel">The <see cref="VisualElement"/> used to display the details panel.</param>
        void InitDetailsPanel(VisualElement detailsPanel);
    }
}
