using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class ShaderGraphSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        public ShaderGraphSearcherDatabaseProvider(Stencil stencil)
            : base(stencil)
        {
        }

        SearcherDatabaseBase CreateNodeDatabaseFromRegistry(IGraphModel graphModel)
        {
            // TODO: Get all keys and names from registry. Make categories by nesting SearcherItems.
            return new SearcherDatabase(new List<SearcherItem>
            {
                new RegistryNodeSearcherItem(graphModel, new PlaceholderRegistryKey(), "TODO")
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
