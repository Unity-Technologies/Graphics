using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    class ImportedGraphSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        /// <inheritdoc />
        public ImportedGraphSearcherDatabaseProvider(Stencil stencil)
            : base(stencil) { }

        /// <inheritdoc />
        public override IReadOnlyList<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            var dbs = base.GetGraphElementsSearcherDatabases(graphModel);
            return AddAssetGraphSubgraphs(dbs);
        }

        public List<SearcherDatabaseBase> AddAssetGraphSubgraphs(IReadOnlyList<SearcherDatabaseBase> dbs)
        {
            var originalDBs = dbs.ToList();
            var graphAssetType = Stencil.GraphModel.Asset.GetType();
            var assetPaths = AssetDatabase.FindAssets($"t:{graphAssetType}").Select(AssetDatabase.GUIDToAssetPath).ToList();
            var graphModels = assetPaths
                .Select(p => (AssetDatabase.LoadAssetAtPath(p, graphAssetType) as GraphAsset)?.GraphModel)
                .Where(g => g != null && !g.IsContainerGraph() && g.CanBeSubgraph());

            var handle = Stencil.GetSubgraphNodeTypeHandle();

            var items = graphModels.Select(graphModel =>
                new GraphNodeModelSearcherItem(graphModel.Name ?? "UnknownAssetGraphModel",
                new TypeSearcherItemData(handle),
                data => data.CreateSubgraphNode(graphModel))
            {
                CategoryPath = GraphElementSearcherDatabase.subgraphs
            });

            originalDBs.Add(new SearcherDatabase(items.ToList()));
            return originalDBs;
        }
    }
}
