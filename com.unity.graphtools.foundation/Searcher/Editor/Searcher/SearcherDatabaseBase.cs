using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Searcher Database base class
    /// Provides ways to index, filter and search a collection of searcher items
    /// </summary>
    [PublicAPI, Serializable]
    public abstract class SearcherDatabaseBase
    {
        /// <summary>
        /// The filter currently being applied to the Database.
        /// </summary>
        public SearcherFilter CurrentFilter { get; private set; }

        /// <summary>
        /// Contains every item in the database after it has been indexed.
        /// </summary>
        public IReadOnlyList<SearcherItem> IndexedItems => m_IndexedItems;

        /// <summary>
        /// The filename to use to store serialized database json file
        /// </summary>
        protected const string k_SerializedJsonFile = "/SerializedDatabase.json";

        /// <summary>
        /// Whether or not to use parallel tasks to compute various operations such as indexing and filtering
        /// </summary>
        protected const bool k_UseParallelTasks = true;

        /// <summary>
        /// The Maximum number of filter results to cache.
        /// </summary>
        protected const int k_MaxNumFilterCache = 5;

        [SerializeField]
        List<SearcherItem> m_IndexedItems;

        [SerializeField]
        List<SearcherItem> m_FilteredItems;

        [SerializeField]
            (SearcherFilter filter, List<SearcherItem> filteredItems)[] m_FilterCache;
        [SerializeField]
        int m_OldestCacheIndex = 0;

        [SerializeField]
        int m_EstimateIndexSize;

        IEnumerable<SearcherItem> m_UnindexedItems;

        Task[] m_ParallelTasks = new Task[Environment.ProcessorCount];
        List<SearcherItem>[] m_ParallelLists = new List<SearcherItem>[Environment.ProcessorCount];

        /// <summary>
        /// Instantiates an empty database
        /// </summary>
        protected SearcherDatabaseBase()
            : this(new List<SearcherItem>())
        {
        }

        /// <summary>
        /// Instantiates a database with items that need to be indexed.
        /// </summary>
        /// <param name="unindexedItems">Items needing to be indexed</param>
        protected SearcherDatabaseBase(List<SearcherItem> unindexedItems)
            : this(unindexedItems, unindexedItems.Count)
        {
        }

        /// <summary>
        /// Instantiates a database with items that need to be indexed.
        /// </summary>
        /// <param name="unindexedItems">Items needing to be indexed.</param>
        /// <param name="estimateIndexSize">Estimate of the number of items, helps avoid reallocations.</param>
        protected SearcherDatabaseBase(IEnumerable<SearcherItem> unindexedItems, int estimateIndexSize)
        {
            m_UnindexedItems = unindexedItems;
            m_EstimateIndexSize = estimateIndexSize;
        }

        /// <summary>
        /// Sets the filter to be used for future search.
        /// </summary>
        /// <param name="filter">The filter to use.</param>
        public void SetCurrentFilter(SearcherFilter filter)
        {
            CurrentFilter = filter;
        }

        /// <summary>
        /// Searches the dabatase for matching items.
        /// </summary>
        /// <param name="query">Search query, e.g. keyword representing items to search for.</param>
        /// <param name="localMaxScore">Best matching score</param>
        /// <returns>A list of items matching the search query.</returns>
        [Obsolete("localMaxScore isn't used anymore, use Search(string query) instead")]
        public List<SearcherItem> Search(string query, out float localMaxScore)
        {
            localMaxScore = 0;
            return Search(query);
        }

        /// <summary>
        /// Searches the dabatase for matching items.
        /// </summary>
        /// <param name="query">Search query, e.g. keyword representing items to search for.</param>
        /// <returns>Items matching the search query as a list.</returns>
        public List<SearcherItem> Search(string query)
        {
            return SearchAsEnumerable(query).ToList();
        }

        /// <summary>
        /// Searches the dababase for matching items.
        /// </summary>
        /// <param name="query">Search query, e.g. keyword representing items to search for.</param>
        /// <returns>Items matching the search query.</returns>
        public IEnumerable<SearcherItem> SearchAsEnumerable(string query)
        {
            IndexIfNeeded();
            if (m_IndexedItems != null)
            {
                var filteredItems = FilterAndCacheItems(CurrentFilter, m_IndexedItems);
                return PerformSearch(query, filteredItems);
            }

            return Enumerable.Empty<SearcherItem>();
        }

        /// <summary>
        /// Indexes the database unless IndexedItems is not empty.
        /// Called by every call to `Search` but can be called manually ahead of time for convenience.
        /// </summary>
        public void IndexIfNeeded()
        {
            m_IndexedItems ??= PerformIndex(m_UnindexedItems);
        }

        /// <summary>
        /// Indexes database items. Get children items, get costly data if needed.
        /// Indexed items are stored in IndexItems.
        /// </summary>
        /// <param name="itemsToIndex">Items to index</param>
        /// <param name="estimateIndexSize">Estimate of the number of items, helps avoid reallocations.</param>
        /// <returns>A list of items that have been indexed</returns>
        public virtual List<SearcherItem> PerformIndex(IEnumerable<SearcherItem> itemsToIndex, int estimateIndexSize = -1)
        {
            if (estimateIndexSize < 0)
            {
                estimateIndexSize = itemsToIndex is IList<SearcherItem> list ? list.Count : 0;
            }
            var indexedItems = new List<SearcherItem>(estimateIndexSize);
            foreach (var item in itemsToIndex)
                AddItemToIndex(item, indexedItems);
            return indexedItems;
        }

        /// <summary>
        /// Applies a filter to a collection of items to only select some of them.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="items">The items to filter.</param>
        /// <returns>The list of items with the filter applied.</returns>
        public virtual List<SearcherItem> PerformFilter(SearcherFilter filter, IReadOnlyList<SearcherItem> items)
        {
            if (k_UseParallelTasks && items.Count > 100)
                return FilterMultiThreaded(filter, items);
            return FilterSingleThreaded(filter, items);
        }

        /// <summary>
        /// Calls PerformFilter and cache its result per filter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="items">The items to filter.</param>
        /// <returns>The list of items with the filter applied.</returns>
        List<SearcherItem> FilterAndCacheItems(SearcherFilter filter, IReadOnlyList<SearcherItem> items)
        {
            if (filter == null)
                return items.ToList();

            m_FilterCache = m_FilterCache ?? new(SearcherFilter filter, List<SearcherItem> filteredItems)[k_MaxNumFilterCache];
            var cachedItems = m_FilterCache.FirstOrDefault(tu => tu.filter == filter).filteredItems;

            if (cachedItems == null)
            {
                cachedItems = PerformFilter(filter, items);
                m_FilterCache[m_OldestCacheIndex] = (filter, cachedItems);
                m_OldestCacheIndex = (m_OldestCacheIndex + 1) % m_FilterCache.Length;
            }

            return cachedItems;
        }

        /// <summary>
        /// Performs a search on a collection of items that has already been indexed and filtered.
        /// </summary>
        /// <param name="query">Search query, e.g. keyword representing items to search for.</param>
        /// <param name="filteredItems">The list of indexed items to search in.</param>
        /// <returns>A list of items matching the search query.</returns>
        public abstract IEnumerable<SearcherItem> PerformSearch(string query, IReadOnlyList<SearcherItem> filteredItems);

        List<SearcherItem> FilterSingleThreaded(SearcherFilter filter, IReadOnlyList<SearcherItem> items)
        {
            var result = new List<SearcherItem>(items.Count);

            foreach (var searcherItem in items)
            {
                if (!filter.Match(searcherItem))
                    continue;

                result.Add(searcherItem);
            }

            return result;
        }

        List<SearcherItem> FilterMultiThreaded(SearcherFilter filter, IReadOnlyList<SearcherItem> items)
        {
            var result = new List<SearcherItem>();
            var tasks = m_ParallelTasks;
            var lists = m_ParallelLists;
            var count = tasks.Length;
            var itemsPerTask = (int)Math.Ceiling(items.Count / (float)count);

            for (var i = 0; i < count; i++)
            {
                var i1 = i;
                tasks[i] = Task.Run(() =>
                {
                    lists[i1] = new List<SearcherItem>();

                    for (var j = 0; j < itemsPerTask; j++)
                    {
                        var index = j + itemsPerTask * i1;
                        if (index >= items.Count)
                            break;

                        var item = items[index];
                        if (!filter.Match(item))
                            continue;

                        lists[i1].Add(item);
                    }
                });
            }

            Task.WaitAll(tasks);

            for (var i = 0; i < count; i++)
            {
                result.AddRange(lists[i]);
            }

            return result;
        }

        /// <summary>
        /// Internal helper to overwrite Id after deserializing
        /// </summary>
        /// <param name="newId"></param>
        internal void OverwriteId(int newId)
        {
            Id = newId;
        }

        internal int Id { get; private set; }

        /// <summary>
        /// Loads a database from a file.
        /// </summary>
        /// <param name="directory">Directory where the file is stored.</param>
        public void LoadFromFile(string directory)
        {
            var reader = new StreamReader(directory + k_SerializedJsonFile);
            var serializedData = reader.ReadToEnd();
            reader.Close();

            EditorJsonUtility.FromJsonOverwrite(serializedData, this);

            foreach (var item in m_IndexedItems)
            {
                item.OverwriteDatabase(this);
                item.ReInitAfterLoadFromFile();
            }
        }

        /// <summary>
        /// Saves a database to a file.
        /// </summary>
        /// <param name="databaseDirectory">Directory where the file should be stored.</param>
        public void SerializeToDirectory(string databaseDirectory)
        {
            if (databaseDirectory == null)
                return;
            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);
            IndexIfNeeded();
            var serializedData = EditorJsonUtility.ToJson(this, true);
            var writer = new StreamWriter(databaseDirectory + k_SerializedJsonFile, false);
            writer.Write(serializedData);
            writer.Close();
        }

        /// <summary>
        /// Index an item in the database.
        /// Includes computing data related to the item that might be expensive, and discovering children items.
        /// </summary>
        /// <param name="item">The item to index.</param>
        /// <param name="indexedItems">The list to add indexed item to.</param>
        protected void AddItemToIndex(SearcherItem item, List<SearcherItem> indexedItems)
        {
            item.OverwriteId(indexedItems.Count);
            item.Build();
            indexedItems.Add(item);

            // This is used for sorting results between databases.
            item.OverwriteDatabase(this);

            if (!item.HasChildren)
                return;

            var childrenIds = new List<int>();
            foreach (SearcherItem child in item.Children)
            {
                AddItemToIndex(child, indexedItems);
                childrenIds.Add(child.Id);
            }

            item.OverwriteChildrenIds(childrenIds);
        }
    }
}
