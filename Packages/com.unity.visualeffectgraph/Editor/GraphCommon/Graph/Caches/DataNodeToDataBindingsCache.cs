namespace Unity.GraphCommon.LowLevel.Editor
{
    sealed class DataNodeToDataBindingsCache : ICopyableCache<DataNodeToDataBindingsCache>, IDataNodeObserver, IDataBindingObserver
    {
        LinearMultiList<DataBindingId> m_Data = new();
        readonly ReadOnlyTaskGraphStore m_Store;

        public LinearMultiList<DataBindingId> Data => m_Data;

        public DataNodeToDataBindingsCache(ReadOnlyTaskGraphStore store) => m_Store = store;

        public void OnDataNodeAdded(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
        {
            m_Data.AddList(1);
        }

        public void OnDataBindingAdded(DataBindingId id,  DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage,
            DataPathSet readPaths, DataPathSet writePaths)
            => m_Data.AddItem(dataNodeId.Index, id);

        public void OnDataBindingRemoved(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, BindingUsage usage)
        {
            if (m_Data.FindItem(dataNodeId.Index, id, out int itemIndex))
                m_Data.RemoveItem(dataNodeId.Index, itemIndex);
        }

        public void OnGraphCleared() => m_Data = new LinearMultiList<DataBindingId>();

        public void RebuildFromPrimary()
        {
            m_Data = new();
            for (int slot = 0; slot < m_Store.DataNodes.SlotCount; slot++)
            {
                m_Data.AddList();
                if (!m_Store.DataNodes.IsAliveAtSlot(slot))
                    continue;

                var dataNode = m_Store.DataNodes.AtSlot(slot);
                foreach (var dataBinding in m_Store.DataBindings)
                {
                    if (dataBinding.DataNodeId.Equals(dataNode.Id))
                    {
                        m_Data.AddItem(dataNode.Id.Index, dataBinding.Id);
                    }
                }
            }
        }

        public void CopyFrom(DataNodeToDataBindingsCache other) => m_Data.CopyFrom(other.m_Data);
    }
}
