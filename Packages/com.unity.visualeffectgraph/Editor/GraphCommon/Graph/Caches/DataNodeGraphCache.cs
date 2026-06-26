using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        sealed class DataNodeGraphCache : ICopyableCache<DataNodeGraphCache>, IDataNodeObserver
        {
            LinearGraph<DataNodeId> m_Graph = new();
            readonly TaskGraph m_Owner;

            public LinearGraph<DataNodeId> Data => m_Graph;

            public DataNodeGraphCache(TaskGraph owner)
            {
                m_Owner = owner;
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
                => m_Graph = new LinearGraph<DataNodeId>();

            public void RebuildFromPrimary()
            {
                m_Graph = new LinearGraph<DataNodeId>();
                for (int slot = 0; slot < m_Owner.m_DataNodes.SlotCount; slot++)
                {
                    if (!m_Owner.m_DataNodes.IsAliveAtSlot(slot))
                    {
                        m_Graph.AddItem(DataNodeId.Invalid, 0, 0);
                        continue;
                    }
                    ref var dataNode = ref m_Owner.m_DataNodes.AtSlot(slot);
                    m_Graph.AddItem(dataNode.Id);
                }
                foreach (var dep in m_Owner.m_DataDependencies)
                    m_Graph.Connect(dep.ParentNodeId.Index, dep.NodeId.Index);
            }

            public void CopyFrom(DataNodeGraphCache other) => m_Graph.CopyFrom(other.m_Graph);
        }
    }
}
