using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Contains databases and preferences used to perform a search.
    /// </summary>
    [PublicAPI]
    public class Searcher
    {
        /// <summary>
        /// Adapter used in this searcher to customize the searching interface.
        /// </summary>
        public ISearcherAdapter Adapter { get; }

        /// <summary>
        /// Sorting function used to organize items.
        /// </summary>
        public Comparison<SearcherItem> SortComparison { get; set; }

        /// <summary>
        /// The name under which the preference for the Preview visibility is stored in the user preferences.
        /// </summary>
        string PreviewTogglePrefName => "PreviewTogglePrefForTool" + (Adapter.SearcherName ?? "UnknownSearcher");

        /// <summary>
        /// Sets the visibility of the preview panel in the user preferences.
        /// </summary>
        /// <param name="visible">Whether or not the preview panel should be visible.</param>
        public void SetPreviewPanelVisibility(bool visible)
        {
            EditorPrefs.SetBool(PreviewTogglePrefName, visible);
        }

        /// <summary>
        /// Gets the visibility of the preview panel from the user preferences.
        /// </summary>
        /// <returns>True if the panel should be visible, false otherwise.</returns>
        public bool IsPreviewPanelVisible()
        {
            return Adapter.HasDetailsPanel && EditorPrefs.GetBool(PreviewTogglePrefName, true);
        }

        readonly List<SearcherDatabaseBase> m_Databases;

        public Searcher(SearcherDatabaseBase database, string title, string searcherName = null)
            : this(new List<SearcherDatabaseBase> { database }, title, searcherName: searcherName)
        {}

        public Searcher(IEnumerable<SearcherDatabaseBase> databases, string title, SearcherFilter filter = null, string searcherName = null)
            : this(databases, title, new SearcherAdapter(title, searcherName), filter)
        {}

        public Searcher(SearcherDatabaseBase database, ISearcherAdapter adapter = null, SearcherFilter filter = null)
            : this(new List<SearcherDatabaseBase> { database }, adapter, filter)
        {}

        public Searcher(IEnumerable<SearcherDatabaseBase> databases, ISearcherAdapter adapter = null, SearcherFilter filter = null)
            : this(databases, string.Empty, adapter, filter)
        {}

        Searcher(IEnumerable<SearcherDatabaseBase> databases, string title, ISearcherAdapter adapter, SearcherFilter filter)
        {
            m_Databases = new List<SearcherDatabaseBase>();
            var databaseId = 0;
            foreach (var database in databases)
            {
                // This is needed for sorting items between databases.
                database.OverwriteId(databaseId);
                databaseId++;
                database.SetCurrentFilter(filter);

                m_Databases.Add(database);
            }

            Adapter = adapter ?? new SearcherAdapter(title);
        }

        public IEnumerable<SearcherItem> Search(string query)
        {
            query = query.ToLower();

            var results = new List<SearcherItem>();
            float maxScore = 0;
            foreach (var database in m_Databases)
            {
                var localResults = database.Search(query);
                var localMaxScore = localResults.Any() ? localResults.Max(i => i.lastSearchScore) : 0;
                if (localMaxScore > maxScore)
                {
                    // skip the highest scored item in the local results and
                    // insert it back as the first item. The first item should always be
                    // the highest scored item. The order of the other items does not matter
                    // because they will be reordered to recreate the tree properly.
                    if (results.Count > 0)
                    {
                        // backup previous best result
                        results.Add(results[0]);
                        // replace it with the new best result
                        results[0] = localResults[0];
                        // add remaining results at the end
                        results.AddRange(localResults.Skip(1));
                    }
                    else // best result will be the first item
                        results.AddRange(localResults);

                    maxScore = localMaxScore;
                }
                else // no new best result just append everything
                {
                    results.AddRange(localResults);
                }
            }

            return results;
        }

        [PublicAPI]
        public class AnalyticsEvent
        {
            [PublicAPI]
            public enum EventType { Pending, Picked, Cancelled }
            public readonly EventType eventType;
            public readonly string currentSearchFieldText;
            public AnalyticsEvent(EventType eventType, string currentSearchFieldText)
            {
                this.eventType = eventType;
                this.currentSearchFieldText = currentSearchFieldText;
            }
        }
    }
}
