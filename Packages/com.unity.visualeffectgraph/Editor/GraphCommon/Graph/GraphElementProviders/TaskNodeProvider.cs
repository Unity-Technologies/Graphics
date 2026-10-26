namespace Unity.GraphCommon.LowLevel.Editor
{
    class TaskNodeProvider : ITaskNodeProvider, IIndexable<GraphNode<TaskNodeId>, TaskNode>
    {
        readonly TaskGraph m_Owner;
        readonly ReadOnlyTaskGraphStore m_Store;
        readonly TaskNodeGraphCache m_Graph;

        public int Count => m_Store.TaskNodes.Count;

        public TaskNode this[TaskNodeId taskNodeId] => this[m_Graph.Data[taskNodeId.Index]];

        public TaskNode this[GraphNode<TaskNodeId> node] => new(this, node, m_Owner, m_Store.TaskNodes[node.Data]);

        public TaskNodeProvider(TaskGraph owner, ReadOnlyTaskGraphStore store, TaskNodeGraphCache graph)
        {
            m_Owner = owner;
            m_Store = store;
            m_Graph = graph;
        }

        /// <inheritdoc cref="ITaskNodeProvider"/>
        public TaskNodeEnumerator GetEnumerator() => new(m_Store.TaskNodes, this);
    }
}
