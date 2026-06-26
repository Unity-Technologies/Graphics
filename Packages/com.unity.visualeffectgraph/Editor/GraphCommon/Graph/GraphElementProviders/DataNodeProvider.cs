namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        class DataNodeProvider : IDataNodeProvider, IIndexable<GraphNode<DataNodeId>, DataNode>
        {
            TaskGraph m_Owner;

            public int Count => m_Owner.m_DataNodes.Count;

            public DataNode this[DataNodeId DataNodeId] => this[m_Owner.DataNodeGraph.Data[DataNodeId.Index]];

            public DataNode this[GraphNode<DataNodeId> node] => new(this, node, m_Owner, m_Owner.m_DataNodes[node.Data]);

            public DataNodeProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            /// <inheritdoc cref="IDataNodeProvider"/>
            public DataNodeEnumerator GetEnumerator() => new(m_Owner.m_DataNodes, this);
        }
    }
}
