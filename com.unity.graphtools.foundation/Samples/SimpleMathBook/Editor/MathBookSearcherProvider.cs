using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class MathBookSearcherProvider : DefaultSearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;

        public MathBookSearcherProvider(MathBookStencil stencil) : base(stencil)
        {
        }

        protected override IReadOnlyList<Type> SupportedTypes => MathBookStencil.SupportedConstants
            .Select(c => c.type.Resolve())
            .ToList();

        public override GraphElementSearcherDatabase InitialGraphElementDatabase(IGraphModel graphModel)
        {
            return AddMathNodes(base.InitialGraphElementDatabase(graphModel));
        }

        public override IReadOnlyList<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return AddMathBookSubgraphs(base.GetGraphElementsSearcherDatabases(graphModel));
        }

        public static GraphElementSearcherDatabase AddMathNodes(GraphElementSearcherDatabase db)
        {
            db.Items.AddRange(MathBookStencil.SupportedConstants.Select(MakeConstantItem).ToList());
            return db;

            SearcherItem MakeConstantItem((TypeHandle type, string name) tuple)
            {
                return new GraphNodeModelSearcherItem(tuple.name + " Constant", null,
                    t => t.GraphModel.CreateConstantNode(tuple.type, "", t.Position, t.Guid, null, t.SpawnFlags))
                    { CategoryPath = "Values" };
            }
        }

        List<SearcherDatabaseBase> AddMathBookSubgraphs(IReadOnlyList<SearcherDatabaseBase> dbs)
        {
            var originalDBs = dbs.ToList();
            var assetPaths = AssetDatabase.FindAssets($"t:{typeof(MathBookAsset)}").Select(AssetDatabase.GUIDToAssetPath).ToList();
            var assetGraphModels = assetPaths.Select(p => AssetDatabase.LoadAssetAtPath(p, typeof(object)) as MathBookAsset)
                .Where(g => g != null && !g.IsContainerGraph() && g.CanBeSubgraph() && ((MathBook)g.GraphModel).SubgraphPropertiesField.ShouldShowInLibrary);

            var handle = Stencil.GetSubgraphNodeTypeHandle();

            var items = assetGraphModels.Select(assetGraphModel =>
                    new GraphNodeModelSearcherItem(assetGraphModel.Name, new TypeSearcherItemData(handle),
                        data => data.CreateSubgraphNode(assetGraphModel))
                    {
                        CategoryPath = ((MathBook)assetGraphModel.GraphModel).SubgraphPropertiesField.GetCategoryPath()
                    }).ToList();

            originalDBs.Add(new SearcherDatabase(items));

            return originalDBs;
        }
    }
}
