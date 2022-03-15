using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    class ContextDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> m_BaseDatabases = new List<SearcherDatabaseBase>();

        Dictionary<Type, List<SearcherDatabaseBase>> m_ContextDatabases = new Dictionary<Type, List<SearcherDatabaseBase>>();

        public ContextDatabaseProvider(ContextSampleStencil stencil) : base(stencil)
        {
            var itemList = new List<SearcherItem>
            {
                new GraphNodeModelSearcherItem("Context A", null,
                    t => t.CreateNode(typeof(SampleContextModelA))),
                new GraphNodeModelSearcherItem("Context B", null,
                    t => t.CreateNode(typeof(SampleContextModelB))),
                new GraphNodeModelSearcherItem("Context A with blocks", null,
                    t => CreateContextWithBlocks(t,
                        typeof(SampleContextModelA),
                        typeof(SampleBlockModelA1),
                        typeof(SampleBlockModelA2))),
                new GraphNodeModelSearcherItem("Node", null,
                    t => t.CreateNode(typeof(SampleNodeModel)))
            };

            m_BaseDatabases.Add(new SearcherDatabase(itemList));

            itemList = new List<SearcherItem>
            {
                new GraphNodeModelSearcherItem("Block A1", null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelA1), contextTypeToCreate: typeof(ContextNodeModel))),
                new GraphNodeModelSearcherItem("Block A2", null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelA2), contextTypeToCreate: typeof(ContextNodeModel))),
            };

            var contextDatabase = new SearcherDatabase(itemList);

            m_ContextDatabases.Add(typeof(SampleContextModelA), new List<SearcherDatabaseBase> { contextDatabase });

            itemList = new List<SearcherItem>
            {
                new GraphNodeModelSearcherItem("Block B1", null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelB1), contextTypeToCreate: typeof(ContextNodeModel))),
                new GraphNodeModelSearcherItem("Block B2", null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelB2), contextTypeToCreate: typeof(ContextNodeModel))),
            };

            contextDatabase = new SearcherDatabase(itemList);

            m_ContextDatabases.Add(typeof(SampleContextModelB), new List<SearcherDatabaseBase> { contextDatabase });
        }

        public override IReadOnlyList<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_BaseDatabases;
        }

        public override IReadOnlyList<SearcherDatabaseBase> GetGraphElementContainerSearcherDatabases(IGraphModel graphModel, IGraphElementContainer container)
        {
            return m_ContextDatabases[container.GetType()];
        }

        INodeModel CreateContextWithBlocks(IGraphNodeCreationData data, Type contextType, params Type[] blocksTypes)
        {
            var context = data.CreateNode(contextType) as IContextNodeModel;

            if (context == null)
                return null;

            foreach (var blockType in blocksTypes)
            {
                context.CreateAndInsertBlock(blockType, -1, default, null, data.SpawnFlags);
            }

            return context;
        }
    }
}
