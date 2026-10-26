using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    // The primary state, read side: everything else the graph exposes is derived from this, and a
    // rebuild reads nothing else. Caches and providers hold this; only TaskGraph holds the writable
    // TaskGraphStore below.
    readonly struct ReadOnlyTaskGraphStore
    {
        readonly TaskGraphStore m_Store;

        public ReadOnlyTaskGraphStore(TaskGraphStore store) => m_Store = store;

        public static implicit operator ReadOnlyTaskGraphStore(TaskGraphStore store) => new(store);

        public ReadOnlyGraphDataList<TaskNodeInfo> TaskNodes => m_Store.TaskNodes;
        public ReadOnlyGraphDataList<DataNodeInfo> DataNodes => m_Store.DataNodes;
        public ReadOnlyGraphDataList<DataContainerInfo> DataContainers => m_Store.DataContainers;
        public ReadOnlyGraphDataList<DataViewInfo> DataViews => m_Store.DataViews;
        public ReadOnlyGraphDataList<DataBindingInfo> DataBindings => m_Store.DataBindings;

        public IReadOnlyCollection<DataDependency> DataDependencies => m_Store.DataDependencies;
    }

    sealed class TaskGraphStore
    {
        readonly GraphDataList<TaskNodeInfo> m_TaskNodes;
        readonly GraphDataList<DataNodeInfo> m_DataNodes;
        readonly GraphDataList<DataContainerInfo> m_DataContainers;
        readonly GraphDataList<DataViewInfo> m_DataViews;
        readonly GraphDataList<DataBindingInfo> m_DataBindings;

        readonly HashSet<DataDependency> m_DataDependencies;

        public TaskGraphStore()
        {
            m_TaskNodes = new();
            m_DataNodes = new();
            m_DataContainers = new();
            m_DataViews = new();
            m_DataBindings = new();

            m_DataDependencies = new();
        }

        public TaskGraphStore(TaskGraphStore source)
        {
            m_TaskNodes = new(source.m_TaskNodes);
            m_DataNodes = new(source.m_DataNodes);
            m_DataContainers = new(source.m_DataContainers);
            m_DataViews = new(source.m_DataViews);
            m_DataBindings = new(source.m_DataBindings);

            m_DataDependencies = new(source.m_DataDependencies);
        }

        public ReadOnlyGraphDataList<TaskNodeInfo> TaskNodes => m_TaskNodes;
        public ReadOnlyGraphDataList<DataNodeInfo> DataNodes => m_DataNodes;
        public ReadOnlyGraphDataList<DataContainerInfo> DataContainers => m_DataContainers;
        public ReadOnlyGraphDataList<DataViewInfo> DataViews => m_DataViews;
        public ReadOnlyGraphDataList<DataBindingInfo> DataBindings => m_DataBindings;

        public IReadOnlyCollection<DataDependency> DataDependencies => m_DataDependencies;

        // The ref comes back from the indexer, not from Allocate: an out argument caps the escape scope
        // of a ref return, so Allocate's own ref cannot leave this method (CS8157).
        public ref TaskNodeInfo AllocateTaskNode(out TaskNodeId id)
        {
            m_TaskNodes.Allocate(out GraphDataId allocated);
            id = allocated;
            return ref m_TaskNodes[allocated];
        }

        public ref DataNodeInfo AllocateDataNode(out DataNodeId id)
        {
            m_DataNodes.Allocate(out GraphDataId allocated);
            id = allocated;
            return ref m_DataNodes[allocated];
        }

        public ref DataContainerInfo AllocateDataContainer(out DataContainerId id)
        {
            m_DataContainers.Allocate(out GraphDataId allocated);
            id = allocated;
            return ref m_DataContainers[allocated];
        }

        public ref DataViewInfo AllocateDataView(out DataViewId id)
        {
            m_DataViews.Allocate(out GraphDataId allocated);
            id = allocated;
            return ref m_DataViews[allocated];
        }

        public ref DataBindingInfo AllocateDataBinding(out DataBindingId id)
        {
            m_DataBindings.Allocate(out GraphDataId allocated);
            id = allocated;
            return ref m_DataBindings[allocated];
        }

        public void RemoveTaskNode(TaskNodeId id) => m_TaskNodes.Remove(id);
        public void RemoveDataNode(DataNodeId id) => m_DataNodes.Remove(id);
        public void RemoveDataContainer(DataContainerId id) => m_DataContainers.Remove(id);
        public void RemoveDataView(DataViewId id) => m_DataViews.Remove(id);
        public void RemoveDataBinding(DataBindingId id) => m_DataBindings.Remove(id);

        public void SetDataView(DataViewId id, DataViewInfo info) => m_DataViews[id] = info;

        public bool AddDependency(DataDependency dependency) => m_DataDependencies.Add(dependency);
        public bool RemoveDependency(DataDependency dependency) => m_DataDependencies.Remove(dependency);

        public void Clear()
        {
            m_TaskNodes.Clear();
            m_DataNodes.Clear();
            m_DataContainers.Clear();
            m_DataViews.Clear();
            m_DataBindings.Clear();

            m_DataDependencies.Clear();
        }
    }
}
