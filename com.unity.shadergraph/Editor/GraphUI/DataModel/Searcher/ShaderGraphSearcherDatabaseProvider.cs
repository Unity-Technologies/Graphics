using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class ShaderGraphSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        public ShaderGraphSearcherDatabaseProvider(Stencil stencil)
            : base(stencil)
        {
        }

        SearcherItem CreateRegistryNodeSearcherItem(IGraphModel graphModel, PlaceholderRegistryKey key, string name)
        {
            return new GraphNodeModelSearcherItem(graphModel, null,
                graphNodeCreationData => graphNodeCreationData.CreateRegistryNode(key, name), name);
        }

        SearcherDatabaseBase CreateNodeDatabaseFromRegistry(IGraphModel graphModel)
        {
            // TODO: Get all keys and names from registry. Make categories by nesting SearcherItems.
            return new SearcherDatabase(new List<SearcherItem>
            {
                CreateRegistryNodeSearcherItem(graphModel, new PlaceholderRegistryKey(), "TODO")
            });
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
