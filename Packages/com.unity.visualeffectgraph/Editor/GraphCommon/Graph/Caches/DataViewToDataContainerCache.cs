using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    sealed class DataViewToDataContainerCache : ICopyableCache<DataViewToDataContainerCache>, IDataViewObserver
    {
        List<DataContainerId> m_Data = new();
        readonly ReadOnlyTaskGraphStore m_Store;

        public List<DataContainerId> Data => m_Data;

        public DataViewToDataContainerCache(ReadOnlyTaskGraphStore store) => m_Store = store;

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
            m_Data = new(m_Store.DataViews.SlotCount);
            for (int slot = 0; slot < m_Store.DataViews.SlotCount; slot++)
            {
                if (!m_Store.DataViews.IsAliveAtSlot(slot))
                {
                    m_Data.Add(DataContainerId.Invalid);
                    continue;
                }

                var dataView = m_Store.DataViews.AtSlot(slot);
                DataContainerId dataContainerId = DataContainerId.Invalid;
                var rootDataViewId = FindRootDataViewId(dataView.Id);
                foreach (var dataContainer in m_Store.DataContainers)
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

        DataViewId FindRootDataViewId(DataViewId dataViewId)
        {
            var dataView = m_Store.DataViews[dataViewId];
            while (dataView.ParentDataViewId.IsValid)
                dataView = m_Store.DataViews[dataView.ParentDataViewId];
            return dataView.Id;
        }

        public void CopyFrom(DataViewToDataContainerCache other)
        {
            m_Data.Clear();
            m_Data.AddRange(other.m_Data);
        }
    }
}
