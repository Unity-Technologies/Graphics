namespace Unity.GraphCommon.LowLevel.Editor
{
    class DataNodeProvider : IDataNodeProvider, IIndexable<GraphNode<DataNodeId>, DataNode>
    {
        readonly TaskGraph m_Owner;
        readonly ReadOnlyTaskGraphStore m_Store;
        readonly DataNodeGraphCache m_Graph;

        public int Count => m_Store.DataNodes.Count;

        public DataNode this[DataNodeId DataNodeId] => this[m_Graph.Data[DataNodeId.Index]];

        public DataNode this[GraphNode<DataNodeId> node] => new(this, node, m_Owner, m_Store.DataNodes[node.Data]);

        public DataNodeProvider(TaskGraph owner, ReadOnlyTaskGraphStore store, DataNodeGraphCache graph)
        {
            m_Owner = owner;
            m_Store = store;
            m_Graph = graph;
        }

        /// <inheritdoc cref="IDataNodeProvider"/>
        public DataNodeEnumerator GetEnumerator() => new(m_Store.DataNodes, this);
    }
}
