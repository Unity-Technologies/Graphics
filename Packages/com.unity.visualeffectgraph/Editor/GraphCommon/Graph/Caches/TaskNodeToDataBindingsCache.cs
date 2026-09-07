using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        sealed class TaskNodeToDataBindingsCache : ICopyableCache<TaskNodeToDataBindingsCache>, ITaskObserver, IDataBindingObserver
        {
            LinearMultiList<DataBindingId> m_Data = new();
            readonly TaskGraph m_Owner;

            public LinearMultiList<DataBindingId> Data => m_Data;

            public TaskNodeToDataBindingsCache(TaskGraph owner) => m_Owner = owner;

            public void OnTaskNodeAdded(TaskNodeId id, ITask task, string name)
            {
                m_Data.AddList(1);
            }

            public void OnDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
                TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage)
            {
                m_Data.AddItem(taskNodeId.Index, id);
            }

            public void OnDataBindingRemoved(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
                TaskNodeId taskNodeId, BindingUsage usage)
            {
                if (m_Data.FindItem(taskNodeId.Index, id, out int itemIndex))
                    m_Data.RemoveItem(taskNodeId.Index, itemIndex);
            }

            public void OnGraphCleared() => m_Data = new LinearMultiList<DataBindingId>();

            public void RebuildFromPrimary()
            {
                m_Data = new();
                for (int slot = 0; slot < m_Owner.m_TaskNodes.SlotCount; slot++)
                    m_Data.AddList();
                foreach (var dataBinding in m_Owner.m_DataBindings)
                {
                    if (m_Owner.m_DataNodes.IsAlive(dataBinding.DataNodeId))
                    {
                        var dataNode = m_Owner.m_DataNodes.AtSlot(dataBinding.DataNodeId.Index);
                        m_Data.AddItem(dataNode.TaskNodeId.Index, dataBinding.Id);
                    }
                }
            }

            public void CopyFrom(TaskNodeToDataBindingsCache other) => m_Data.CopyFrom(other.m_Data);
        }
    }
}
