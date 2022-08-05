using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Provides the data required by the searcher (SearcherDatabase with SearcherItems).
    /// Makes all nodes returned from stencil.GetRegistry() availabe in the searcher.
    /// </summary>
    public class ShaderGraphSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        public ShaderGraphSearcherDatabaseProvider(ShaderGraphStencil stencil)
            : base(stencil)
        {
        }

        /// <inheritdoc/>
        public override IReadOnlyList<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            var databases = base.GetGraphElementsSearcherDatabases(graphModel).ToList();
            List<SearcherItem> searcherItems = GetNodeSearcherItems(graphModel);
            SearcherDatabase db = new(searcherItems);
            databases.Add(db);
            return databases;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            var databases = base.GetVariableTypesSearcherDatabases().ToList();
            List<SearcherItem> searcherItems = GetTypeSearcherItems();
            SearcherDatabase db = new(searcherItems);
            databases.Add(db);
            return databases;
        }

        internal static List<SearcherItem> GetNodeSearcherItems(IGraphModel graphModel)
        {
            var searcherItems = new List<SearcherItem>();
            if (graphModel is ShaderGraphModel shaderGraphModel)
            {
                // Keep track of all the names that have been added to the SearcherItem list.
                // Having conflicting names in the SearcherItem list causes errors
                // in the SearcherDatabase initialization.
                HashSet<string> namesAddedToSearcher = new();
                var registry = shaderGraphModel.RegistryInstance;
                foreach (var registryKey in registry.Registry.BrowseRegistryKeys())
                {
                    if (shaderGraphModel.ShouldBeInSearcher(registryKey))
                    {
                        // Should be part of the registry contract that if a key is registered,
                        // a valid topology and nodeUIInfo for it will also exist.
                        var uiInfo = registry.GetNodeUIDescriptor(registryKey);
                        string searcherItemName = uiInfo.DisplayName;
                        // If there is already a SearcherItem with the current name,
                        // warn and skip.
                        if (namesAddedToSearcher.Contains(searcherItemName))
                        {
                            Debug.LogWarning($"Not adding \"{searcherItemName}\" to the searcher. A searcher item with this name already exists.");
                            continue;
                        }
                        SearcherItem searcherItem = new GraphNodeModelSearcherItem(
                            name: searcherItemName,
                            null,
                            creationData => graphModel.CreateGraphDataNode(
                                registryKey,
                                searcherItemName,
                                creationData.Position,
                                creationData.Guid,
                                creationData.SpawnFlags
                            )
                        )
                        {
                            CategoryPath = uiInfo.Category,
                            Synonyms = uiInfo.Synonyms.ToArray()
                        };
                        namesAddedToSearcher.Add(searcherItemName);
                        searcherItems.Add(searcherItem);
                    }
                }
            }
            return searcherItems;
        }

        internal static List<SearcherItem> GetTypeSearcherItems()
        {
            // TODO: Retrieve types from registry, map to type handles.
            return  new List<SearcherItem>
            {
                new TypeSearcherItem("TODO", TypeHandle.Float)
            };
        }
    }
}
