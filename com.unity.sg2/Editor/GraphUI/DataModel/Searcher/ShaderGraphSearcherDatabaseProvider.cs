using System;
using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using Unity.ItemLibrary.Editor;
using Unity.GraphToolsFoundation;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Provides the data required by the searcher (ItemLibraryDatabase with SearcherItems).
    /// Makes all nodes returned from stencil.GetRegistry() availabe in the searcher.
    /// </summary>
    class ShaderGraphSearcherDatabaseProvider : DefaultDatabaseProvider
    {
        public ShaderGraphSearcherDatabaseProvider(ShaderGraphStencil stencil)
            : base(stencil)
        {
        }

        /// <inheritdoc/>
        public override IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementsDatabases(GraphModel graphModel)
        {
            var databases = base.GetGraphElementsDatabases(graphModel).ToList();
            List<ItemLibraryItem> searcherItems = GetNodeSearcherItems(graphModel);
            ItemLibraryDatabase db = new(searcherItems);
            databases.Add(db);
            return databases;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<ItemLibraryDatabaseBase> GetVariableTypesDatabases()
        {
            var databases = base.GetVariableTypesDatabases().ToList();
            List<ItemLibraryItem> searcherItems = GetTypeSearcherItems();
            ItemLibraryDatabase db = new(searcherItems);
            databases.Add(db);
            return databases;
        }

        internal static List<ItemLibraryItem> GetNodeSearcherItems(GraphModel graphModel)
        {
            var searcherItems = new List<ItemLibraryItem>();
            if (graphModel is ShaderGraphModel shaderGraphModel)
            {
                // Keep track of all the names that have been added to the ItemLibraryItem list.
                // Having conflicting names in the ItemLibraryItem list causes errors
                // in the ItemLibraryDatabase initialization.
                HashSet<string> namesAddedToSearcher = new();
                var registry = shaderGraphModel.RegistryInstance;

                var versionCounts = new Dictionary<string, int>();
                foreach (var registryKey in registry.Registry.BrowseRegistryKeys())
                {
                    versionCounts[registryKey.Name] = versionCounts.GetValueOrDefault(registryKey.Name, 0) + 1;
                }

                foreach (var registryKey in registry.Registry.BrowseRegistryKeys())
                {
                    if (shaderGraphModel.ShouldBeInSearcher(registryKey))
                    {
                        // Should be part of the registry contract that if a key is registered,
                        // a valid topology and nodeUIInfo for it will also exist.
                        var uiInfo = registry.GetNodeUIDescriptor(registryKey);
                        string searcherItemName = uiInfo.DisplayName;
                        // If a registry key has more than 1 available version, show version numbers explicitly in the
                        // searcher to prevent conflicts.
                        // TODO: This isn't the final design for presenting nodes with multiple versions.
                        if (versionCounts[registryKey.Name] > 1)
                        {
                            searcherItemName += $" (v{uiInfo.Version})";
                        }
                        // If there is already a ItemLibraryItem with the current name,
                        // warn and skip.
                        if (namesAddedToSearcher.Contains(searcherItemName))
                        {
                            Debug.LogWarning($"Not adding \"{searcherItemName}\" to the searcher. A searcher item with this name already exists.");
                            continue;
                        }
                        ItemLibraryItem searcherItem = new GraphNodeModelLibraryItem(
                            name: searcherItemName,
                            null,
                            creationData => graphModel.CreateGraphDataNode(
                                registryKey,
                                uiInfo.DisplayName,
                                creationData.Position,
                                creationData.Guid,
                                creationData.SpawnFlags
                            )
                        )
                        {
                            CategoryPath = uiInfo.Category,
                            Synonyms = uiInfo.Synonyms.ToArray(),
                            Help = uiInfo.Description,
                        };
                        namesAddedToSearcher.Add(searcherItemName);
                        searcherItems.Add(searcherItem);
                    }
                }
            }
            return searcherItems;
        }

        internal static List<ItemLibraryItem> GetTypeSearcherItems()
        {
            // TODO: Retrieve types from registry, map to type handles.
            return  new List<ItemLibraryItem>
            {
                new TypeLibraryItem("TODO", TypeHandle.Float)
            };
        }
    }
}
