namespace Unity.GraphCommon.LowLevel.Editor
{
    interface IGraphLifecycleObserver
    {
        void OnGraphCleared() {}
    }

    /// <summary>Task node mutation callbacks. </summary>
    interface ITaskObserver : IGraphLifecycleObserver
    {
        void OnTaskNodeAdded(TaskNodeId id, ITask task, string name) {}
        void OnTaskChanged(TaskNodeId id, ITask oldTask, ITask newTask) {}
        void OnTaskNodeRemoved(TaskNodeId id) {}
    }

    /// <summary>Data node mutation callbacks. </summary>
    interface IDataNodeObserver : IGraphLifecycleObserver
    {
        void OnDataNodeAdded(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId) {}
        void OnDataNodeRemoved(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId) {}
        void OnDataDependencyAdded(DataNodeId childDataNodeId, DataNodeId parentDataNodeId) {}
        void OnDataDependencyRemoved(DataNodeId childDataNodeId, DataNodeId parentDataNodeId) {}
    }

    /// <summary>Data binding mutation callbacks. </summary>
    interface IDataBindingObserver : IGraphLifecycleObserver
    {
        void OnDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId, TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage) {}
        void OnDataBindingRemoved(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId, TaskNodeId taskNodeId, BindingUsage usage) {}
    }

    /// <summary>Data view mutation callbacks. </summary>
    interface IDataViewObserver : IGraphLifecycleObserver
    {
        void OnDataViewAdded(DataViewId id, DataViewId parentId, DataContainerId dataContainerId, IDataDescription dataDescription) {}
        void OnDataViewRemoved(DataViewId id, DataViewId parentId, DataContainerId dataContainerId) {}
    }

    /// <summary>Data container mutation callbacks. </summary>
    interface IDataContainerObserver : IGraphLifecycleObserver
    {
        void OnDataContainerAdded(DataContainerId id, string name, DataViewId rootDataViewId) {}
        void OnDataContainerRemoved(DataContainerId id, DataViewId rootDataViewId) {}
    }

    /// <summary>
    /// Cache that can be deep-copied from a peer or rebuilt from primary state.
    /// </summary>
    interface ICopyableCache<TSelf>
        where TSelf : ICopyableCache<TSelf>
    {
        void CopyFrom(TSelf other);
        void RebuildFromPrimary();
    }
}
