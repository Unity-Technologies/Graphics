namespace Unity.GraphCommon.LowLevel.Editor
{
    sealed class DataNodeGraphCache : ICopyableCache<DataNodeGraphCache>, IDataNodeObserver
    {
        LinearDirectedGraph<DataNodeId> m_Graph = new();
        readonly ReadOnlyTaskGraphStore m_Store;

        public LinearDirectedGraph<DataNodeId> Data => m_Graph;

        public DataNodeGraphCache(ReadOnlyTaskGraphStore store)
        {
            m_Store = store;
        }

        public void OnDataNodeAdded(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
        {
            m_Graph.AddItem(id);
        }

        public void OnDataDependencyAdded(DataNodeId child, DataNodeId parent)
            => m_Graph.Connect(parent.Index, child.Index);

        public void OnDataDependencyRemoved(DataNodeId child, DataNodeId parent)
            => m_Graph.Disconnect(parent.Index, child.Index);

        public void OnDataNodeRemoved(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            => m_Graph.RemoveNode(id.Index);

        public void OnGraphCleared()
            => m_Graph = new LinearDirectedGraph<DataNodeId>();

        public void RebuildFromPrimary()
        {
            m_Graph = new LinearDirectedGraph<DataNodeId>();
            for (int slot = 0; slot < m_Store.DataNodes.SlotCount; slot++)
            {
                if (!m_Store.DataNodes.IsAliveAtSlot(slot))
                {
                    m_Graph.AddItem(DataNodeId.Invalid, 0, 0);
                    continue;
                }
                var dataNode = m_Store.DataNodes.AtSlot(slot);
                m_Graph.AddItem(dataNode.Id);
            }
            foreach (var dep in m_Store.DataDependencies)
                m_Graph.Connect(dep.ParentNodeId.Index, dep.NodeId.Index);
        }

        public void CopyFrom(DataNodeGraphCache other) => m_Graph.CopyFrom(other.m_Graph);
    }
}
