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
        readonly GraphDataList<TaskNodeInfo> m_TaskNodes = new();
        readonly GraphDataList<DataNodeInfo> m_DataNodes = new();
        readonly GraphDataList<DataContainerInfo> m_DataContainers = new();
        readonly GraphDataList<DataViewInfo> m_DataViews = new();
        readonly GraphDataList<DataBindingInfo> m_DataBindings = new();

        readonly DelegateGraphCacheData<SubordinateList<TaskNodeCacheInfo>> m_TaskNodesCacheInfo;
        readonly DelegateGraphCacheData<SubordinateList<DataNodeCacheInfo>> m_DataNodesCacheInfo;
        readonly DelegateGraphCacheData<SubordinateList<DataViewCacheInfo>> m_DataViewsCacheInfo;
        readonly DelegateGraphCacheData<SubordinateList<DataViewWriteCacheInfo>> m_DataViewsWriteCacheInfo;

        readonly HashSet<TaskDependency> m_TaskDependencies = new();
        readonly HashSet<DataDependency> m_DataDependencies = new();

        readonly Dictionary<(DataViewId, IDataKey), DataViewId> m_DataViewIdDictionary = new();
        readonly Dictionary<(TaskNodeId, DataContainerId), DataNodeId> m_DataNodeIdDictionary = new();

        readonly TaskNodeProvider m_TaskNodeProvider;
        readonly DataNodeProvider m_DataNodeProvider;
        readonly DataViewProvider m_DataViewProvider;
        readonly DataBindingProvider m_DataBindingProvider;
        readonly DataContainerProvider m_DataContainerProvider;

        /// <summary>
        /// Gets the current version of the graph. Increases whenever the graph is modified.
        /// </summary>
        public uint Version { get; private set; } = 1u; // 0u reserved for null/invalid version

        /// <inheritdoc cref="IGraph{T}"/>
        public ITaskNodeProvider TaskNodes => m_TaskNodeProvider.Refresh();

        /// <inheritdoc cref="IGraph{T}"/>
        public IDataNodeProvider DataNodes => m_DataNodeProvider.Refresh();

        /// <inheritdoc cref="IGraph{T}"/>
        public IDataViewProvider DataViews => m_DataViewProvider.Refresh();

        /// <inheritdoc cref="IGraph{T}"/>
        public IDataBindingProvider DataBindings => m_DataBindingProvider.Refresh();

        /// <inheritdoc cref="IGraph{T}"/>
        public IDataContainerProvider DataContainers => m_DataContainerProvider.Refresh();

        DelegateGraphCacheData<LinearGraph<TaskNodeId>> TaskNodeGraph { get; }
        DelegateGraphCacheData<LinearGraph<DataNodeId>> DataNodeGraph { get; }
        DelegateGraphCacheData<LinearMultiTree<DataViewId>> DataViewTrees { get; }
        DelegateGraphCacheData<LinearMultiList<DataNodeId>> TaskNodeToDataNodes { get; }
        DelegateGraphCacheData<LinearMultiList<DataBindingId>> TaskNodeToDataBindings { get; }
        DelegateGraphCacheData<LinearMultiList<DataContainerId>> TaskNodeToDataContainers { get; }
        DelegateGraphCacheData<LinearMultiTree<DataViewId>> DataNodeToDataViews { get; }
        DelegateGraphCacheData<LinearMultiTree<DataViewId>> DataNodeToReadDataViews { get; }
        DelegateGraphCacheData<LinearMultiTree<DataViewId>> DataNodeToWrittenDataViews { get; }
        DelegateGraphCacheData<LinearMultiList<DataBindingId>> DataNodeToDataBindings { get; }
        DelegateGraphCacheData<LinearMultiList<DataNodeId>> DataViewToDataNodes { get; }
        DelegateGraphCacheData<List<DataContainerId>> DataViewToDataContainer { get; }
        DelegateGraphCacheData<List<DataNodeId>> DataBindingToDataNodes { get; }
        DelegateGraphCacheData<LinearMultiList<DataViewId>> DataContainerToDataViews { get; }
        DelegateGraphCacheData<LinearMultiList<TaskNodeId>> DataContainerToTaskNodes { get; }

        DelegateGraphCacheData<SubordinateList<TaskNodeCacheInfo>> TaskNodesCacheInfo => m_TaskNodesCacheInfo.Refresh();

        DelegateGraphCacheData<SubordinateList<DataNodeCacheInfo>> DataNodesCacheInfo => m_DataNodesCacheInfo.Refresh();

        DelegateGraphCacheData<SubordinateList<DataViewCacheInfo>> DataViewsCacheInfo => m_DataViewsCacheInfo.Refresh();

        DelegateGraphCacheData<SubordinateList<DataViewWriteCacheInfo>> DataViewsWriteCacheInfo => m_DataViewsWriteCacheInfo.Refresh();

        /// <summary>
        /// Creates a new instance of the <see cref="TaskGraph"/>.
        /// </summary>
        public TaskGraph()
        {
            m_TaskNodeProvider = new TaskNodeProvider(this);
            m_DataNodeProvider = new DataNodeProvider(this);
            m_DataViewProvider = new DataViewProvider(this);
            m_DataBindingProvider = new DataBindingProvider(this);
            m_DataContainerProvider = new DataContainerProvider(this);

            TaskNodeGraph = new(this, graph => (graph as TaskGraph).BuildTaskNodeGraph());
            DataNodeGraph = new(this, graph => (graph as TaskGraph).BuildDataNodeGraph());
            DataViewTrees = new(this, graph => (graph as TaskGraph).BuildDataViewTrees());
            TaskNodeToDataNodes = new(this, graph => (graph as TaskGraph).BuildTaskNodeToDataNodes());
            TaskNodeToDataBindings = new(this, graph => (graph as TaskGraph).BuildTaskNodeToDataBindings());
            TaskNodeToDataContainers = new(this, graph => (graph as TaskGraph).BuildTaskNodeToDataContainers());
            DataNodeToDataViews = new(this, graph => (graph as TaskGraph).BuildDataNodeToUsedDataViews());
            DataNodeToReadDataViews = new(this, graph => (graph as TaskGraph).BuildDataNodeToReadDataViews());
            DataNodeToWrittenDataViews = new(this, graph => (graph as TaskGraph).BuildDataNodeToWrittenDataViews());
            DataNodeToDataBindings = new(this, graph => (graph as TaskGraph).BuildDataNodeToDataBindings());
            DataViewToDataNodes = new(this, graph => (graph as TaskGraph).BuildDataViewToDataNodes());
            DataViewToDataContainer = new(this, graph => (graph as TaskGraph).BuildDataViewToDataContainers());
            DataBindingToDataNodes = new(this, graph => (graph as TaskGraph).BuildDataBindingToDataNodes());
            DataContainerToDataViews = new(this, graph => (graph as TaskGraph).BuildDataContainerToDataViews());
            DataContainerToTaskNodes = new(this, graph => (graph as TaskGraph).BuildDataContainerToTaskNodes());
            m_DataNodesCacheInfo = new(this, graph => (graph as TaskGraph).BuildDataNodesCacheInfo());
            m_TaskNodesCacheInfo = new(this, graph => (graph as TaskGraph).BuildTaskNodesCacheInfo());
            m_DataViewsCacheInfo = new(this, graph => (graph as TaskGraph).BuildDataViewsCacheInfo());
            m_DataViewsWriteCacheInfo = new(this, graph => (graph as TaskGraph).BuildDataViewsWriteCacheInfo());
        }

        private TaskGraph(TaskGraph taskGraph, bool copyCache = true) : this()
        {
            m_TaskNodes = new(taskGraph.m_TaskNodes, copyCache);
            m_DataNodes = new(taskGraph.m_DataNodes, copyCache);
            m_DataContainers = new(taskGraph.m_DataContainers);
            m_DataViews = new(taskGraph.m_DataViews, copyCache);
            m_DataBindings = new(taskGraph.m_DataBindings);

            m_TaskDependencies = new(taskGraph.m_TaskDependencies);
            m_DataDependencies = new(taskGraph.m_DataDependencies);

            // Cache, can be rebuilt any time
            m_DataViewIdDictionary = new(taskGraph.m_DataViewIdDictionary);
            m_DataNodeIdDictionary = new(taskGraph.m_DataNodeIdDictionary);
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

            m_TaskDependencies.Clear();
            m_DataDependencies.Clear();

            m_DataViewIdDictionary.Clear();

            Version = 1u;
        }

        /// <summary>
        /// Adds a task to the graph.
        /// </summary>
        /// <param name="task">The task to add.</param>
        /// <returns>The ID of the newly created task node.</returns>
        public TaskNodeId AddTask(ITask task)
        {
            Version++;
            m_TaskNodes.Allocate(out var taskNodeId) = new TaskNodeInfo(taskNodeId, task);
            return taskNodeId;
        }

        /// <inheritdoc cref="IBuildableGraph"/>
        public DataViewId AddData(string name, IDataDescription dataDescription)
        {
            Version++;
            DataViewsCacheInfo.Refresh();
            DataViewsWriteCacheInfo.Refresh();

            m_DataViews.Allocate(out var dataViewId) = new DataViewInfo(dataViewId, dataDescription);
            m_DataContainers.Allocate(out var dataContainerId) = new DataContainerInfo(dataContainerId, name, dataViewId);

            DataViewsCacheInfo.Data[dataViewId].DataContainerId = dataContainerId;
            DataViewsWriteCacheInfo.Data[dataViewId].LastWrite = DataNodeId.Invalid;

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
            if (!m_DataViewIdDictionary.TryGetValue((parentDataViewId, subDataKey), out DataViewId dataViewId))
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
                    m_DataViews.Allocate(out var newDataViewId) = new DataViewInfo(newDataViewId, dataDescription, parentDataViewId, subDataKey);
                    dataViewId = newDataViewId;
                    DataViewsCacheInfo.Data[dataViewId].DataContainerId = DataViewsCacheInfo.Data[parentDataViewId].DataContainerId;
                    DataViewsCacheInfo.Data[parentDataViewId].ChildCount++;

                    m_DataViewIdDictionary.Add((parentDataViewId, subDataKey), dataViewId);
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
            foreach (var subDataKey in subdataPath.PathSequence)
            {
                currentDataViewId = subDataKey != null ? GetSubdata(currentDataViewId, subDataKey) : currentDataViewId;
                //currentDataViewId = GetSubdata(currentDataViewId, subDataKey);
                if (!currentDataViewId.IsValid)
                    break;
            }
            return currentDataViewId;
        }

        /// <inheritdoc cref="IBuildableGraph"/>
        public void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage = BindingUsage.Unknown)
        {
            Debug.Assert(DataViewsCacheInfo.Valid);

            IEnumerable<DataNodeId> parentNodeIds = Array.Empty<DataNodeId>();
            if (usage.HasFlag(BindingUsage.Read))
            {
                parentNodeIds = FindImplicitParentDataNodes(dataViewId);
            }

            BindData(taskNodeId, bindingKey, dataViewId, usage, parentNodeIds);
        }

        /// <inheritdoc cref="IMutableGraph"/>
        public void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage,
            IEnumerable<DataNodeId> parentNodeIds)
        {
            Version++;

            if (!m_TaskNodes[taskNodeId].Task.GetBindingUsage(bindingKey, out var usageFromTask))
            {
                Debug.LogWarning("The task is not using this data. Skipping binding data.");
                return;
            }
            if (usage == BindingUsage.Unknown)
            {
                usage = usageFromTask;
            }
            else if (usageFromTask != BindingUsage.Unknown && usage != usageFromTask)
            {
                Debug.LogWarning($"Provided binding usage {usage} doesn't match binding usage {usageFromTask} from task node");
                return;
            }
            if (usage == BindingUsage.Unknown)
            {
                Debug.LogWarning("Binding usage cannot be determined. Skipping binding data.");
                return;
            }

            Debug.Assert(usage != BindingUsage.Unknown);

            m_DataBindings.Allocate(out var dataBindingId) =
                new DataBindingInfo(dataBindingId, taskNodeId, dataViewId, bindingKey, usage);

            var dataContainerId = DataViewsCacheInfo.Data[dataViewId].DataContainerId;
            if (!m_DataNodeIdDictionary.TryGetValue((taskNodeId, dataContainerId), out var dataNodeId))
            {
                m_DataNodes.Allocate(out var newDataNodeId) = new DataNodeInfo(newDataNodeId, taskNodeId, dataContainerId);
                dataNodeId = newDataNodeId;
                m_DataNodeIdDictionary.Add((taskNodeId, dataContainerId), dataNodeId);
            }

            foreach (var parentNodeId in parentNodeIds)
            {
                AddDataDependency(dataNodeId, parentNodeId);
            }

            var task = m_TaskNodes[taskNodeId].Task;
            bool usesData = task.GetDataUsage(bindingKey, out DataPathSet readUsage, out DataPathSet writeUsage);
            if (usesData)
            {
                foreach (var path in readUsage.DataPaths)
                {
                    var currentDataViewId = dataViewId;
                    foreach (var key in path.PathSequence)
                    {
                        currentDataViewId = key != null ? GetSubdata(currentDataViewId, key) : currentDataViewId;
                        if (!currentDataViewId.IsValid)
                            break;
                    }
                }

                foreach (var path in writeUsage.DataPaths)
                {
                    var currentDataViewId = dataViewId;
                    foreach (var key in path.PathSequence)
                    {
                        currentDataViewId = key != null ? GetSubdata(currentDataViewId, key) : currentDataViewId;
                        if (!currentDataViewId.IsValid)
                            break;
                    }
                }
            }

            //if (TaskNodesCacheInfo.Valid)
            {
                TaskNodesCacheInfo.Data[taskNodeId].BindingCount++;
            }

            if (/*DataViewsWriteCacheInfo.Valid && */usage.HasFlag(BindingUsage.Write))
            {
                ref DataViewWriteCacheInfo cacheInfo = ref DataViewsWriteCacheInfo.Data[dataViewId];
                cacheInfo.LastWrite = dataNodeId;
                cacheInfo.SubWrites.Clear();

                // Propagate subwrites up
                var subDataViewId = dataViewId;
                var parentDataViewId = m_DataViews[subDataViewId].ParentDataViewId;
                while (parentDataViewId.IsValid)
                {
                    ref DataViewWriteCacheInfo parentCacheInfo = ref DataViewsWriteCacheInfo.Data[parentDataViewId];
                    parentCacheInfo.SubWrites.Add(subDataViewId);
                    subDataViewId = parentDataViewId;
                    parentDataViewId = m_DataViews[subDataViewId].ParentDataViewId;
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

                //if (DataNodesCacheInfo.Valid)
                {
                    DataNodesCacheInfo.Data[dataNodeId].ParentCount++;
                    DataNodesCacheInfo.Data[parentDataNodeId].ChildCount++;
                }

                bool generateTaskDependencies = true;
                if (generateTaskDependencies)
                {
                    AddTaskDependency(m_DataNodes[dataNodeId].TaskNodeId, m_DataNodes[parentDataNodeId].TaskNodeId);
                }
            }

            return added;
        }

        /// <summary>
        /// Adds a dependency relationship between two task nodes.
        /// </summary>
        /// <param name="taskNodeId">The ID of the dependent task node.</param>
        /// <param name="parentTaskNodeId">The ID of the parent task node.</param>
        /// <returns>True if the dependency was added, false if it already existed.</returns>
        public bool AddTaskDependency(TaskNodeId taskNodeId, TaskNodeId parentTaskNodeId)
        {
            Debug.Assert(taskNodeId.IsValid);
            Debug.Assert(parentTaskNodeId.IsValid);
            bool added = m_TaskDependencies.Add(new TaskDependency(taskNodeId, parentTaskNodeId));
            if (added)
            {
                if (TaskNodesCacheInfo.Valid)
                {
                    TaskNodesCacheInfo.Data[taskNodeId].ParentCount++;
                    TaskNodesCacheInfo.Data[parentTaskNodeId].ChildCount++;
                }
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
        public DataNodeEnumerable<SubEnumerable<DataNodeId>> GetDataNodes(TaskNodeId taskNodeId)
        {
            var container = TaskNodeToDataNodes.Data;
            SubEnumerable<DataNodeId> subEnumerable = new(container, taskNodeId.Index, container[taskNodeId.Index].Count);
            return new(DataNodes, subEnumerable);
        }

        /// <inheritdoc cref="IReadOnlyGraph"/>
        public DataNode GetDataNode(DataBindingId dataBindingId)
        {
            var dataNodeId = DataBindingToDataNodes.Data[dataBindingId.Index];
            return DataNodes[dataNodeId];
        }

        /// <inheritdoc cref="IReadOnlyGraph"/>
        public DataBindingEnumerable<SubEnumerable<DataBindingId>> GetDataBindings(TaskNodeId taskNodeId)
        {
            var container = TaskNodeToDataBindings.Data;
            SubEnumerable<DataBindingId> subEnumerable = new(container, taskNodeId.Index, container[taskNodeId.Index].Count);
            return new(DataBindings, subEnumerable);
        }

        /// <inheritdoc cref="IReadOnlyGraph.GetUsedDataViews"/>
        public DataView GetUsedDataViews(DataNodeId dataNodeId)
        {
            var treeNode = DataNodeToDataViews.Data[dataNodeId.Index];
            return treeNode.Data.IsValid ? new(m_DataViewProvider, treeNode, this, m_DataViews[treeNode.Data]) : new();
        }

        /// <inheritdoc cref="IReadOnlyGraph.GetReadDataViews"/>
        public DataView GetReadDataViews(DataNodeId dataNodeId)
        {
            var treeNode = DataNodeToReadDataViews.Data[dataNodeId.Index];
            return treeNode.Data.IsValid ? new(m_DataViewProvider, treeNode, this, m_DataViews[treeNode.Data]) : new();
        }

        /// <inheritdoc cref="IReadOnlyGraph.GetWrittenDataViews"/>
        public DataView GetWrittenDataViews(DataNodeId dataNodeId)
        {
            var treeNode = DataNodeToWrittenDataViews.Data[dataNodeId.Index];
            return treeNode.Data.IsValid ? new(m_DataViewProvider, treeNode, this, m_DataViews[treeNode.Data]) : new();
        }

        /// <inheritdoc cref="IReadOnlyGraph.GetDataContainer"/>
        public DataContainer GetDataContainer(DataViewId dataViewId)
        {
            var dataContainerId = DataViewToDataContainer.Data[dataViewId.Index];
            return DataContainers[dataContainerId];
        }

        IEnumerable<DataNodeId> FindImplicitParentDataNodes(DataViewId dataViewId)
        {
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
                yield return parentDataNodeId;

                // Find more recent data nodes in subdata
                foreach (DataNodeId subdataNodeId in FindSubdataParentDataNodes(dataViewId, parentDataNodeId))
                {
                    yield return subdataNodeId;
                }
            }

            // TODO: Look to siblings if there is data overlap
        }

        IEnumerable<DataNodeId> FindSubdataParentDataNodes(DataViewId dataViewId, DataNodeId parentDataNodeId)
        {
            foreach (var subdataViewId in DataViewsWriteCacheInfo.Data[dataViewId].SubWrites)
            {
                DataNodeId dataNodeId = DataViewsWriteCacheInfo.Data[subdataViewId].LastWrite;
                if (parentDataNodeId.Index < dataNodeId.Index)
                {
                    yield return dataNodeId;
                    foreach (DataNodeId subdataNodeId in FindSubdataParentDataNodes(subdataViewId, dataNodeId))
                    {
                        yield return subdataNodeId;
                    }
                }
            }
        }

        GraphTraverser IReadOnlyGraph.CreateTraverser() => new GraphTraverser(this);

        IMutableGraph IReadOnlyGraph.EmptyCopy()
        {
            throw new NotImplementedException();
        }

        void IMutableGraph.SetData(DataNodeId id, DataContainerId dataId)
        {
            Version++;

            throw new NotImplementedException();
        }

        void IMutableGraph.SetTask(TaskNodeId id, ITask task)
        {
            m_TaskNodes[id] = new TaskNodeInfo(id, task);
        }


        struct TaskNodeCacheInfo
        {
            public int BindingCount { get; set; }
            public int ParentCount { get; set; }
            public int ChildCount { get; set; }
        }

        struct DataNodeCacheInfo
        {
            public int ParentCount { get; set; }
            public int ChildCount { get; set; }
        }

        struct DataViewCacheInfo
        {
            public DataContainerId DataContainerId { get; set; }
            public int ChildCount { get; set; }
        }
        struct DataViewWriteCacheInfo
        {
            public DataNodeId LastWrite { get; set; }

            HashSet<DataViewId> m_Subwrites;
            public HashSet<DataViewId> SubWrites
            {
                get
                {
                    if (m_Subwrites == null)
                    {
                        m_Subwrites = new();
                    }
                    return m_Subwrites;
                }
            }
        }

        class TaskNodeProvider : ITaskNodeProvider, IIndexable<GraphNode<TaskNodeId>, TaskNode>
        {
            Handle<TaskGraph> m_Owner;

            /// <inheritdoc cref="ITaskNodeProvider"/>
            public bool Valid => m_Owner.Valid;

            /// <summary>
            /// Gets the number of task nodes in the graph.
            /// </summary>
            public int Count => m_Owner.Ref.m_TaskNodes.Count;

            /// <summary>
            /// Gets the task node at the specified index in the graph.
            /// </summary>
            /// <param name="index">The zero-based index of the task node to get.</param>
            /// <returns>The task node at the specified index.</returns>
            public TaskNode this[int index] => this[m_Owner.Ref.TaskNodeGraph.Data[index]];

            /// <summary>
            /// Gets the task node from the specified ID.
            /// </summary>
            /// <param name="taskNodeId">The ID of the task node to get.</param>
            /// <returns>The task node with the specified ID.</returns>
            public TaskNode this[TaskNodeId taskNodeId] => this[m_Owner.Ref.TaskNodeGraph.Data[taskNodeId.Index]];

            /// <summary>
            /// Gets the data node from a graph node.
            /// </summary>
            /// <param name="node">The graph node to convert.</param>
            /// <returns>A task node representing the graph node.</returns>
            public TaskNode this[GraphNode<TaskNodeId> node] => new(this, node, m_Owner.Ref, m_Owner.Ref.m_TaskNodes[node.Data]);

            /// <summary>
            /// Initializes a new instance of the <see cref="TaskNodeProvider"/> class.
            /// </summary>
            /// <param name="owner">The task graph that owns this provider.</param>
            public TaskNodeProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            /// <inheritdoc cref="ITaskNodeProvider"/>
            public LinearEnumerator<ITaskNodeProvider, TaskNode> GetEnumerator() => new(this);

            /// <summary>
            /// Refreshes the provider to ensure it has the latest data from the graph.
            /// </summary>
            /// <returns>This provider instance.</returns>
            public TaskNodeProvider Refresh()
            {
                m_Owner.Update();
                return this;
            }
        }

        class DataNodeProvider : IDataNodeProvider, IIndexable<GraphNode<DataNodeId>, DataNode>
        {
            Handle<TaskGraph> m_Owner;

            /// <inheritdoc cref="IDataNodeProvider"/>
            public bool Valid => m_Owner.Valid;

            /// <summary>
            /// Gets the number of data nodes in the graph.
            /// </summary>
            public int Count => m_Owner.Ref.m_DataNodes.Count;

            /// <summary>
            /// Gets the data node at the specified index in the graph.
            /// </summary>
            /// <param name="index">The zero-based index of the data node to get.</param>
            /// <returns>The data node at the specified index.</returns>
            public DataNode this[int index] => this[m_Owner.Ref.DataNodeGraph.Data[index]];
            /// <summary>
            /// Gets the data node from the specified ID.
            /// </summary>
            /// <param name="dataNodeId">The ID of the data node to get.</param>
            /// <returns>The data node with the specified ID.</returns>
            public DataNode this[DataNodeId DataNodeId] => this[m_Owner.Ref.DataNodeGraph.Data[DataNodeId.Index]];
            /// <summary>
            /// Gets the data node from a graph node.
            /// </summary>
            /// <param name="node">The graph node to convert.</param>
            /// <returns>A data node representing the graph node.</returns>
            public DataNode this[GraphNode<DataNodeId> node] => new(this, node, m_Owner.Ref, m_Owner.Ref.m_DataNodes[node.Data]);

            /// <summary>
            /// Initializes a new instance of the <see cref="DataNodeProvider"/> class.
            /// </summary>
            /// <param name="owner">The data graph that owns this provider.</param>
            public DataNodeProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            /// <inheritdoc cref="IDataNodeProvider"/>
            public LinearEnumerator<IDataNodeProvider, DataNode> GetEnumerator() => new(this);

            /// <summary>
            /// Refreshes the provider to ensure it has the latest data from the graph.
            /// </summary>
            /// <returns>This provider instance.</returns>
            public DataNodeProvider Refresh()
            {
                m_Owner.Update();
                return this;
            }
        }

        class DataViewProvider : IDataViewProvider, IIndexable<MultiTreeNode<DataViewId>, DataView>
        {
            Handle<TaskGraph> m_Owner;

            public bool Valid => m_Owner.Valid;

            public int Count => m_Owner.Ref.m_DataViews.Count;

            public DataView this[int index] => this[new DataViewId(index)];
            public DataView this[DataViewId dataViewId] => new(this, m_Owner.Ref.DataViewTrees.Data[dataViewId.Index], m_Owner.Ref, m_Owner.Ref.m_DataViews[dataViewId]);
            public DataView this[MultiTreeNode<DataViewId> node] => new(this, node, m_Owner.Ref, m_Owner.Ref.m_DataViews[node.Data]);

            public DataViewProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public LinearEnumerator<IDataViewProvider, DataView> GetEnumerator() => new(this);

            public DataViewProvider Refresh()
            {
                m_Owner.Update();
                return this;
            }
        }

        class DataBindingProvider : IDataBindingProvider
        {
            Handle<TaskGraph> m_Owner;

            public bool Valid => m_Owner.Valid;

            public int Count => m_Owner.Ref.m_DataBindings.Count;

            public DataBinding this[int index] => this[new DataBindingId(index)];
            public DataBinding this[DataBindingId dataBindingId] => new(this, m_Owner.Ref, m_Owner.Ref.m_DataBindings[dataBindingId]);

            public DataBindingProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public LinearEnumerator<IDataBindingProvider, DataBinding> GetEnumerator() => new(this);

            public DataBindingProvider Refresh()
            {
                m_Owner.Update();
                return this;
            }
        }

        class DataContainerProvider : IDataContainerProvider
        {
            Handle<TaskGraph> m_Owner;

            public bool Valid => m_Owner.Valid;

            public int Count => m_Owner.Ref.m_DataContainers.Count;

            public DataContainer this[int index] => this[new DataContainerId(index)];
            public DataContainer this[DataContainerId dataContainerId] => new(m_Owner.Ref, m_Owner.Ref.m_DataContainers[dataContainerId]);

            public DataContainerProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public LinearEnumerator<IDataContainerProvider, DataContainer> GetEnumerator() => new(this);

            public DataContainerProvider Refresh()
            {
                m_Owner.Update();
                return this;
            }
        }
    }
}
