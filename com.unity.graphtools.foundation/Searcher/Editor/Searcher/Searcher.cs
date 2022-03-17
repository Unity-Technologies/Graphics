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
        public static string DefaultContext => "UnknownSearcherContext";

        /// <summary>
        /// Adapter used in this searcher to customize the searching interface.
        /// </summary>
        public ISearcherAdapter Adapter { get; }

        /// <summary>
        /// Sorting function used to organize items.
        /// </summary>
        public Comparison<SearcherItem> SortComparison { get; set; }

        /// <summary>
        /// Associates style names to category paths.
        /// </summary>
        /// <remarks>Allows UI to apply custom styles to certain categories.</remarks>
        public IReadOnlyDictionary<string, string> CategoryPathStyleNames => Adapter?.CategoryPathStyleNames;

        /// <summary>
        /// Sets the visibility of the preview panel in the user preferences.
        /// </summary>
        /// <param name="visible">Whether or not the preview panel should be visible.</param>
        public void SetPreviewPanelVisibility(bool visible)
        {
            Preferences.PreviewVisibility = visible;
        }

        /// <summary>
        /// Gets the visibility of the preview panel from the user preferences.
        /// </summary>
        /// <returns>True if the panel should be visible, false otherwise.</returns>
        public bool IsPreviewPanelVisible()
        {
            return Adapter.HasDetailsPanel && Preferences.PreviewVisibility;
        }

        readonly List<SearcherDatabaseBase> m_Databases;

        public Searcher(SearcherDatabaseBase database, string title, string searcherName = null, string context = null)
            : this(new List<SearcherDatabaseBase> { database }, title, searcherName: searcherName, context: context)
        {}

        public Searcher(IEnumerable<SearcherDatabaseBase> databases, string title, SearcherFilter filter = null, string searcherName = null, string context = null)
            : this(databases, title, new SearcherAdapter(title, searcherName), filter, context)
        {}

        public Searcher(SearcherDatabaseBase database, ISearcherAdapter adapter = null, SearcherFilter filter = null, string context = null)
            : this(new List<SearcherDatabaseBase> { database }, adapter, filter, context)
        {}

        public Searcher(IEnumerable<SearcherDatabaseBase> databases, ISearcherAdapter adapter = null, SearcherFilter filter = null, string context = null)
            : this(databases, null, adapter, filter, context)
        {}

        internal SearcherPreferences Preferences { get; }

        Dictionary<string, SearcherItem> m_ItemsByPath;
        List<SearcherItem> m_CachedFavorites;
        List<SearcherItem> m_CurrentFavorites;

        public IReadOnlyList<SearcherItem> CurrentFavorites => m_CachedFavorites;

        public bool IsFavorite(SearcherItem item)
        {
            return Preferences.GetFavorites().Contains(item.FullName);
        }

        public void SetFavorite(SearcherItem item, bool setFavorite = true)
        {
            Preferences.SetFavorite(item.FullName, setFavorite);
            if (setFavorite)
                m_CachedFavorites.Add(item);
            else
                m_CachedFavorites.Remove(item);
        }

        public void ClearFavorites()
        {
            Preferences.ClearFavorites();
            m_CachedFavorites.Clear();
        }

        Searcher(IEnumerable<SearcherDatabaseBase> databases, string title, ISearcherAdapter adapter, SearcherFilter filter, string context = null)
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
            Preferences = new SearcherPreferences(Adapter.SearcherName, string.IsNullOrEmpty(context) ? DefaultContext : context);
        }

        Dictionary<SearcherItem, SearcherDatabaseBase.SearchData> m_LastSearchDataPerItem = new Dictionary<SearcherItem, SearcherDatabaseBase.SearchData>();
        long m_LastMaxScore;
        SearcherItem m_LastMaxItem;

        SearcherItem GetMaxInLastSearch(IReadOnlyList<SearcherItem> searchResults)
        {
            long maxScore = long.MinValue;
            SearcherItem result = null;
            foreach (var item in searchResults)
            {
                if (m_LastSearchDataPerItem.TryGetValue(item, out var data) && data.Score > maxScore)
                {
                    maxScore = data.Score;
                    result = item;
                }
            }

            return result;
        }

        public IEnumerable<SearcherItem> Search(string query)
        {
            var results = new List<SearcherItem>();
            m_LastSearchDataPerItem.Clear();

            query = query.ToLower();
            m_LastMaxScore = long.MinValue;
            foreach (var database in m_Databases)
            {
                var localResults = database.Search(query);
                foreach (var kv in database.LastSearchData)
                {
                    m_LastSearchDataPerItem.Add(kv.Key, kv.Value);
                }

                var bestInDb = GetMaxInLastSearch(localResults);
                var localMaxScore = bestInDb != null ? m_LastSearchDataPerItem[bestInDb].Score : long.MinValue;
                if (localMaxScore > m_LastMaxScore)
                {
                    m_LastMaxItem = bestInDb;
                    m_LastMaxScore = localMaxScore;
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
                }
                else // no new best result just append everything
                {
                    results.AddRange(localResults);
                }
            }

            if (string.IsNullOrEmpty(query))
            {
                var comparison = SortComparison ?? ((i1, i2) => i1.Priority - i2.Priority);
                results.Sort(comparison);
            }
            else
            {
                int Comparison(SearcherItem a, SearcherItem b) => (int)(m_LastSearchDataPerItem[b].Score - m_LastSearchDataPerItem[a].Score);
                results.Sort(Comparison);
            }

            if (m_ItemsByPath == null)
            {
                m_ItemsByPath = new Dictionary<string, SearcherItem>();
                var allItems = string.IsNullOrEmpty(query) ? results : Search("");
                m_ItemsByPath = allItems.ToDictionary(r => r.FullName, r => r);

                m_CachedFavorites = Preferences.GetFavorites()
                    .Select(path => m_ItemsByPath.TryGetValue(path, out var item) ? item : null)
                    .Where(i => i != null)
                    .ToList();
            }

            if (m_CachedFavorites != null)
                m_CurrentFavorites = results.Where(item => m_CachedFavorites.Contains(item)).ToList();

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
