using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a graph of tasks and data nodes with their dependencies.
    /// </summary>
    sealed partial class TaskGraph : IGraph
    {
        // Note: not readonly so the copy ctor can rebind via CopyPrimaryStateFrom.
        // Reference reassignment otherwise never happens at runtime.
        GraphDataList<TaskNodeInfo> m_TaskNodes = new();
        GraphDataList<DataNodeInfo> m_DataNodes = new();
        GraphDataList<DataContainerInfo> m_DataContainers = new();
        GraphDataList<DataViewInfo> m_DataViews = new();
        GraphDataList<DataBindingInfo> m_DataBindings = new();

        HashSet<DataDependency> m_DataDependencies = new();

        TaskNodeProvider m_TaskNodeProvider;
        DataNodeProvider m_DataNodeProvider;
        DataViewProvider m_DataViewProvider;
        DataBindingProvider m_DataBindingProvider;
        DataContainerProvider m_DataContainerProvider;

        readonly List<IGraphLifecycleObserver>  m_LifecycleObservers    = new();
        readonly List<ITaskObserver>            m_TaskObservers          = new();
        readonly List<IDataNodeObserver>        m_DataNodeObservers      = new();
        readonly List<IDataBindingObserver>     m_DataBindingObservers   = new();
        readonly List<IDataViewObserver>        m_DataViewObservers      = new();
        readonly List<IDataContainerObserver>   m_DataContainerObservers = new();

        /// <summary>
        /// Gets the current version of the graph. Increases whenever the graph is modified.
        /// </summary>
        public uint Version { get; private set; } = 1u; // 0u reserved for null/invalid version

        /// <inheritdoc/>
        public ITaskNodeProvider TaskNodes => m_TaskNodeProvider;
        /// <inheritdoc/>
        public IDataNodeProvider DataNodes => m_DataNodeProvider;
        /// <inheritdoc/>
        public IDataViewProvider DataViews => m_DataViewProvider;
        /// <inheritdoc/>
        public IDataBindingProvider DataBindings => m_DataBindingProvider;
        /// <inheritdoc/>
        public IDataContainerProvider DataContainers => m_DataContainerProvider;

        TaskNodeGraphCache TaskNodeGraph { get; }
        DataNodeGraphCache DataNodeGraph { get; }
        DataViewTreesCache DataViewTrees { get; }
        TaskNodeToDataNodesCache TaskNodeToDataNodes { get; }
        TaskNodeToDataBindingsCache TaskNodeToDataBindings { get; }
        DataNodeToDataViewsCache DataNodeToDataViews { get; }
        DataNodeToDataBindingsCache DataNodeToDataBindings { get; }
        DataViewToDataContainerCache DataViewToDataContainer { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="TaskGraph"/>.
        /// </summary>
        public TaskGraph() : this(source: null, copyCache: false) { }

        private TaskGraph(TaskGraph source, bool copyCache = true)
        {
            InitProviders();
            if (source != null) CopyPrimaryStateFrom(source);

            DataViewTrees              = RegisterCache(new DataViewTreesCache(this),              source?.DataViewTrees,              copyCache);
            TaskNodeGraph              = RegisterCache(new TaskNodeGraphCache(this),              source?.TaskNodeGraph,              copyCache);
            DataNodeGraph              = RegisterCache(new DataNodeGraphCache(this),              source?.DataNodeGraph,              copyCache);
            TaskNodeToDataNodes        = RegisterCache(new TaskNodeToDataNodesCache(this),        source?.TaskNodeToDataNodes,        copyCache);
            TaskNodeToDataBindings     = RegisterCache(new TaskNodeToDataBindingsCache(this),     source?.TaskNodeToDataBindings,     copyCache);
            DataNodeToDataBindings     = RegisterCache(new DataNodeToDataBindingsCache(this),     source?.DataNodeToDataBindings,     copyCache);
            DataViewToDataContainer    = RegisterCache(new DataViewToDataContainerCache(this),    source?.DataViewToDataContainer,    copyCache);
            DataNodeToDataViews        = RegisterCache(new DataNodeToDataViewsCache(this),        source?.DataNodeToDataViews,        copyCache);
        }

        void InitProviders()
        {
            m_TaskNodeProvider      = new TaskNodeProvider(this);
            m_DataNodeProvider      = new DataNodeProvider(this);
            m_DataViewProvider      = new DataViewProvider(this);
            m_DataBindingProvider   = new DataBindingProvider(this);
            m_DataContainerProvider = new DataContainerProvider(this);
        }

        void CopyPrimaryStateFrom(TaskGraph source)
        {
            m_TaskNodes      = new(source.m_TaskNodes);
            m_DataNodes      = new(source.m_DataNodes);
            m_DataContainers = new(source.m_DataContainers);
            m_DataViews      = new(source.m_DataViews);
            m_DataBindings   = new(source.m_DataBindings);

            m_DataDependencies = new(source.m_DataDependencies);
        }

        T RegisterCache<T>(T cache, T sourcePeer, bool copyCache) where T : class, ICopyableCache<T>
        {
            if (sourcePeer != null)
            {
                if (copyCache) cache.CopyFrom(sourcePeer);
                else cache.RebuildFromPrimary();
            }
            RegisterObserver(cache);
            return cache;
        }

        internal void RegisterObserver(object observer)
        {
            if (observer is IGraphLifecycleObserver lifecycle) m_LifecycleObservers.Add(lifecycle);
            if (observer is ITaskObserver task)                m_TaskObservers.Add(task);
            if (observer is IDataNodeObserver dataNode)        m_DataNodeObservers.Add(dataNode);
            if (observer is IDataBindingObserver dataBinding)  m_DataBindingObservers.Add(dataBinding);
            if (observer is IDataViewObserver dataView)        m_DataViewObservers.Add(dataView);
            if (observer is IDataContainerObserver container)  m_DataContainerObservers.Add(container);
        }

        internal void UnregisterObserver(object observer)
        {
            if (observer is IGraphLifecycleObserver lifecycle) m_LifecycleObservers.Remove(lifecycle);
            if (observer is ITaskObserver task)                m_TaskObservers.Remove(task);
            if (observer is IDataNodeObserver dataNode)        m_DataNodeObservers.Remove(dataNode);
            if (observer is IDataBindingObserver dataBinding)  m_DataBindingObservers.Remove(dataBinding);
            if (observer is IDataViewObserver dataView)        m_DataViewObservers.Remove(dataView);
            if (observer is IDataContainerObserver container)  m_DataContainerObservers.Remove(container);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            m_TaskNodes.Clear();
            m_DataNodes.Clear();
            m_DataContainers.Clear();
            m_DataViews.Clear();
            m_DataBindings.Clear();
            m_DataDependencies.Clear();

            Version = 1u;
            NotifyGraphCleared();
        }

        /// <inheritdoc/>
        public TaskNodeId AddTask(ITask task, string name = null)
        {
            Version++;
            m_TaskNodes.Allocate(out var taskNodeId) = new TaskNodeInfo(taskNodeId, task, name);
            NotifyTaskNodeAdded(taskNodeId, task, name);
            return taskNodeId;
        }

        /// <inheritdoc/>
        public DataViewId AddData(string name, IDataDescription dataDescription)
        {
            Version++;

            m_DataViews.Allocate(out var dataViewId) = new DataViewInfo(dataViewId, dataDescription);
            m_DataContainers.Allocate(out var dataContainerId) = new DataContainerInfo(dataContainerId, name, dataViewId);

            NotifyDataContainerAdded(dataContainerId, name, dataViewId);
            NotifyDataViewAdded(dataViewId, DataViewId.Invalid, dataContainerId, dataDescription);

            return dataViewId;
        }

        //TODO: TEMPORARY, we don't really support mutability as of now
        /// <inheritdoc/>
        public void OverrideDataDescription(DataViewId dataViewId, IDataDescription dataDescription)
        {
            DataViewInfo dataViewInfo = m_DataViews[dataViewId];
            Debug.Assert(dataViewInfo.DataDescription.IsCompatible(dataDescription));
            m_DataViews[dataViewId] = new DataViewInfo(dataViewInfo.Id, dataDescription, dataViewInfo.ParentDataViewId, dataViewInfo.SubDataKey );
        }

        /// <inheritdoc/>
        public DataViewId GetSubdata(DataViewId parentDataViewId, IDataKey subDataKey, IDataDescription dataDescription = null)
        {
            if (!DataViewTrees.TryGetSubView(parentDataViewId, subDataKey, out DataViewId dataViewId))
            {
                dataViewId = DataViewId.Invalid;
                IDataDescription targetDataDescription = m_DataViews[parentDataViewId].DataDescription.GetSubdata(subDataKey);
                if (targetDataDescription != null)
                {
                    if (dataDescription == null)
                    {
                        dataDescription = targetDataDescription;
                    }
                    else
                    {
                        Debug.Assert(targetDataDescription.IsCompatible(dataDescription));
                    }
                    Version++;

                    var containerId = DataViews[parentDataViewId].DataContainer.Id;
                    m_DataViews.Allocate(out var newDataViewId) = new DataViewInfo(newDataViewId, dataDescription, parentDataViewId, subDataKey);
                    dataViewId = newDataViewId;

                    NotifyDataViewAdded(dataViewId, parentDataViewId, containerId, dataDescription);
                }
            }
            else if (dataDescription != null)
            {
                Debug.Assert(m_DataViews[dataViewId].DataDescription.IsCompatible(dataDescription));
            }

            return dataViewId;
        }

        /// <inheritdoc/>
        public DataViewId GetSubdata(DataViewId parentDataViewId, DataPath subdataPath)
        {
            var currentDataViewId = parentDataViewId;
            foreach (var subDataKey in subdataPath)
            {
                currentDataViewId = subDataKey != null ? GetSubdata(currentDataViewId, subDataKey) : currentDataViewId;
                if (!currentDataViewId.IsValid)
                    break;
            }
            return currentDataViewId;
        }

        internal void ResolveBindingUsage(TaskNodeId taskNodeId, IDataKey bindingKey, ref BindingUsage usage,
            out DataPathSet readPaths, out DataPathSet writePaths)
        {
            readPaths = new();
            writePaths = new();
            var usageFromTask = m_TaskNodes[taskNodeId].Task.GetBindingUsage(bindingKey, readPaths, writePaths);

            if (usage != BindingUsage.Unknown)
            {
                if (usageFromTask != BindingUsage.Unknown && usage != usageFromTask)
                {
                    Debug.LogWarning($"Provided binding usage {usage} doesn't match binding usage {usageFromTask} from task node");
                    usage = BindingUsage.Unknown;
                }
            }
            else
            {
                usage = usageFromTask;
            }
        }

        /// <inheritdoc/>
        public void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId,
            IEnumerable<DataNodeId> parentNodeIds, BindingUsage usage = BindingUsage.Unknown)
        {
            ResolveBindingUsage(taskNodeId, bindingKey, ref usage, out var readPaths, out var writePaths);
            BindData(taskNodeId, bindingKey, dataViewId, usage, parentNodeIds, readPaths, writePaths);
        }

        // Shared core, taking usage and paths already resolved. Internal so a TaskGraphBuilder can
        // resolve usage once, infer the parents from it, and bind without resolving a second time.
        internal void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage,
            IEnumerable<DataNodeId> parentNodeIds, DataPathSet readPaths, DataPathSet writePaths)
        {
            Debug.Assert(dataViewId.IsValid);

            if (usage == BindingUsage.Unknown)
            {
                Debug.LogWarning("Binding usage cannot be determined. Skipping binding data.");
                return;
            }
            Version++;

            CreateDataViewsFromUsage(dataViewId, usage, readPaths, writePaths);

            var dataContainerId = DataViews[dataViewId].DataContainer.Id;

            if (!TaskNodeToDataNodes.TryGetDataNode(taskNodeId, dataContainerId, out var dataNodeId))
            {
                m_DataNodes.Allocate(out var newDataNodeId) = new DataNodeInfo(newDataNodeId, taskNodeId, dataContainerId);
                NotifyDataNodeAdded(newDataNodeId, taskNodeId, dataContainerId);
                dataNodeId = newDataNodeId;
            }

            m_DataBindings.Allocate(out var dataBindingId) =
                new DataBindingInfo(dataBindingId, dataNodeId, dataViewId, bindingKey, usage);
            NotifyDataBindingAdded(dataBindingId, dataViewId, dataNodeId, taskNodeId, bindingKey, usage);

            foreach (var parentNodeId in parentNodeIds)
            {
                AddDataDependency(dataNodeId, parentNodeId);
            }
        }

        void CreateDataViewsFromUsage(DataViewId dataViewId, BindingUsage usage, DataPathSet readPaths,
            DataPathSet writePaths)
        {
            if (!readPaths.Empty)
            {
                Debug.Assert(usage.HasFlag(BindingUsage.Read));
                foreach (var path in readPaths)
                {
                    var currentDataViewId = dataViewId;
                    foreach (var key in path)
                    {
                        currentDataViewId = key != null ? GetSubdata(currentDataViewId, key) : currentDataViewId;
                        if (!currentDataViewId.IsValid)
                            break;
                    }
                }
            }

            if (!writePaths.Empty)
            {
                Debug.Assert(usage.HasFlag(BindingUsage.Write));
                foreach (var path in writePaths)
                {
                    var currentDataViewId = dataViewId;
                    foreach (var key in path)
                    {
                        currentDataViewId = key != null ? GetSubdata(currentDataViewId, key) : currentDataViewId;
                        if (!currentDataViewId.IsValid)
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a dependency relationship between two data nodes.
        /// </summary>
        /// <param name="dataNodeId">The ID of the dependent data node.</param>
        /// <param name="parentDataNodeId">The ID of the parent data node.</param>
        /// <returns>True if the dependency was added, false if it already existed.</returns>
        public bool AddDataDependency(DataNodeId dataNodeId, DataNodeId parentDataNodeId)
        {
            Debug.Assert(dataNodeId.IsValid);
            Debug.Assert(parentDataNodeId.IsValid);
            Debug.Assert(!dataNodeId.Equals(parentDataNodeId), $"Data node {dataNodeId} cannot depend on itself.");
            bool added = m_DataDependencies.Add(new DataDependency(dataNodeId, parentDataNodeId));
            if (added)
            {
                Version++;
                NotifyDataDependencyAdded(dataNodeId, parentDataNodeId);
            }

            return added;
        }

        /// <inheritdoc/>
        public IGraph Copy()
        {
            return new TaskGraph(this);
        }

        /// <inheritdoc/>
        DataNodeEnumerable IReadOnlyGraph.GetDataNodes(TaskNodeId taskNodeId)
        {
            var container = TaskNodeToDataNodes.Data;
            SubEnumerable<DataNodeId> subEnumerable = new(container, taskNodeId.Index, container[taskNodeId.Index].Count);
            return new(DataNodes, subEnumerable);
        }

        /// <inheritdoc/>
        DataBindingEnumerable IReadOnlyGraph.GetDataBindings(TaskNodeId taskNodeId)
        {
            var container = TaskNodeToDataBindings.Data;
            SubEnumerable<DataBindingId> subEnumerable = new(container, taskNodeId.Index, container[taskNodeId.Index].Count);
            return new(DataBindings, subEnumerable);
        }

        /// <inheritdoc/>
        DataBindingEnumerable IReadOnlyGraph.GetDataBindings(DataNodeId dataNodeId)
        {
            var container = DataNodeToDataBindings.Data;
            SubEnumerable<DataBindingId> subEnumerable = new(container, dataNodeId.Index, container[dataNodeId.Index].Count);
            return new(DataBindings, subEnumerable);
        }


        /// <inheritdoc/>
        DataView IReadOnlyGraph.GetUsedDataViews(DataNodeId dataNodeId)
        {
            var treeNode = DataNodeToDataViews.Data.RootNodes[dataNodeId.Index];
            return treeNode.Data.IsValid ? new(m_DataViewProvider, treeNode, this, m_DataViews[treeNode.Data]) : new();
        }

        /// <inheritdoc/>
        bool IReadOnlyGraph.IsRead(DataNodeId dataNodeId, DataViewId dataViewId) => DataNodeToDataViews.IsRead(dataNodeId, dataViewId);

        /// <inheritdoc/>
        bool IReadOnlyGraph.IsWritten(DataNodeId dataNodeId, DataViewId dataViewId) => DataNodeToDataViews.IsWritten(dataNodeId, dataViewId);

        /// <inheritdoc/>
        DataContainer IReadOnlyGraph.GetDataContainer(DataViewId dataViewId)
        {
            var dataContainerId = DataViewToDataContainer.Data[dataViewId.Index];
            return DataContainers[dataContainerId];
        }

        /// <inheritdoc/>
        bool IReadOnlyGraph.TryGetSubView(DataViewId parentDataViewId, IDataKey subDataKey, out DataViewId dataViewId)
            => DataViewTrees.TryGetSubView(parentDataViewId, subDataKey, out dataViewId);

        // Reads the declaration back from ITask.GetBindingUsage rather than from DataNodeToDataViews: the
        // cached Read/Written bits propagate upward, so every node touching a container overlaps at its root.
        // Either output may be null to collect only the other side.
        void CollectDeclaredViews(DataNode dataNode, List<DataViewId> readViews, List<DataViewId> writeViews)
        {
            readViews?.Clear();
            writeViews?.Clear();

            var readUsage = readViews != null ? new DataPathSet() : null;
            var writeUsage = writeViews != null ? new DataPathSet() : null;

            var task = dataNode.TaskNode.Task;
            foreach (var binding in dataNode.DataBindings)
            {
                readUsage?.Clear();
                writeUsage?.Clear();
                if (task.GetBindingUsage(binding.BindingDataKey, readUsage, writeUsage) == BindingUsage.Unknown)
                    continue;

                if (readViews != null)
                    AddResolvedPaths(binding.DataView, readUsage, readViews);
                if (writeViews != null)
                    AddResolvedPaths(binding.DataView, writeUsage, writeViews);
            }
        }

        void AddResolvedPaths(DataView bindingDataView, DataPathSet paths, List<DataViewId> result)
        {
            foreach (var path in paths)
            {
                if (bindingDataView.FindSubData(path, out var subDataView))
                    AddTopmost(result, subDataView);
            }
        }

        // Whether one of views contains dataViewId, itself included.
        bool IsCovered(List<DataViewId> views, DataViewId dataViewId)
        {
            foreach (var viewId in views)
            {
                if (DataViews[viewId].ContainsSubData(dataViewId))
                    return true;
            }
            return false;
        }

        void AddTopmost(List<DataViewId> views, DataView candidate)
        {
            if (IsCovered(views, candidate.Id))
                return;

            if (candidate.Children.Count > 0)
            {
                for (int i = views.Count - 1; i >= 0; i--)
                {
                    if (candidate.ContainsSubData(views[i]))
                        views.RemoveAt(i);
                }
            }

            views.Add(candidate.Id);
        }

        void SpliceOutDataNode(DataNode dataNode, GraphTraverser traverser)
        {
            using var parentIds = dataNode.Parents.Snapshot();
            using var childIds = dataNode.Children.Snapshot();

            foreach (var childId in childIds)
            {
                if (m_DataDependencies.Remove(new DataDependency(childId, dataNode.Id)))
                    NotifyDataDependencyRemoved(childId, dataNode.Id);
            }

            foreach (var childId in childIds)
                ReconnectOrphanedChild(dataNode, childId, traverser);

            foreach (var parentId in parentIds)
            {
                if (m_DataDependencies.Remove(new DataDependency(dataNode.Id, parentId)))
                    NotifyDataDependencyRemoved(dataNode.Id, parentId);
            }
        }

        void ReconnectOrphanedChild(DataNode removedNode, DataNodeId childId, GraphTraverser traverser)
        {
            using (ListPool<DataViewId>.Get(out var childReads))
            using (ListPool<DataViewId>.Get(out var ancestorWrites))
            using (ListPool<DataViewId>.Get(out var claimedWrites))
            using (ListPool<DataNodeId>.Get(out var candidates))
            {
                CollectDeclaredViews(DataNodes[childId], childReads, null);
                if (childReads.Count == 0)
                    return;

                foreach (var ancestor in traverser.TraverseDataUpwardsBreadthFirst(removedNode))
                {
                    if (childReads.Count == 0)
                        break;
                    if (ancestor.Id.Equals(removedNode.Id))
                        continue;

                    CollectDeclaredViews(ancestor, null, ancestorWrites);
                    if (ClaimWrittenViews(childReads, ancestorWrites, claimedWrites))
                        candidates.Add(ancestor.Id);
                }

                foreach (var candidateNodeId in candidates)
                    AddDataDependency(childId, candidateNodeId);
            }
        }

        // Matches one ancestor's writes against the reads still looking for a writer; true if any landed.
        // A read survives a partial cover, so a whole-container reader still reaches every sub-view writer.
        bool ClaimWrittenViews(List<DataViewId> reads, List<DataViewId> writes, List<DataViewId> claimedWrites)
        {
            bool claimedAny = false;
            foreach (var writeId in writes)
            {
                // A nearer node already took responsibility for this region.
                if (IsCovered(claimedWrites, writeId))
                    continue;

                var write = DataViews[writeId];
                bool overlaps = false;
                for (int r = reads.Count - 1; r >= 0; r--)
                {
                    var readId = reads[r];
                    if (write.ContainsSubData(readId))
                    {
                        // Covered whole: nothing of this read is left for a further ancestor to write.
                        reads.RemoveAt(r);
                        overlaps = true;
                    }
                    else if (DataViews[readId].ContainsSubData(writeId))
                    {
                        // Covered in part: the remainder of the read still needs its own writer.
                        overlaps = true;
                    }
                }

                if (overlaps)
                {
                    AddTopmost(claimedWrites, write);
                    claimedAny = true;
                }
            }
            return claimedAny;
        }

        void RemoveDataNodeAndBindings(DataNodeId dataNodeId)
        {
            var dataNodeInfo = m_DataNodes[dataNodeId];
            using var bindingIds = DataNodes[dataNodeId].DataBindings.Snapshot();

            m_DataNodes.Remove(dataNodeId);
            NotifyDataNodeRemoved(dataNodeId, dataNodeInfo.TaskNodeId, dataNodeInfo.DataContainerId);

            foreach (var bindingId in bindingIds)
            {
                var bindingInfo = m_DataBindings[bindingId];
                m_DataBindings.Remove(bindingId);
                NotifyDataBindingRemoved(bindingId, bindingInfo.DataViewId, dataNodeId, dataNodeInfo.TaskNodeId,
                    bindingInfo.Usage);
            }
        }

        /// <inheritdoc/>
        public void RemoveTask(TaskNodeId taskNodeId)
        {
            var taskNode = TaskNodes[taskNodeId];

            using var dataNodeIds = taskNode.DataNodes.Snapshot();

            var traverser = ((IReadOnlyGraph)this).CreateTraverser();
            foreach (var dataNodeId in dataNodeIds)
                SpliceOutDataNode(DataNodes[dataNodeId], traverser);

            foreach (var dataNodeId in dataNodeIds)
                RemoveDataNodeAndBindings(dataNodeId);

            m_TaskNodes.Remove(taskNodeId);
            NotifyTaskNodeRemoved(taskNodeId);
            Version++;
        }

        /// <inheritdoc/>
        public void UnbindData(TaskNodeId taskNodeId, IDataKey bindingKey)
        {
            var dataBinding = TaskNodes[taskNodeId].DataBindings[bindingKey];
            if (!dataBinding.HasValue)
                return;

            var dataNode = dataBinding.Value.DataNode;

            // A data node exists to carry its bindings, so it goes away with the last one.
            if (dataNode.DataBindings.Count == 1)
            {
                SpliceOutDataNode(dataNode, ((IReadOnlyGraph)this).CreateTraverser());
                RemoveDataNodeAndBindings(dataNode.Id);
            }
            else
            {
                var bindingInfo = m_DataBindings[dataBinding.Value.Id];
                m_DataBindings.Remove(bindingInfo.Id);
                NotifyDataBindingRemoved(bindingInfo.Id, bindingInfo.DataViewId, dataNode.Id, taskNodeId,
                    bindingInfo.Usage);
            }
            Version++;
        }

        /// <inheritdoc/>
        public void RemoveDataContainer(DataContainerId dataContainerId)
        {
            var dataContainer = DataContainers[dataContainerId];
            var rootDataViewId = dataContainer.RootDataView.Id;
            using var dataViewIds = dataContainer.RootDataView.Flat.Snapshot();

            using (ListPool<DataNodeId>.Get(out var dataNodeIds))
            {
                foreach (var dataNode in DataNodes)
                {
                    if (dataNode.DataContainer.Id.Equals(dataContainerId))
                        dataNodeIds.Add(dataNode.Id);
                }

                var traverser = ((IReadOnlyGraph)this).CreateTraverser();
                foreach (var dataNodeId in dataNodeIds)
                    SpliceOutDataNode(DataNodes[dataNodeId], traverser);

                foreach (var dataNodeId in dataNodeIds)
                    RemoveDataNodeAndBindings(dataNodeId);
            }

            for (int i = dataViewIds.Count - 1; i >= 0; i--)
            {
                var dataViewId = dataViewIds[i];
                NotifyDataViewRemoved(dataViewId, m_DataViews[dataViewId].ParentDataViewId, dataContainerId);
                m_DataViews.Remove(dataViewId);
            }

            m_DataContainers.Remove(dataContainerId);
            NotifyDataContainerRemoved(dataContainerId, rootDataViewId);
            Version++;
        }
        GraphTraverser IReadOnlyGraph.CreateTraverser() => new GraphTraverser(this);

        void NotifyTaskNodeAdded(TaskNodeId id, ITask task, string name)
        {
            for (int i = 0; i < m_TaskObservers.Count; i++)
                m_TaskObservers[i].OnTaskNodeAdded(id, task, name);
        }

        void NotifyTaskChanged(TaskNodeId id, ITask oldTask, ITask newTask)
        {
            for (int i = 0; i < m_TaskObservers.Count; i++)
                m_TaskObservers[i].OnTaskChanged(id, oldTask, newTask);
        }

        void NotifyTaskNodeRemoved(TaskNodeId id)
        {
            for (int i = m_TaskObservers.Count - 1; i >= 0; i--)
                m_TaskObservers[i].OnTaskNodeRemoved(id);
        }

        void NotifyDataContainerAdded(DataContainerId id, string name, DataViewId rootDataViewId)
        {
            for (int i = 0; i < m_DataContainerObservers.Count; i++)
                m_DataContainerObservers[i].OnDataContainerAdded(id, name, rootDataViewId);
        }

        void NotifyDataViewAdded(DataViewId id, DataViewId parent, DataContainerId container, IDataDescription description)
        {
            for (int i = 0; i < m_DataViewObservers.Count; i++)
                m_DataViewObservers[i].OnDataViewAdded(id, parent, container, description);
        }

        void NotifyDataViewRemoved(DataViewId id, DataViewId parent, DataContainerId container)
        {
            for (int i = m_DataViewObservers.Count - 1; i >= 0; i--)
                m_DataViewObservers[i].OnDataViewRemoved(id, parent, container);
        }

        void NotifyDataContainerRemoved(DataContainerId id, DataViewId rootDataViewId)
        {
            for (int i = m_DataContainerObservers.Count - 1; i >= 0; i--)
                m_DataContainerObservers[i].OnDataContainerRemoved(id, rootDataViewId);
        }

        void NotifyDataNodeAdded(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
        {
            for (int i = 0; i < m_DataNodeObservers.Count; i++)
                m_DataNodeObservers[i].OnDataNodeAdded(id, taskNodeId, dataContainerId);
        }

        void NotifyDataNodeRemoved(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
        {
            for (int i = m_DataNodeObservers.Count - 1; i >= 0; i--)
                m_DataNodeObservers[i].OnDataNodeRemoved(id, taskNodeId, dataContainerId);
        }

        void NotifyDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage)
        {
            for (int i = 0; i < m_DataBindingObservers.Count; i++)
                m_DataBindingObservers[i].OnDataBindingAdded(id, dataViewId, dataNodeId, taskNodeId, bindingKey, usage);
        }

        void NotifyDataBindingRemoved(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, BindingUsage usage)
        {
            for (int i = m_DataBindingObservers.Count - 1; i >= 0; i--)
                m_DataBindingObservers[i].OnDataBindingRemoved(id, dataViewId, dataNodeId, taskNodeId, usage);
        }

        void NotifyDataDependencyAdded(DataNodeId child, DataNodeId parent)
        {
            for (int i = 0; i < m_DataNodeObservers.Count; i++)
                m_DataNodeObservers[i].OnDataDependencyAdded(child, parent);
        }

        void NotifyDataDependencyRemoved(DataNodeId child, DataNodeId parent)
        {
            for (int i = m_DataNodeObservers.Count - 1; i >= 0; i--)
                m_DataNodeObservers[i].OnDataDependencyRemoved(child, parent);
        }

        void NotifyGraphCleared()
        {
            for (int i = m_LifecycleObservers.Count - 1; i >= 0; i--)
                m_LifecycleObservers[i].OnGraphCleared();
        }

        public bool IsValid(DataViewId dataViewId) => m_DataViews.IsAlive(dataViewId);
        public bool IsValid(TaskNodeId taskNodeId) => m_TaskNodes.IsAlive(taskNodeId);
        public bool IsValid(DataNodeId dataNodeId) => m_DataNodes.IsAlive(dataNodeId);
        public bool IsValid(DataBindingId dataBindingId) => m_DataBindings.IsAlive(dataBindingId);
        public bool IsValid(DataContainerId dataContainerId) => m_DataContainers.IsAlive(dataContainerId);

        DataViewId FindRootDataViewId(DataViewId dataViewId)
        {
            var parentDataView = m_DataViews[dataViewId];
            while (parentDataView.ParentDataViewId.IsValid)
            {
                parentDataView = m_DataViews[parentDataView.ParentDataViewId];
            }
            return parentDataView.Id;
        }
    }
}
