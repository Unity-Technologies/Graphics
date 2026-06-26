using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        sealed class DataViewToDataContainerCache : ICopyableCache<DataViewToDataContainerCache>, IDataViewObserver
        {
            List<DataContainerId> m_Data = new();
            readonly TaskGraph m_Owner;

            public List<DataContainerId> Data => m_Data;

            public DataViewToDataContainerCache(TaskGraph owner) => m_Owner = owner;

            public void OnDataViewAdded(DataViewId id, DataViewId parentId, DataContainerId dataContainerId, IDataDescription dataDescription)
            {
                m_Data.Add(dataContainerId);
            }

            public void OnDataViewRemoved(DataViewId id, DataViewId parentId, DataContainerId dataContainerId)
            {
                m_Data[id.Index] = DataContainerId.Invalid;
            }

            public void OnGraphCleared() => m_Data.Clear();

            public void RebuildFromPrimary()
            {
                m_Data = new(m_Owner.m_DataViews.SlotCount);
                for (int slot = 0; slot < m_Owner.m_DataViews.SlotCount; slot++)
                {
                    if (!m_Owner.m_DataViews.IsAliveAtSlot(slot))
                    {
                        m_Data.Add(DataContainerId.Invalid);
                        continue;
                    }

                    ref var dataView = ref m_Owner.m_DataViews.AtSlot(slot);
                    DataContainerId dataContainerId = DataContainerId.Invalid;
                    var rootDataViewId = m_Owner.FindRootDataViewId(dataView.Id);
                    foreach (var dataContainer in m_Owner.m_DataContainers)
                    {
                        if (rootDataViewId.Equals(dataContainer.RootDataViewId))
                        {
                            dataContainerId = dataContainer.Id;
                            break;
                        }
                    }
                    m_Data.Add(dataContainerId);
                }
            }

            public void CopyFrom(DataViewToDataContainerCache other)
            {
                m_Data.Clear();
                m_Data.AddRange(other.m_Data);
            }
        }
    }
}
