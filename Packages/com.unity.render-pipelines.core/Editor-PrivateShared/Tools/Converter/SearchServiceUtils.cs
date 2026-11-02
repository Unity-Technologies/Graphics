using System.Collections.Generic;
using System;
using UnityEditor.Search;

namespace UnityEditor.Rendering
{
    static class SearchServiceUtils
    {
        [Flags]
        public enum IndexingOptions
        {
            None = 0,
            DeepSearch = 1 << 0,
            PackageIndexing = 1 << 1
        }

        // Set while a caller blocks the Editor, so searches have to finish before RunQueuedSearch returns
        public static bool synchronous { get; set; }

        public static void RunQueuedSearch(
            IndexingOptions neededOptions,
            List<(string query, string description)> contextSearchQueriesAndIds,
            Action<SearchItem, string> onAssetGUIDFound,
            Action onSearchesFinished)
        {
            if (contextSearchQueriesAndIds == null || contextSearchQueriesAndIds.Count == 0)
            {
                onSearchesFinished?.Invoke();
                return;
            }

            bool needsDeepSearch = (neededOptions & IndexingOptions.DeepSearch) != 0;
            bool needsPackageIndexing = (neededOptions & IndexingOptions.PackageIndexing) != 0;

            bool isDeepSearchEnabled = Search.SearchService.IsDeepIndexingEnabled();
            bool isPackageIndexingEnabled = Search.SearchService.IsPackageIndexingEnabled();

            if (synchronous)
            {
                // Changing the indexing settings rebuilds the index over several Editor updates, which a blocked caller can't wait for
                if (isDeepSearchEnabled != needsDeepSearch || isPackageIndexingEnabled != needsPackageIndexing)
                {
                    throw new InvalidOperationException(
                        "A synchronous scan can't change the Search indexing settings. In Preferences > Search > Indexing, " +
                        $"set 'Deep scene and prefab indexing' to {(needsDeepSearch ? "enabled" : "disabled")} and 'Packages indexing' to {(needsPackageIndexing ? "enabled" : "disabled")}, then scan again.");
                }

                foreach (var (query, id) in contextSearchQueriesAndIds)
                {
                    using (var context = Search.SearchService.CreateContext(query))
                    {
                        Search.SearchService.Request(context, (searchContext, searchItems) =>
                        {
                            foreach (var item in searchItems)
                            {
                                try
                                {
                                    onAssetGUIDFound.Invoke(item, id);
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogException(e);
                                }
                            }
                        }, Search.SearchFlags.Synchronous);
                    }
                }

                onSearchesFinished?.Invoke();
                return;
            }

            int index = 0;
            void ProcessNextSearch()
            {
                if (index >= contextSearchQueriesAndIds.Count)
                {
                    if (isDeepSearchEnabled != Search.SearchService.IsDeepIndexingEnabled() ||
                        isPackageIndexingEnabled != Search.SearchService.IsPackageIndexingEnabled())
                    {
                        // Rollback the index settings, and call the callback to notify that the initialization is done
                        Search.SearchService.ChangeIndexingSettings(
                            deepIndexing: isDeepSearchEnabled,
                            packageIndexing: isPackageIndexingEnabled,
                            () => onSearchesFinished?.Invoke());
                    }
                    else
                    {
                        // No need to rollback the index info, the initialization is done now
                        onSearchesFinished?.Invoke();
                    }
                    return;
                }
                
                var id = contextSearchQueriesAndIds[index].description;
                var query = contextSearchQueriesAndIds[index].query;
                var context = Search.SearchService.CreateContext(query);

                Search.SearchService.Request(context, (searchContext, searchItems) =>
                {
                    foreach (var item in searchItems)
                    {
                        onAssetGUIDFound.Invoke(item, id);
                    }

                    searchContext?.Dispose();
                    index++;
                    ProcessNextSearch();
                });
            }

            void OnSearchIndexReady()
            {
                ProcessNextSearch();
            }

            if (isDeepSearchEnabled != needsDeepSearch || isPackageIndexingEnabled != needsPackageIndexing)
            {
                Search.SearchService.ChangeIndexingSettings(
                    deepIndexing: needsDeepSearch,
                    packageIndexing: needsPackageIndexing,
                    OnSearchIndexReady
                );
            }
            else
            {
                OnSearchIndexReady();
            }
        }
    }
}
