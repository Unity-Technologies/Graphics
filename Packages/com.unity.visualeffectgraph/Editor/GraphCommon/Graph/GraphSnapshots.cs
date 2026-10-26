namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Pooled snapshots of the graph's collections, for loops that mutate what they iterate.
    /// </summary>
    static class GraphSnapshots
    {
        /// <summary>Snapshots the linked data node ids.</summary>
        public static PooledList<DataNodeId> Snapshot(this DataNodeLinks links)
        {
            var snapshot = PooledList<DataNodeId>.Rent();
            for (int i = 0; i < links.Count; i++)
                snapshot.Add(links.GetId(i));
            return snapshot;
        }

        /// <summary>Snapshots the linked task node ids.</summary>
        public static PooledList<TaskNodeId> Snapshot(this TaskNodeLinks links)
        {
            var snapshot = PooledList<TaskNodeId>.Rent();
            for (int i = 0; i < links.Count; i++)
                snapshot.Add(links.GetId(i));
            return snapshot;
        }

        /// <summary>Snapshots the data node ids.</summary>
        public static PooledList<DataNodeId> Snapshot(this DataNodeEnumerable dataNodes)
        {
            var snapshot = PooledList<DataNodeId>.Rent();
            for (int i = 0; i < dataNodes.Count; i++)
                snapshot.Add(dataNodes.GetId(i));
            return snapshot;
        }

        /// <summary>Snapshots the task node ids.</summary>
        public static PooledList<TaskNodeId> Snapshot(this TaskNodeEnumerable taskNodes)
        {
            var snapshot = PooledList<TaskNodeId>.Rent();
            for (int i = 0; i < taskNodes.Count; i++)
                snapshot.Add(taskNodes.GetId(i));
            return snapshot;
        }

        /// <summary>Snapshots the data binding ids.</summary>
        public static PooledList<DataBindingId> Snapshot(this DataBindingEnumerable dataBindings)
        {
            var snapshot = PooledList<DataBindingId>.Rent();
            for (int i = 0; i < dataBindings.Count; i++)
                snapshot.Add(dataBindings.GetId(i));
            return snapshot;
        }

        /// <summary>Snapshots the data view ids.</summary>
        public static PooledList<DataViewId> Snapshot(this DataViewEnumerable dataViews)
        {
            var snapshot = PooledList<DataViewId>.Rent();
            for (int i = 0; i < dataViews.Count; i++)
                snapshot.Add(dataViews.GetId(i));
            return snapshot;
        }

        /// <summary>
        /// Snapshots a data view tree, in the enumerable's pre-order. Walk the result backwards to visit
        /// every view before its parent.
        /// </summary>
        public static PooledList<DataViewId> Snapshot(this DataViewFlatTreeEnumerable dataViews)
        {
            var snapshot = PooledList<DataViewId>.Rent();
            foreach (var dataView in dataViews)
                snapshot.Add(dataView.Id);
            return snapshot;
        }

        /// <summary>Snapshots the data container ids.</summary>
        public static PooledList<DataContainerId> Snapshot(this DataContainerEnumerable dataContainers)
        {
            var snapshot = PooledList<DataContainerId>.Rent();
            for (int i = 0; i < dataContainers.Count; i++)
                snapshot.Add(dataContainers.GetId(i));
            return snapshot;
        }

        /// <summary>Snapshots the ids of every live task node in the graph.</summary>
        public static PooledList<TaskNodeId> Snapshot(this ITaskNodeProvider taskNodes)
        {
            var snapshot = PooledList<TaskNodeId>.Rent();
            foreach (var taskNode in taskNodes)
                snapshot.Add(taskNode.Id);
            return snapshot;
        }

        /// <summary>Snapshots the ids of every live data node in the graph.</summary>
        public static PooledList<DataNodeId> Snapshot(this IDataNodeProvider dataNodes)
        {
            var snapshot = PooledList<DataNodeId>.Rent();
            foreach (var dataNode in dataNodes)
                snapshot.Add(dataNode.Id);
            return snapshot;
        }

        /// <summary>Snapshots the ids of every live data view in the graph.</summary>
        public static PooledList<DataViewId> Snapshot(this IDataViewProvider dataViews)
        {
            var snapshot = PooledList<DataViewId>.Rent();
            foreach (var dataView in dataViews)
                snapshot.Add(dataView.Id);
            return snapshot;
        }

        /// <summary>Snapshots the ids of every live data binding in the graph.</summary>
        public static PooledList<DataBindingId> Snapshot(this IDataBindingProvider dataBindings)
        {
            var snapshot = PooledList<DataBindingId>.Rent();
            foreach (var dataBinding in dataBindings)
                snapshot.Add(dataBinding.Id);
            return snapshot;
        }

        /// <summary>Snapshots the ids of every live data container in the graph.</summary>
        public static PooledList<DataContainerId> Snapshot(this IDataContainerProvider dataContainers)
        {
            var snapshot = PooledList<DataContainerId>.Rent();
            foreach (var dataContainer in dataContainers)
                snapshot.Add(dataContainer.Id);
            return snapshot;
        }
    }
}
