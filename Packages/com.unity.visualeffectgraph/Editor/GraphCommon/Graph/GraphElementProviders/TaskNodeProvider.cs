namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        class TaskNodeProvider : ITaskNodeProvider, IIndexable<GraphNode<TaskNodeId>, TaskNode>
        {
            TaskGraph m_Owner;

            public int Count => m_Owner.m_TaskNodes.Count;

            public TaskNode this[TaskNodeId taskNodeId] => this[m_Owner.TaskNodeGraph.Data[taskNodeId.Index]];

            public TaskNode this[GraphNode<TaskNodeId> node] => new(this, node, m_Owner, m_Owner.m_TaskNodes[node.Data]);

            public TaskNodeProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            /// <inheritdoc cref="ITaskNodeProvider"/>
            public TaskNodeEnumerator GetEnumerator() => new(m_Owner.m_TaskNodes, this);
        }
    }
}
