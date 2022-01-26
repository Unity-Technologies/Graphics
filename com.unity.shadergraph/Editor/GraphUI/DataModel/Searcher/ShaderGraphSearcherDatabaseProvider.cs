using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class ShaderGraphSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        ShaderGraphStencil m_Stencil;

        public ShaderGraphSearcherDatabaseProvider(ShaderGraphStencil stencil)
            : base(stencil)
        {
            m_Stencil = stencil;
        }

        SearcherDatabaseBase CreateNodeDatabaseFromRegistry(IGraphModel graphModel)
        {
            // TODO: Handle categories, possible caching

            var searcherItems = new List<SearcherItem>();
            if (graphModel is ShaderGraphModel shaderGraphModel &&
                shaderGraphModel.Stencil is ShaderGraphStencil shaderGraphStencil)
            {
                var registry = shaderGraphStencil.GetRegistry();
                foreach (var registryKey in registry.BrowseRegistryKeys())
                {
                    if(ShaderGraphModel.ShouldElementBeVisibleToSearcher(shaderGraphModel, registryKey))
                        searcherItems.Add(new RegistryNodeSearcherItem(graphModel, registryKey, registryKey.Name));
                }
            }
            return new SearcherDatabase(searcherItems);
        }

        public override List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            var databases = base.GetGraphElementsSearcherDatabases(graphModel);
            databases.Add(CreateNodeDatabaseFromRegistry(graphModel));
            return databases;
        }

        SearcherDatabaseBase CreateTypeDatabaseFromRegistry()
        {
            // TODO: Retrieve types from registry, map to type handles.
            return new SearcherDatabase(new List<SearcherItem>
            {
                new TypeSearcherItem(TypeHandle.Float, "TODO")
            });
        }

        public override List<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            var databases = base.GetVariableTypesSearcherDatabases();
            databases.Add(CreateTypeDatabaseFromRegistry());
            return databases;
        }
    }
}
