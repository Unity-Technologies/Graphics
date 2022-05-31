using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        ShaderGraphStencil m_Stencil;

        public ShaderGraphSearcherDatabaseProvider(ShaderGraphStencil stencil)
            : base(stencil)
        {
            m_Stencil = stencil;
        }

        // TODO: (Sai) If we were to specify the category path directly as the path string i.e.
        // "Artistic/Color" instead of {"Artistic", "Color"} then we could avoid doing this
        static string GetCategoryPath(NodeUIDescriptor uiDescriptor)
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

        SearcherDatabaseBase CreateNodeDatabaseFromRegistry(IGraphModel graphModel)
        {
            var searcherItems = new List<SearcherItem>();
            if (graphModel is ShaderGraphModel shaderGraphModel &&
                shaderGraphModel.Stencil is ShaderGraphStencil shaderGraphStencil)
            {
                var registry = shaderGraphStencil.GetRegistry();
                foreach (var registryKey in registry.Registry.BrowseRegistryKeys())
                {
                    if (ShaderGraphModel.ShouldElementBeVisibleToSearcher(shaderGraphModel, registryKey))
                    {
                        var uiHints = m_Stencil.GetUIHints(registryKey, registry.GetDefaultTopology(registryKey));
                        var categoryPath = GetCategoryPath(uiHints);

                        // TODO: it's possible for searcher names to collides, which will prevent the searcher from functioning.
                        var searcherItem = new RegistryNodeSearcherItem(graphModel, registryKey, registryKey.Name);
                        searcherItem.CategoryPath = categoryPath;

                        searcherItem.Synonyms = uiHints.Synonyms.ToArray();
                        searcherItems.Add(searcherItem);
                    }
                }
            }
            return new SearcherDatabase(searcherItems);
        }

        public override IReadOnlyList<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            var databases = base.GetGraphElementsSearcherDatabases(graphModel).ToList();
            databases.Add(CreateNodeDatabaseFromRegistry(graphModel));
            return databases;
        }

        SearcherDatabaseBase CreateTypeDatabaseFromRegistry()
        {
            // TODO: Retrieve types from registry, map to type handles.
            return new SearcherDatabase(new List<SearcherItem>
            {
                new TypeSearcherItem("TODO", TypeHandle.Float)
            });
        }

        public override IReadOnlyList<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            var databases = base.GetVariableTypesSearcherDatabases().ToList();
            databases.Add(CreateTypeDatabaseFromRegistry());
            return databases;
        }
    }
}
