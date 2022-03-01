using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    class ContextSampleStencil : Stencil, ISearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> m_BaseDatabases = new List<SearcherDatabaseBase>();

        Dictionary<Type, List<SearcherDatabaseBase>> m_ContextDatabases = new Dictionary<Type, List<SearcherDatabaseBase>>();

        public static string GraphName => "Contexts";

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

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return base.CanPasteNode(originalModel, graph) && !(originalModel is IBlockNodeModel);
        }

        public ContextSampleStencil()
        {
            List<SearcherItem> itemList = new List<SearcherItem>
            {
                new GraphNodeModelSearcherItem(GraphModel, null, t => t.CreateNode(typeof(SampleContextModelA)), "Context A"),
                new GraphNodeModelSearcherItem(GraphModel, null, t => t.CreateNode(typeof(SampleContextModelB)), "Context B"),
                new GraphNodeModelSearcherItem(GraphModel, null,
                    t => CreateContextWithBlocks(t, typeof(SampleContextModelA), typeof(SampleBlockModelA1),
                        typeof(SampleBlockModelA2)), "Context A with blocks"),
                new GraphNodeModelSearcherItem(GraphModel, null, t => t.CreateNode(typeof(SampleNodeModel)), "Node")
            };

            m_BaseDatabases.Add(new SearcherDatabase(itemList));

            itemList = new List<SearcherItem>
            {
                new GraphNodeModelSearcherItem(GraphModel, null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelA1), contextTypeToCreate: typeof(ContextNodeModel)), "Block A1"),
                new GraphNodeModelSearcherItem(GraphModel, null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelA2), contextTypeToCreate: typeof(ContextNodeModel)), "Block A2"),
            };

            var contextDatabase = new SearcherDatabase(itemList);

            m_ContextDatabases.Add(typeof(SampleContextModelA), new List<SearcherDatabaseBase> { contextDatabase });

            itemList = new List<SearcherItem>
            {
                new GraphNodeModelSearcherItem(GraphModel, null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelB1), contextTypeToCreate: typeof(ContextNodeModel)), "Block B1"),
                new GraphNodeModelSearcherItem(GraphModel, null,
                    t => NodeDataCreationExtensions.CreateBlock(t, typeof(SampleBlockModelB2), contextTypeToCreate: typeof(ContextNodeModel)), "Block B2"),
            };

            contextDatabase = new SearcherDatabase(itemList);

            m_ContextDatabases.Add(typeof(SampleContextModelB), new List<SearcherDatabaseBase> { contextDatabase });
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new BlackboardGraphModel(graphAssetModel);
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return this;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_BaseDatabases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetVariableTypesSearcherDatabases()
        {
            return new List<SearcherDatabaseBase>();
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return m_BaseDatabases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return m_BaseDatabases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return m_BaseDatabases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphElementContainerSearcherDatabases(IGraphModel graphModel, IGraphElementContainer container)
        {
            return m_ContextDatabases[container.GetType()];
        }
    }
}
