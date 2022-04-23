using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    class BBSearcherProvider : DefaultSearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;

        public BBSearcherProvider(BBStencil stencil)
            : base(stencil) { }

        protected override IReadOnlyList<Type> SupportedTypes => BBStencil.SupportedConstants
            .Select(c => c.type.Resolve())
            .ToList();

        public override GraphElementSearcherDatabase InitialGraphElementDatabase(IGraphModel graphModel)
        {
            return AddConstantNodes(base.InitialGraphElementDatabase(graphModel));
        }

        public static GraphElementSearcherDatabase AddConstantNodes(GraphElementSearcherDatabase db)
        {
            db.Items.AddRange(BBStencil.SupportedConstants.Select(MakeConstantItem).ToList());
            return db;

            SearcherItem MakeConstantItem((TypeHandle type, string name) tuple)
            {
                return new GraphNodeModelSearcherItem(tuple.name + " Constant", null,
                        t => t.GraphModel.CreateConstantNode(tuple.type, "", t.Position, t.Guid, null, t.SpawnFlags))
                    { CategoryPath = "Values" };
            }
        }
    }
}
