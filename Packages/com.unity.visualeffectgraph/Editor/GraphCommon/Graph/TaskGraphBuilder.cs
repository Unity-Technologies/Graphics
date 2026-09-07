using System;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Builds a <see cref="TaskGraph"/> front to back: every element is added only once everything it
    /// depends on already is, which is what lets a binding infer its own parent data nodes.
    /// </summary>
    sealed class TaskGraphBuilder : IDataBindingObserver
    {
        struct DataViewWrites
        {
            public DataNodeId LastWrite;
            public HashSet<DataViewId> SubWrites;
        }

        // Indexed by DataViewId.Index. Entries are seeded to LastWrite = Invalid as the array grows.
        DataViewWrites[] m_Writes = Array.Empty<DataViewWrites>();

        TaskGraph m_Graph;

        TaskGraph BuildableGraph => m_Graph ?? throw new InvalidOperationException(
            "This TaskGraphBuilder finished building and can no longer be used.");

        /// <summary>The graph built so far, for reading.</summary>
        public IReadOnlyGraph Graph => BuildableGraph;

        public TaskGraphBuilder()
        {
            m_Graph = new TaskGraph();
            m_Graph.RegisterObserver(this);
        }

        /// <summary>Hands over the graph. The builder is unusable afterwards.</summary>
        public TaskGraph EndBuilding()
        {
            var graph = BuildableGraph;

            graph.UnregisterObserver(this);
            m_Graph = null;
            m_Writes = Array.Empty<DataViewWrites>();

            return graph;
        }

        public TaskNodeId AddTask(ITask task, string name = null) => BuildableGraph.AddTask(task, name);

        public DataViewId AddData(string name, IDataDescription dataDescription) => BuildableGraph.AddData(name, dataDescription);

        public DataViewId GetSubdata(DataViewId parentDataViewId, IDataKey subdataKey, IDataDescription dataDescription = null)
            => BuildableGraph.GetSubdata(parentDataViewId, subdataKey, dataDescription);

        public DataViewId GetSubdata(DataViewId parentDataViewId, DataPath subdataPath)
            => BuildableGraph.GetSubdata(parentDataViewId, subdataPath);

        public bool AddDataDependency(DataNodeId dataNodeId, DataNodeId parentDataNodeId)
            => BuildableGraph.AddDataDependency(dataNodeId, parentDataNodeId);

        /// <summary>Binds data, inferring the parents of a read from what has been built so far.</summary>
        public void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId,
            BindingUsage usage = BindingUsage.Unknown)
        {
            var graph = BuildableGraph;
            graph.ResolveBindingUsage(taskNodeId, bindingKey, ref usage, out var readPaths, out var writePaths);

            IEnumerable<DataNodeId> parentNodeIds = Array.Empty<DataNodeId>();
            if (usage.HasFlag(BindingUsage.Read))
            {
                parentNodeIds = FindImplicitParentDataNodes(dataViewId);
            }

            graph.BindData(taskNodeId, bindingKey, dataViewId, usage, parentNodeIds, readPaths, writePaths);
        }

        /// <summary>Binds data with explicit parents, overriding inference for this one binding.</summary>
        public void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId,
            IEnumerable<DataNodeId> parentNodeIds, BindingUsage usage = BindingUsage.Unknown)
            => BuildableGraph.BindData(taskNodeId, bindingKey, dataViewId, parentNodeIds, usage);

        void IDataBindingObserver.OnDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage)
        {
            if (!usage.HasFlag(BindingUsage.Write))
                return;

            ref DataViewWrites writes = ref WritesOf(dataViewId);
            writes.LastWrite = dataNodeId;
            writes.SubWrites?.Clear();

            // Each ancestor records this sub-view, so a read of the whole can find partial writes under it.
            var subDataViewId = dataViewId;
            var parentDataViewId = ParentOf(subDataViewId);
            while (parentDataViewId.IsValid)
            {
                ref DataViewWrites parentWrites = ref WritesOf(parentDataViewId);
                parentWrites.SubWrites ??= new HashSet<DataViewId>();
                parentWrites.SubWrites.Add(subDataViewId);

                subDataViewId = parentDataViewId;
                parentDataViewId = ParentOf(subDataViewId);
            }
        }

        List<DataNodeId> FindImplicitParentDataNodes(DataViewId dataViewId)
        {
            var result = new List<DataNodeId>();

            // Most recent writer of this view or any ancestor; ids are monotonic, so the higher index wins.
            DataNodeId parentDataNodeId = DataNodeId.Invalid;
            DataViewId parentDataViewId = dataViewId;
            while (parentDataViewId.IsValid)
            {
                DataNodeId dataNodeId = WritesOf(parentDataViewId).LastWrite;
                if (dataNodeId.IsValid && (!parentDataNodeId.IsValid || parentDataNodeId.Index < dataNodeId.Index))
                {
                    parentDataNodeId = dataNodeId;
                }
                parentDataViewId = ParentOf(parentDataViewId);
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
            var subWrites = WritesOf(dataViewId).SubWrites;
            if (subWrites == null)
                return;

            foreach (var subdataViewId in subWrites)
            {
                DataNodeId dataNodeId = WritesOf(subdataViewId).LastWrite;
                if (parentDataNodeId.Index < dataNodeId.Index)
                {
                    result.Add(dataNodeId);
                    AppendSubdataParentDataNodes(subdataViewId, dataNodeId, result);
                }
            }
        }

        DataViewId ParentOf(DataViewId dataViewId)
        {
            var parent = BuildableGraph.DataViews[dataViewId].Parent;
            return parent.HasValue ? parent.Value.Id : DataViewId.Invalid;
        }

        // Callers must not hold the returned ref across another WritesOf call: growing rebinds the array.
        ref DataViewWrites WritesOf(DataViewId dataViewId)
        {
            int index = dataViewId.Index;
            if (index >= m_Writes.Length)
            {
                int grownFrom = m_Writes.Length;
                Array.Resize(ref m_Writes, Math.Max(index + 1, m_Writes.Length * 2));

                for (int i = grownFrom; i < m_Writes.Length; i++)
                    m_Writes[i].LastWrite = DataNodeId.Invalid;
            }

            return ref m_Writes[index];
        }
    }
}
