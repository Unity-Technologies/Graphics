using System;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A read only graph.
    /// </summary>
    /*public*/ interface IReadOnlyGraph : IVersioned
    {
        /// <summary>
        /// Gets an enumerable of all task nodes in the graph.
        /// </summary>
        ITaskNodeProvider TaskNodes { get; }

        /// <summary>
        /// Gets an enumerable of all data nodes in the graph.
        /// </summary>
        IDataNodeProvider DataNodes { get; }

        /// <summary>
        /// Gets an enumerable of all data views in the graph.
        /// </summary>
        IDataViewProvider DataViews { get; }

        /// <summary>
        /// Gets an enumerable of all data containers in the graph.
        /// </summary>
        IDataBindingProvider DataBindings { get; }

        /// <summary>
        /// Gets an enumerable of all data containers in the graph.
        /// </summary>
        IDataContainerProvider DataContainers { get; }

        /// <summary>
        /// Creates a new GraphTraverser for the graph.
        /// </summary>
        /// <returns>A new GraphTraverser.</returns>
        GraphTraverser CreateTraverser();

        /// <summary>
        /// Copies this graph into a new <see cref="IGraph"/>.
        /// </summary>
        /// <returns>A new <see cref="IGraph"/>, with the same contents of this graph.</returns>
        IGraph Copy();

        /// <summary>
        /// Retrieves an enumerable collection of data nodes associated with a specified task node.
        /// </summary>
        /// <param name="taskNodeId">The identifier of the task node for which data nodes are to be retrieved.</param>
        /// <returns>
        /// A <see cref="DataNodeEnumerable"/> containing the data nodes related to the specified
        /// <paramref name="taskNodeId"/>.
        /// </returns>
        internal DataNodeEnumerable GetDataNodes(TaskNodeId taskNodeId);

        /// <summary>
        /// Retrieves an enumerable collection of data bindings associated with a specified task node.
        /// </summary>
        internal DataBindingEnumerable GetDataBindings(TaskNodeId taskNodeId);

        /// <summary>
        /// Retrieves an enumerable collection of data bindings associated with a specified data node.
        /// </summary>
        internal DataBindingEnumerable GetDataBindings(DataNodeId dataNodeId);

        /// <summary>
        /// Retrieves an enumerable collection of data views used in the specified data node.
        /// </summary>
        /// <param name="dataNodeId">The identifier of the data node.</param>
        /// <returns>
        /// The <see cref="DataView"/> subtree containing the data views used by the specified <paramref name="dataNodeId"/>.
        /// </returns>
        internal DataView GetUsedDataViews(DataNodeId dataNodeId);

        /// <summary>
        /// Returns whether the specified data view is read by the specified data node.
        /// </summary>
        internal bool IsRead(DataNodeId dataNodeId, DataViewId dataViewId);

        /// <summary>
        /// Returns whether the specified data view is written by the specified data node.
        /// </summary>
        internal bool IsWritten(DataNodeId dataNodeId, DataViewId dataViewId);

        /// <summary>
        /// Retrieves the data container where the specified data view is stored.
        /// </summary>
        /// <param name="dataViewId">The identifier of the data view.</param>
        /// <returns>
        /// The <see cref="DataContainer"/> where the specified <paramref name="dataNodeId"/> is stored.
        /// </returns>
        internal DataContainer GetDataContainer(DataViewId dataViewId);

        /// <summary>
        /// Looks up an existing sub-data view by its parent view and sub-data key.
        /// </summary>
        /// <param name="parentDataViewId">The parent data view to search under.</param>
        /// <param name="subDataKey">The sub-data key identifying the child view.</param>
        /// <param name="dataViewId">The matching sub-view id, or invalid if none exists.</param>
        /// <returns>True if a matching sub-view exists; false otherwise.</returns>
        internal bool TryGetSubView(DataViewId parentDataViewId, IDataKey subDataKey, out DataViewId dataViewId);

        /// <summary>Returns true if the given data view is alive in this graph.</summary>
        bool IsValid(DataViewId dataViewId);

        /// <summary>Returns true if the given task node is alive in this graph.</summary>
        bool IsValid(TaskNodeId taskNodeId);

        /// <summary>Returns true if the given data node is alive in this graph.</summary>
        bool IsValid(DataNodeId dataNodeId);

        /// <summary>Returns true if the given data binding is alive in this graph.</summary>
        bool IsValid(DataBindingId dataBindingId);

        /// <summary>Returns true if the given data container is alive in this graph.</summary>
        bool IsValid(DataContainerId dataContainerId);
    }


    /// <summary>
    /// Specifies how a task uses data associated with a binding.
    /// </summary>
    [Flags]
    /*public*/ enum BindingUsage
    {
        /// <summary>
        /// The binding usage is undefined or unspecified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The task reads from the data but does not modify it.
        /// </summary>
        Read = 1 << 0,

        /// <summary>
        /// The task writes to the data, potentially modifying its contents.
        /// </summary>
        Write = 1 << 1,

        /// <summary>
        /// The task both reads from and writes to the data.
        /// </summary>
        ReadWrite = Read | Write,
    }

    /// <summary>
    /// A graph whose elements can be added, removed and changed, in any order.
    /// </summary>
    /*public*/ interface IGraph : IReadOnlyGraph
    {
        /// <summary>
        /// Clears the graph.
        /// Effectively removing all elements in it.
        /// </summary>
        void Clear();

        /// <summary>
        /// Adds a new task node to the graph.
        /// </summary>
        /// <param name="task">The task to wrap in a node.</param>
        /// <param name="name">The name of the node.</param>
        /// <returns>The Id associated with the new task node.</returns>
        TaskNodeId AddTask(ITask task, string name = null);

        /// <summary>
        /// Add a new data node to the graph, from a new data.
        /// </summary>
        /// <param name="name">Name of the new container being created.</param>
        /// <param name="dataDescription">The description of the data added to the graph.</param>
        /// <returns>The Id associated with the new data node.</returns>
        DataViewId AddData(string name, IDataDescription dataDescription);

        /// <summary>
        /// Retrieves or creates a subdata view using a data key to navigate within a parent data view.
        /// </summary>
        /// <param name="parentDataViewId">The ID of the parent data view.</param>
        /// <param name="subdataKey">The key identifying the subdata within the parent.</param>
        /// <param name="dataDescription">Optional data description, used to create the subdata if it is not created, or to validate it if it is.</param>
        /// <returns>The ID of the subdata view.</returns>
        DataViewId GetSubdata(DataViewId parentDataViewId, IDataKey subdataKey, IDataDescription dataDescription = null);

        /// <summary>
        /// Retrieves or creates a subdata view using a data path to navigate within a parent data view.
        /// </summary>
        /// <param name="parentDataViewId">The ID of the parent data view.</param>
        /// <param name="subdataPath">The path identifying the subdata within the parent.</param>
        /// <returns>The ID of the subdata view.</returns>
        DataViewId GetSubdata(DataViewId parentDataViewId, DataPath subdataPath);

        /// <summary>
        /// Binds data to a task node with explicit parent data nodes.
        /// </summary>
        /// <param name="taskNodeId">The ID of the task node to bind to.</param>
        /// <param name="bindingKey">The key identifying the binding point in the task.</param>
        /// <param name="dataViewId">The ID of the data view to bind.</param>
        /// <param name="parentNodeIds">The explicit parent data nodes to establish dependencies with.</param>
        /// <param name="usage">The usage mode for the binding.</param>
        void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId,
            IEnumerable<DataNodeId> parentNodeIds, BindingUsage usage = BindingUsage.Unknown);

        /// <summary>
        /// Removes the task node with the given id together with all data nodes, data bindings,
        /// task dependencies, and data dependencies that involve it.
        /// </summary>
        /// <param name="id">The id of the task node to remove.</param>
        void RemoveTask(TaskNodeId id);

        /// <summary>
        /// Removes the data binding identified by <paramref name="bindingKey"/> from the given task node.
        /// </summary>
        /// <param name="taskNodeId">The id of the task node holding the binding.</param>
        /// <param name="bindingKey">The key identifying the binding to remove.</param>
        void UnbindData(TaskNodeId taskNodeId, IDataKey bindingKey);

        /// <summary>
        /// Removes the data container with the given id together with all data views, data nodes, and data bindings that involve it.
        /// </summary>
        /// <param name="dataContainerId">The id of the container to remove.</param>
        void RemoveDataContainer(DataContainerId dataContainerId);

        //void Apply(GraphCommandList graph);

        //TODO: TEMPORARY, needs a more proper API
        /// <summary>
        /// Temporary. Overrides the data description of a data view.
        /// </summary>
        /// <param name="dataViewId">The ID of the data view to be overriden.</param>
        /// <param name="dataDescription">The new description of the data.</param>
        void OverrideDataDescription(DataViewId dataViewId, IDataDescription dataDescription);
    }
}
