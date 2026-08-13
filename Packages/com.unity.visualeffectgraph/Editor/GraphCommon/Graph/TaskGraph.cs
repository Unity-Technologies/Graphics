using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a graph of tasks and data nodes with their dependencies.
    /// </summary>
    sealed partial class TaskGraph : IMutableGraph
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

        // Observers receive a fine-grained signal for every primitive mutation. They are
        // registered at construction time in topological order — a dependency (e.g. DataViewTrees)
        // before any cache that reads it — and never unregistered. Each list is iterated
        // synchronously from the matching mutating API method: Added/Changed dispatch in
        // registration order, so a dependency updates before its readers; Removed/Cleared dispatch
        // in reverse, so a reader is notified while the caches it reads are still intact, before
        // they tear down.
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

        /// <inheritdoc cref="IGraph{T}"/>
        public ITaskNodeProvider TaskNodes => m_TaskNodeProvider;
        /// <inheritdoc cref="IGraph{T}"/>
        public IDataNodeProvider DataNodes => m_DataNodeProvider;
        /// <inheritdoc cref="IGraph{T}"/>
        public IDataViewProvider DataViews => m_DataViewProvider;
        /// <inheritdoc cref="IGraph{T}"/>
        public IDataBindingProvider DataBindings => m_DataBindingProvider;
        /// <inheritdoc cref="IGraph{T}"/>
        public IDataContainerProvider DataContainers => m_DataContainerProvider;

        TaskNodeGraphCache TaskNodeGraph { get; }
        DataNodeGraphCache DataNodeGraph { get; }
        DataViewTreesCache DataViewTrees { get; }
        TaskNodeToDataNodesCache TaskNodeToDataNodes { get; }
        TaskNodeToDataBindingsCache TaskNodeToDataBindings { get; }
        DataNodeToDataViewsCache DataNodeToDataViews { get; }
        DataNodeToDataBindingsCache DataNodeToDataBindings { get; }
        DataViewToDataContainerCache DataViewToDataContainer { get; }
        DataViewsWriteCacheInfoCache DataViewsWriteCacheInfo { get; }

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
            DataViewsWriteCacheInfo    = RegisterCache(new DataViewsWriteCacheInfoCache(m_DataViews, this),   source?.DataViewsWriteCacheInfo,   copyCache);
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

        void RegisterObserver(object observer)
        {
            if (observer is IGraphLifecycleObserver lifecycle) m_LifecycleObservers.Add(lifecycle);
            if (observer is ITaskObserver task)                m_TaskObservers.Add(task);
            if (observer is IDataNodeObserver dataNode)        m_DataNodeObservers.Add(dataNode);
            if (observer is IDataBindingObserver dataBinding)  m_DataBindingObservers.Add(dataBinding);
            if (observer is IDataViewObserver dataView)        m_DataViewObservers.Add(dataView);
            if (observer is IDataContainerObserver container)  m_DataContainerObservers.Add(container);
        }

        /// <summary>
        /// Clears all nodes and dependencies from the graph.
        /// </summary>
        public void Clear()
        {
            m_TaskNodes.Clear();
            m_DataNodes.Clear();
            m_DataContainers.Clear();
            m_DataViews.Clear();

            m_DataDependencies.Clear();

            Version = 1u;
            NotifyGraphCleared();
        }

        /// <summary>
        /// Adds a task to the graph.
        /// </summary>
        /// <param name="task">The task to add.</param>
        /// <returns>The ID of the newly created task node.</returns>
        public TaskNodeId AddTask(ITask task, string name = null)
        {
            Version++;
            m_TaskNodes.Allocate(out var taskNodeId) = new TaskNodeInfo(taskNodeId, task, name);
            NotifyTaskNodeAdded(taskNodeId, task, name);
            return taskNodeId;
        }

        /// <inheritdoc cref="IBuildableGraph"/>
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
        /// <inheritdoc cref="IMutableGraph"/>
        public void OverrideDataDescription(DataViewId dataViewId, IDataDescription dataDescription)
        {
            DataViewInfo dataViewInfo = m_DataViews[dataViewId];
            Debug.Assert(dataViewInfo.DataDescription.IsCompatible(dataDescription));
            m_DataViews[dataViewId] = new DataViewInfo(dataViewInfo.Id, dataDescription, dataViewInfo.ParentDataViewId, dataViewInfo.SubDataKey );
        }

        /// <inheritdoc cref="IBuildableGraph"/>
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

        /// <inheritdoc cref="IBuildableGraph"/>
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

        void ResolveBindingUsage(TaskNodeId taskNodeId, IDataKey bindingKey, ref BindingUsage usage,
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

        /// <inheritdoc cref="IBuildableGraph"/>
        public void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage = BindingUsage.Unknown)
        {
            ResolveBindingUsage(taskNodeId, bindingKey, ref usage, out var readPaths, out var writePaths);

            IEnumerable<DataNodeId> parentNodeIds = Array.Empty<DataNodeId>();
            if (usage.HasFlag(BindingUsage.Read))
            {
                parentNodeIds = FindImplicitParentDataNodes(dataViewId);
            }

            BindData(taskNodeId, bindingKey, dataViewId, usage, parentNodeIds, readPaths, writePaths);
        }

        /// <inheritdoc cref="IMutableGraph"/>
        public void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId,
            IEnumerable<DataNodeId> parentNodeIds, BindingUsage usage = BindingUsage.Unknown)
        {
            ResolveBindingUsage(taskNodeId, bindingKey, ref usage, out var readPaths, out var writePaths);
            BindData(taskNodeId, bindingKey, dataViewId, usage, parentNodeIds, readPaths, writePaths);
        }

        void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage,
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
            NotifyDataBindingAdded(dataBindingId, dataViewId, dataNodeId, bindingKey, usage);

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
            bool added = m_DataDependencies.Add(new DataDependency(dataNodeId, parentDataNodeId));
            if (added)
            {
                Version++;
                NotifyDataDependencyAdded(dataNodeId, parentDataNodeId);
            }

            return added;
        }

        /// <inheritdoc cref="IReadOnlyGraph"/>
        public IMutableGraph Copy()
        {
            return new TaskGraph(this);
        }

        /// <inheritdoc cref="IBuildableGraph"/>
        public IReadOnlyGraph EndBuilding()
        {
            return this;
        }

        /// <inheritdoc cref="IReadOnlyGraph"/>
        DataNodeEnumerable IReadOnlyGraph.GetDataNodes(TaskNodeId taskNodeId)
        {
            var container = TaskNodeToDataNodes.Data;
            SubEnumerable<DataNodeId> subEnumerable = new(container, taskNodeId.Index, container[taskNodeId.Index].Count);
            return new(DataNodes, subEnumerable);
        }

        /// <inheritdoc cref="IReadOnlyGraph"/>
        DataBindingEnumerable IReadOnlyGraph.GetDataBindings(TaskNodeId taskNodeId)
        {
            var container = TaskNodeToDataBindings.Data;
            SubEnumerable<DataBindingId> subEnumerable = new(container, taskNodeId.Index, container[taskNodeId.Index].Count);
            return new(DataBindings, subEnumerable);
        }

        /// <inheritdoc cref="IReadOnlyGraph"/>
        DataBindingEnumerable IReadOnlyGraph.GetDataBindings(DataNodeId dataNodeId)
        {
            var container = DataNodeToDataBindings.Data;
            SubEnumerable<DataBindingId> subEnumerable = new(container, dataNodeId.Index, container[dataNodeId.Index].Count);
            return new(DataBindings, subEnumerable);
        }


        /// <inheritdoc cref="IReadOnlyGraph.GetUsedDataViews"/>
        DataView IReadOnlyGraph.GetUsedDataViews(DataNodeId dataNodeId)
        {
            var treeNode = DataNodeToDataViews.Data.RootNodes[dataNodeId.Index];
            return treeNode.Data.IsValid ? new(m_DataViewProvider, treeNode, this, m_DataViews[treeNode.Data]) : new();
        }

        /// <inheritdoc cref="IReadOnlyGraph.IsRead"/>
        bool IReadOnlyGraph.IsRead(DataNodeId dataNodeId, DataViewId dataViewId) => DataNodeToDataViews.IsRead(dataNodeId, dataViewId);

        /// <inheritdoc cref="IReadOnlyGraph.IsWritten"/>
        bool IReadOnlyGraph.IsWritten(DataNodeId dataNodeId, DataViewId dataViewId) => DataNodeToDataViews.IsWritten(dataNodeId, dataViewId);

        /// <inheritdoc cref="IReadOnlyGraph.GetDataContainer"/>
        DataContainer IReadOnlyGraph.GetDataContainer(DataViewId dataViewId)
        {
            var dataContainerId = DataViewToDataContainer.Data[dataViewId.Index];
            return DataContainers[dataContainerId];
        }

        /// <inheritdoc cref="IReadOnlyGraph.TryGetSubView"/>
        bool IReadOnlyGraph.TryGetSubView(DataViewId parentDataViewId, IDataKey subDataKey, out DataViewId dataViewId)
            => DataViewTrees.TryGetSubView(parentDataViewId, subDataKey, out dataViewId);

        ReadOnlyList<DataNodeId> FindImplicitParentDataNodes(DataViewId dataViewId)
        {
            var result = new List<DataNodeId>();

            // Find the most recent data nodes in parents
            DataNodeId parentDataNodeId = DataNodeId.Invalid;
            DataViewId parentDataViewId = dataViewId;
            while (parentDataViewId.IsValid)
            {
                DataNodeId dataNodeId = DataViewsWriteCacheInfo.Data[parentDataViewId].LastWrite;
                if (dataNodeId.IsValid && (!parentDataNodeId.IsValid || parentDataNodeId.Index < dataNodeId.Index))
                {
                    parentDataNodeId = dataNodeId;
                }
                parentDataViewId = m_DataViews[parentDataViewId].ParentDataViewId;
            }

            if (parentDataNodeId.IsValid)
            {
                result.Add(parentDataNodeId);
                AppendSubdataParentDataNodes(dataViewId, parentDataNodeId, result);
            }
            // TODO: Look to siblings if there is data overlap
            return result;
        }

        void AppendSubdataParentDataNodes(DataViewId dataViewId, DataNodeId parentDataNodeId, List<DataNodeId> result)
        {
            foreach (var subdataViewId in DataViewsWriteCacheInfo.Data[dataViewId].SubWrites)
            {
                DataNodeId dataNodeId = DataViewsWriteCacheInfo.Data[subdataViewId].LastWrite;
                if (parentDataNodeId.Index < dataNodeId.Index)
                {
                    result.Add(dataNodeId);
                    AppendSubdataParentDataNodes(subdataViewId, dataNodeId, result);
                }
            }
        }

        GraphTraverser IReadOnlyGraph.CreateTraverser() => new GraphTraverser(this);

        /// <inheritdoc cref="IMutableGraph"/>
        public void RemoveTask(TaskNodeId taskNodeId)
        {
            throw new NotImplementedException("RemoveTask is not yet implemented");
        }

        /// <inheritdoc cref="IMutableGraph"/>
        public void RemoveDataContainer(DataContainerId dataContainerId)
        {
            throw new NotImplementedException("RemoveDataContainer is not yet implemented");
        }

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

        void NotifyDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId, IDataKey bindingKey, BindingUsage usage)
        {
            for (int i = 0; i < m_DataBindingObservers.Count; i++)
                m_DataBindingObservers[i].OnDataBindingAdded(id, dataViewId, dataNodeId, bindingKey, usage);
        }

        void NotifyDataBindingRemoved(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId, BindingUsage usage)
        {
            for (int i = m_DataBindingObservers.Count - 1; i >= 0; i--)
                m_DataBindingObservers[i].OnDataBindingRemoved(id, dataViewId, dataNodeId, usage);
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
