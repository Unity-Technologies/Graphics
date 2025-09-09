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

        public static void RunQueuedSearch(
            IndexingOptions neededOptions,
            List<(string query, string description)> contextSearchQueriesAndIds,
            Action<SearchItem, string> onAssetGUIDFound,
            Action onSearchsFinished)
        {
            bool needsDeepSearch = (neededOptions & IndexingOptions.DeepSearch) != 0;
            bool needsPackageIndexing = (neededOptions & IndexingOptions.PackageIndexing) != 0;

            bool isDeepSearchEnabled = Search.SearchService.IsDeepIndexingEnabled();
            bool isPackageIndexingEnabled = Search.SearchService.IsPackageIndexingEnabled();

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
                            () => onSearchsFinished?.Invoke());
                    }
                    else
                    {
                        // No need to rollback the index info, the initialization is done now
                        onSearchsFinished?.Invoke();
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
