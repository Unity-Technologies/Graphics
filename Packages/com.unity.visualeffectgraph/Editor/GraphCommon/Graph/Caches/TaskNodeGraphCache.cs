using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        sealed class TaskNodeGraphCache : ICopyableCache<TaskNodeGraphCache>, ITaskObserver, IDataNodeObserver
        {
            LinearDirectedGraph<TaskNodeId> m_Graph = new();
            readonly TaskGraph m_Owner;

            readonly Dictionary<TaskDependency, int> m_EdgeRefCount = new();

            public LinearDirectedGraph<TaskNodeId> Data => m_Graph;

            public TaskNodeGraphCache(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public void OnTaskNodeAdded(TaskNodeId id, ITask task, string name)
            {
                m_Graph.AddItem(id);
            }

            public void OnTaskNodeRemoved(TaskNodeId id)
                => m_Graph.RemoveNode(id.Index);

            public void OnDataDependencyAdded(DataNodeId child, DataNodeId parent)
                => AddTaskEdge(m_Owner.m_DataNodes[child].TaskNodeId, m_Owner.m_DataNodes[parent].TaskNodeId);

            public void OnDataDependencyRemoved(DataNodeId child, DataNodeId parent)
                => RemoveTaskEdge(m_Owner.m_DataNodes[child].TaskNodeId, m_Owner.m_DataNodes[parent].TaskNodeId);

            void AddTaskEdge(TaskNodeId child, TaskNodeId parent)
            {
                var edge = new TaskDependency(child, parent);
                if (m_EdgeRefCount.TryGetValue(edge, out int count))
                {
                    m_EdgeRefCount[edge] = count + 1;
                }
                else
                {
                    m_EdgeRefCount[edge] = 1;
                    m_Graph.Connect(parent.Index, child.Index);
                }
            }

            void RemoveTaskEdge(TaskNodeId child, TaskNodeId parent)
            {
                var edge = new TaskDependency(child, parent);
                if (!m_EdgeRefCount.TryGetValue(edge, out int count))
                    return;

                if (count <= 1)
                {
                    m_EdgeRefCount.Remove(edge);
                    m_Graph.Disconnect(parent.Index, child.Index);
                }
                else
                {
                    m_EdgeRefCount[edge] = count - 1;
                }
            }

            public void OnGraphCleared()
            {
                m_Graph = new LinearDirectedGraph<TaskNodeId>();
                m_EdgeRefCount.Clear();
            }

            public void RebuildFromPrimary()
            {
                m_Graph = new LinearDirectedGraph<TaskNodeId>();
                m_EdgeRefCount.Clear();
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
                foreach (var dep in m_Owner.m_DataDependencies)
                    AddTaskEdge(m_Owner.m_DataNodes[dep.NodeId].TaskNodeId, m_Owner.m_DataNodes[dep.ParentNodeId].TaskNodeId);
            }

            public void CopyFrom(TaskNodeGraphCache other)
            {
                m_Graph.CopyFrom(other.m_Graph);
                m_EdgeRefCount.Clear();
                foreach (var kvp in other.m_EdgeRefCount)
                    m_EdgeRefCount.Add(kvp.Key, kvp.Value);
            }
        }
    }
}
