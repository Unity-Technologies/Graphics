using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        sealed class TaskNodeToDataNodesCache : ICopyableCache<TaskNodeToDataNodesCache>, ITaskObserver, IDataNodeObserver
        {
            LinearMultiList<DataNodeId> m_Data = new();
            readonly TaskGraph m_Owner;

            Dictionary<(TaskNodeId, DataContainerId), DataNodeId> m_DataNodeIdDictionary = new();

            public LinearMultiList<DataNodeId> Data => m_Data;

            public TaskNodeToDataNodesCache(TaskGraph owner) => m_Owner = owner;

            public void OnTaskNodeAdded(TaskNodeId id, ITask task, string name)
            {
                m_Data.AddList(1);
            }

            public void OnDataNodeAdded(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            {
                m_Data.AddItem(taskNodeId.Index, id);
                m_DataNodeIdDictionary.Add((taskNodeId, dataContainerId), id);
            }

            public void OnDataNodeRemoved(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            {
                if (m_Data.FindItem(taskNodeId.Index, id, out int itemIndex))
                    m_Data.RemoveItem(taskNodeId.Index, itemIndex);
                m_DataNodeIdDictionary.Remove((taskNodeId, dataContainerId));
            }

            public void OnGraphCleared()
            {
                m_DataNodeIdDictionary.Clear();
                m_Data = new LinearMultiList<DataNodeId>();
            }

            public void RebuildFromPrimary()
            {
                m_Data = new();
                m_DataNodeIdDictionary.Clear();

                for (int slot = 0; slot < m_Owner.m_TaskNodes.SlotCount; slot++)
                    m_Data.AddList();
                foreach (var dataNode in m_Owner.m_DataNodes)
                {
                    m_Data.AddItem(dataNode.TaskNodeId.Index, dataNode.Id);
                    m_DataNodeIdDictionary.Add((dataNode.TaskNodeId, dataNode.DataContainerId), dataNode.Id);
                }
                m_Data.Pack();
            }

            public bool TryGetDataNode(TaskNodeId taskNodeId, DataContainerId containerId, out DataNodeId dataNodeId)
            {
                return m_DataNodeIdDictionary.TryGetValue((taskNodeId, containerId), out dataNodeId);
            }

            public void CopyFrom(TaskNodeToDataNodesCache other)
            {
                m_DataNodeIdDictionary = new Dictionary<(TaskNodeId, DataContainerId), DataNodeId>(other.m_DataNodeIdDictionary);
                m_Data.CopyFrom(other.m_Data);
            }
        }
    }
}
