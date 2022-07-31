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
            databases.Add(CreateNodeDatabaseFromRegistry(graphModel));
            return databases;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            var databases = base.GetVariableTypesSearcherDatabases().ToList();
            databases.Add(CreateTypeDatabaseFromRegistry());
            return databases;
        }

        // TODO: (Sai) If we were to specify the category path directly as the path string i.e.
        // "Artistic/Color" instead of {"Artistic", "Color"} then we could avoid doing this
        private static string GetCategoryPath(NodeUIDescriptor uiDescriptor)
        {
            var categoryPath = String.Empty;
            var categoryData = uiDescriptor.Categories.ToList();
            for (var i = 0; i < categoryData.Count; ++i)
            {
                var pathPiece = categoryData[i];
                categoryPath += pathPiece;
                if (i != categoryData.Count - 1)
                    categoryPath += "/";
            }

            return categoryPath;
        }


        private SearcherDatabaseBase CreateNodeDatabaseFromRegistry(IGraphModel graphModel)
        {
            var searcherItems = new List<SearcherItem>();
            if (graphModel is ShaderGraphModel shaderGraphModel &&
                shaderGraphModel.Stencil is ShaderGraphStencil shaderGraphStencil)
            {
                // Keep track of all the names that have been added to the SearcherItem list.
                // Having conflicting names in the SearcherItem list causes errors
                // in the SearcherDatabase initialization.
                HashSet<string> namesAddedToSearcher = new();
                var registry = shaderGraphStencil.GetRegistry();
                foreach (var registryKey in registry.Registry.BrowseRegistryKeys())
                {
                    if (shaderGraphModel.ShouldBeInSearcher(registryKey))
                    {
                        var uiHints = shaderGraphStencil.GetUIHints(
                            registryKey,
                            registry.GetDefaultTopology(registryKey)
                        );
                        string searcherItemName = uiHints.DisplayName;
                        // fallback to the registry name if there is no display name
                        if (string.IsNullOrEmpty(searcherItemName))
                            searcherItemName = registryKey.Name;
                        if (namesAddedToSearcher.Contains(searcherItemName))
                        {
                            // If there is already a SearcherItem with the current
                            // name, warn and skip.
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
                            CategoryPath = GetCategoryPath(uiHints),
                            Synonyms = uiHints.Synonyms.ToArray()
                        };
                        namesAddedToSearcher.Add(searcherItemName);
                        searcherItems.Add(searcherItem);
                    }
                }
            }
            return new SearcherDatabase(searcherItems);
        }

        private SearcherDatabaseBase CreateTypeDatabaseFromRegistry()
        {
            // TODO: Retrieve types from registry, map to type handles.
            return new SearcherDatabase(new List<SearcherItem>
            {
                new TypeSearcherItem("TODO", TypeHandle.Float)
            });
        }
    }
}
