namespace Unity.GraphCommon.LowLevel.Editor
{
    sealed class TaskNodeToDataBindingsCache : ICopyableCache<TaskNodeToDataBindingsCache>, ITaskObserver, IDataBindingObserver
    {
        LinearMultiList<DataBindingId> m_Data = new();
        readonly ReadOnlyTaskGraphStore m_Store;

        public LinearMultiList<DataBindingId> Data => m_Data;

        public TaskNodeToDataBindingsCache(ReadOnlyTaskGraphStore store) => m_Store = store;

        public void OnTaskNodeAdded(TaskNodeId id, ITask task, string name)
        {
            m_Data.AddList(1);
        }

        public void OnDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage,
            DataPathSet readPaths, DataPathSet writePaths)
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
            for (int slot = 0; slot < m_Store.TaskNodes.SlotCount; slot++)
                m_Data.AddList();
            foreach (var dataBinding in m_Store.DataBindings)
            {
                if (m_Store.DataNodes.IsAlive(dataBinding.DataNodeId))
                {
                    var dataNode = m_Store.DataNodes.AtSlot(dataBinding.DataNodeId.Index);
                    m_Data.AddItem(dataNode.TaskNodeId.Index, dataBinding.Id);
                }
            }
        }

        public void CopyFrom(TaskNodeToDataBindingsCache other) => m_Data.CopyFrom(other.m_Data);
    }
}
