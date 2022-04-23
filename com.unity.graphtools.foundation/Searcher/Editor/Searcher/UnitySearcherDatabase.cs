#if QUICKSEARCH_3_0_0_OR_NEWER || USE_SEARCH_MODULE || USE_QUICK_SEARCH_MODULE

using System;
using System.Collections.Generic;
using UnityEditor.Search;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Searcher Database using Unity Search as a backend
    /// </summary>
    public class SearcherDatabase : SearcherDatabaseBase
    {
        /// <summary>
        /// Used in tests because the older database doesn't behave quite the same
        /// </summary>
        internal static bool IsOldDatabaseWithoutQuickSearch => false;

        List<int> m_MatchIndicesBuffer;

        /// <summary>
        /// During one search query, stores the item being processed currently.
        /// </summary>
        SearcherItem m_CurrentItem;

        float m_ScoreMultiplier = 1f;
        QueryEngine<SearcherItem> m_QueryEngine;

        /// <summary>
        /// Instantiates an empty database.
        /// </summary>
        public SearcherDatabase()
            : this(new List<SearcherItem>())
        {
        }

        /// <summary>
        /// Instantiates a database with items.
        /// </summary>
        /// <param name="items">Items to populate the database.</param>
        public SearcherDatabase(IReadOnlyList<SearcherItem> items)
            : base(items)
        {
            SetupQueryEngine();
        }

        /// <summary>
        /// Creates a database from a serialized file.
        /// </summary>
        /// <param name="directory">Path of the directory where the database is stored.</param>
        /// <returns>A Database with items retrieved from the serialized file.</returns>
        public static SearcherDatabase FromFile(string directory)
        {
            var db = new SearcherDatabase();
            db.LoadFromFile(directory);
            return db;
        }

        /// <inheritdoc/>
        public override IEnumerable<SearcherItem> PerformSearch(string query,
            IReadOnlyList<SearcherItem> filteredItems)
        {
            var searchQuery = m_QueryEngine.ParseQuery("\"" + query + "\""); // TODO add support for "doc:" filter?
            m_CurrentItem = null;
            var searchResults = searchQuery.Apply(filteredItems);
            return searchResults;
        }

        void SetupQueryEngine()
        {
            m_QueryEngine = new QueryEngine<SearcherItem>();
            m_QueryEngine.SetSearchDataCallback(GetSearchData, s => s.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
            m_QueryEngine.SetSearchWordMatcher((searchWord, _, __, searchData) =>
            {
                if (m_MatchIndicesBuffer == null)
                    m_MatchIndicesBuffer = new List<int>(searchData.Length);
                else
                    m_MatchIndicesBuffer.Clear();
                long score = 0;
                var fuzzyMatch = FuzzySearch.FuzzyMatch(searchWord, searchData, ref score, m_MatchIndicesBuffer);
                if (m_CurrentItem != null)
                {
                    LastSearchData[m_CurrentItem] = new SearchData {
                        MatchedIndices = m_MatchIndicesBuffer,
                        MatchedString = searchData,
                        Score = (long)(score * m_ScoreMultiplier) };
                }
                return fuzzyMatch;
            });
        }

        IEnumerable<string> GetSearchData(SearcherItem arg)
        {
            if (arg == null)
                yield break;
            m_CurrentItem = arg;
            foreach (var keysRatio in arg.SearchKeys)
            {
                var items = keysRatio.searchData;
                m_ScoreMultiplier = keysRatio.ratio;
                foreach (var item in items)
                    yield return item;
            }
        }
    }
}
#endif
