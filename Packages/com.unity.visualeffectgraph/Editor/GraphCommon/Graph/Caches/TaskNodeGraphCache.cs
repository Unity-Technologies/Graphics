using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        // Task-node dependency graph
        sealed class TaskNodeGraphCache : ICopyableCache<TaskNodeGraphCache>, ITaskObserver
        {
            LinearGraph<TaskNodeId> m_Graph = new();
            readonly TaskGraph m_Owner;

            public LinearGraph<TaskNodeId> Data => m_Graph;

            public TaskNodeGraphCache(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public void OnTaskNodeAdded(TaskNodeId id, ITask task, string name)
            {
                m_Graph.AddItem(id);
            }

            public void OnTaskDependencyAdded(TaskNodeId child, TaskNodeId parent)
                => m_Graph.Connect(parent.Index, child.Index);

            public void OnTaskDependencyRemoved(TaskNodeId child, TaskNodeId parent)
                => m_Graph.Disconnect(parent.Index, child.Index);

            public void OnTaskNodeRemoved(TaskNodeId id)
                => m_Graph.RemoveNode(id.Index);

            public void OnGraphCleared()
                => m_Graph = new LinearGraph<TaskNodeId>();

            public void RebuildFromPrimary()
            {
                m_Graph = new LinearGraph<TaskNodeId>();
                for (int slot = 0; slot < m_Owner.m_TaskNodes.SlotCount; slot++)
                {
                    if (!m_Owner.m_TaskNodes.IsAliveAtSlot(slot))
                    {
                        m_Graph.AddItem(TaskNodeId.Invalid, 0, 0);
                        continue;
                    }
                    ref var taskNode = ref m_Owner.m_TaskNodes.AtSlot(slot);
                    m_Graph.AddItem(taskNode.Id);
                }
                foreach (var dep in m_Owner.m_TaskDependencies)
                    m_Graph.Connect(dep.ParentNodeId.Index, dep.NodeId.Index);
            }

            public void CopyFrom(TaskNodeGraphCache other) => m_Graph.CopyFrom(other.m_Graph);
        }
    }
}
