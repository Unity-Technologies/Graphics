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
        /// Copies this graph into a new IMutableGraph.
        /// </summary>
        /// <returns>A new IMutableGraph, with the same contents of this graph.</returns>
        IMutableGraph Copy();

        /// <summary>
        /// Creates a new empty graph of the same type.
        /// </summary>
        /// <returns>A new empty IMutableGraph.</returns>
        IMutableGraph EmptyCopy();

        /// <summary>
        /// Retrieves an enumerable collection of data nodes associated with a specified task node.
        /// </summary>
        /// <param name="taskNodeId">The identifier of the task node for which data nodes are to be retrieved.</param>
        /// <returns>
        /// A <see cref="DataNodeEnumerable{SubEnumerable{DataNodeId}}"/> containing the data nodes
        /// related to the specified <paramref name="taskNodeId"/>.
        /// </returns>
        DataNodeEnumerable<SubEnumerable<DataNodeId>> GetDataNodes(TaskNodeId taskNodeId);

        /// <summary>
        /// Retrieves the data node corresponding to the specified data binding.
        /// </summary>
        /// <param name="dataBindingId">The identifier of the data binding for which the data node is to be retrieved.</param>
        /// <returns>
        /// The data node corresponding to the specified <paramref name="dataBindingId"/>.
        /// </returns>
        public DataNode GetDataNode(DataBindingId dataBindingId);

        /// <summary>
        /// Retrieves an enumerable collection of data bindings associated with a specified task node.
        /// </summary>
        /// <param name="taskNodeId">The identifier of the task node for which data bindings are to be retrieved.</param>
        /// <returns>
        /// A <see cref="DataNodeEnumerable{SubEnumerable{DataBindingId}}"/> containing the data bindings
        /// related to the specified <paramref name="taskNodeId"/>.
        /// </returns>
        DataBindingEnumerable<SubEnumerable<DataBindingId>> GetDataBindings(TaskNodeId taskNodeId);

        /// <summary>
        /// Retrieves an enumerable collection of data views used in the specified data node.
        /// </summary>
        /// <param name="dataNodeId">The identifier of the data node.</param>
        /// <returns>
        /// The <see cref="DataView"/> subtree containing the data views used by the specified <paramref name="dataNodeId"/>.
        /// </returns>
        DataView GetUsedDataViews(DataNodeId dataNodeId);

        /// <summary>
        /// Retrieves an enumerable collection of data views read in the specified data node.
        /// </summary>
        /// <param name="dataNodeId">The identifier of the data node.</param>
        /// <returns>
        /// The <see cref="DataView"/> subtree containing the data views read by the specified <paramref name="dataNodeId"/>.
        /// </returns>
        DataView GetReadDataViews(DataNodeId dataNodeId);

        /// <summary>
        /// Retrieves an enumerable collection of data views written in the specified data node.
        /// </summary>
        /// <param name="dataNodeId">The identifier of the data node.</param>
        /// <returns>
        /// The <see cref="DataView"/> subtree containing the data views written by the specified <paramref name="dataNodeId"/>.
        /// </returns>
        DataView GetWrittenDataViews(DataNodeId dataNodeId);

        /// <summary>
        /// Retrieves the data container where the specified data view is stored.
        /// </summary>
        /// <param name="dataViewId">The identifier of the data view.</param>
        /// <returns>
        /// The <see cref="DataContainer"/> where the specified <paramref name="dataNodeId"/> is stored.
        /// </returns>
        DataContainer GetDataContainer(DataViewId dataViewId);
    }

    /// <summary>
    /// Provides access to task nodes in a graph.
    /// </summary>
    /*public*/ interface ITaskNodeProvider : IIndexable<int, TaskNode>, IIndexable<TaskNodeId, TaskNode>, ICountable
    {
        /// <summary>
        /// Gets whether the provider contains valid data.
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the task nodes.
        /// </summary>
        /// <returns>A linear enumerator for task nodes.</returns>
        LinearEnumerator<ITaskNodeProvider, TaskNode> GetEnumerator();
    }

    /// <summary>
    /// Provides access to data nodes in a graph.
    /// </summary>
    /*public*/ interface IDataNodeProvider : IIndexable<int, DataNode>, IIndexable<DataNodeId, DataNode>, ICountable
    {
        /// <summary>
        /// Gets whether the provider contains valid data.
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the data nodes.
        /// </summary>
        /// <returns>A linear enumerator for data nodes.</returns>
        LinearEnumerator<IDataNodeProvider, DataNode> GetEnumerator();
    }

    /// <summary>
    /// Provides access to data views in a graph.
    /// </summary>
    /*public*/ interface IDataViewProvider : IIndexable<int, DataView>, IIndexable<DataViewId, DataView>, ICountable
    {
        /// <summary>
        /// Gets whether the provider contains valid data.
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the data views.
        /// </summary>
        /// <returns>A linear enumerator for data views.</returns>
        LinearEnumerator<IDataViewProvider, DataView> GetEnumerator();
    }

    /// <summary>
    /// Provides access to data bindings in a graph.
    /// </summary>
    /*public*/ interface IDataBindingProvider : IIndexable<int, DataBinding>, IIndexable<DataBindingId, DataBinding>, ICountable
    {
        /// <summary>
        /// Gets whether the provider contains valid data.
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the data bindings.
        /// </summary>
        /// <returns>A linear enumerator for data bindings.</returns>
        LinearEnumerator<IDataBindingProvider, DataBinding> GetEnumerator();
    }

    /// <summary>
    /// Provides access to data containers in a graph.
    /// </summary>
    /*public*/ interface IDataContainerProvider : IIndexable<int, DataContainer>, IIndexable<DataContainerId, DataContainer>, ICountable
    {
        /// <summary>
        /// Gets whether the provider contains valid data.
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the data containers.
        /// </summary>
        /// <returns>A linear enumerator for data containers.</returns>
        LinearEnumerator<IDataContainerProvider, DataContainer> GetEnumerator();
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
    /// A buildable graph.
    /// </summary>
    /// <remarks>
    /// This interface allows to add elements to a graph but not to remove them (or all at once with Clear()).
    /// Meaning element Ids remain valid in the graph.
    /// </remarks>
    /*public*/ interface IBuildableGraph
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
        /// <returns>The Id associated with the new task node.</returns>
        TaskNodeId AddTask(ITask task);

        /// <summary>
        /// Add a new data node to the graph, from a new data.
        /// </summary>
        /// <param name="name">Name of the new container being created.</param>
        /// <param name="dataDescription">The description of the data added to the graph.</param>
        /// <returns>The Id associated with the new data node.</returns>
        DataViewId AddData(string name, IDataDescription dataDescription);

        //TODO: TEMPORARY, we don't really support mutability as of now
        /// <summary>
        /// Temporary. Overrides the data description of a data view.
        /// </summary>
        /// <param name="dataViewId">The ID of the data view to be overriden.</param>
        /// <param name="dataDescription">The new description of the data.</param>
        void OverrideDataDescription(DataViewId dataViewId, IDataDescription dataDescription);

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
        /// Creates a binding between a task node and a data view through a specific binding key.
        /// </summary>
        /// <param name="taskNodeId">The ID of the task node to bind.</param>
        /// <param name="bindingKey">The key identifying which binding point on the task to use.</param>
        /// <param name="dataViewId">The ID of the data view to bind to the task.</param>
        /// <param name="usage">How the data will be used by the task (read, write, or both).</param>
        void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage = BindingUsage.Unknown);

        /// <summary>
        /// Finalizes the graph building process and returns a read-only version of the graph.
        /// </summary>
        /// <returns>A read-only representation of the built graph.</returns>
        public IReadOnlyGraph EndBuilding();
    }

    /// <summary>
    /// A mutable graph.
    /// </summary>
    /// <remarks>
    /// This interface allows to change elements in the graph.
    /// </remarks>
    /*public*/ interface IMutableGraph : IBuildableGraph, IReadOnlyGraph
    {
        /// <summary>
        /// Sets the task used for a specific TaskNodeId.
        /// </summary>
        /// <param name="id">The TaskNodeId of the task being set.</param>
        /// <param name="task">The task to be set for this id.</param>
        void SetTask(TaskNodeId id, ITask task);

        /// <summary>
        /// Sets the data used for a specific DataNodeId.
        /// </summary>
        /// <param name="id">The DataNodeId of the data being set.</param>
        /// <param name="dataId">The data id to be set for this id.</param>
        void SetData(DataNodeId id, DataContainerId dataId);

        /// <summary>
        /// Binds data to a task node with explicit parent data nodes.
        /// </summary>
        /// <param name="taskNodeId">The ID of the task node to bind to.</param>
        /// <param name="bindingKey">The key identifying the binding point in the task.</param>
        /// <param name="dataViewId">The ID of the data view to bind.</param>
        /// <param name="usage">The usage mode for the binding.</param>
        /// <param name="parentNodeIds">The explicit parent data nodes to establish dependencies with.</param>
        void BindData(TaskNodeId taskNodeId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage, IEnumerable<DataNodeId> parentNodeIds);

        //void RemoveTaskNode(TaskNodeId id);
        //void RemoveDataNode(DataNodeId id);
        //void RemoveData(DataId id);

        //void Apply(GraphCommandList graph);
    }

    /// <summary>
    /// Represents a graph structure.
    /// Provides methods to access node data, parent nodes, and child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in each graph node.</typeparam>
    /*public*/ interface IGraph<T> : IIndexable<int, GraphNode<T>>, ICountable, IVersioned
    {
        /// <summary>
        /// Gets the data associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The data of type <typeparamref name="T"/> stored in the specified node.</returns>
        T GetData(int index);

        /// <summary>
        /// Gets the parent nodes for the specified node index in the graph.
        /// </summary>
        /// <param name="index">The index of the node whose parents are to be retrieved.</param>
        /// <returns>
        /// A <see cref="GraphNodeLinks{T}"/> containing the parent nodes of the specified node.
        /// </returns>
        GraphNodeLinks<T> GetParents(int index);

        /// <summary>
        /// Gets the child nodes for the specified node index in the graph.
        /// </summary>
        /// <param name="index">The index of the node whose children are to be retrieved.</param>
        /// <returns>
        /// A <see cref="GraphNodeLinks{T}"/> containing the child nodes of the specified node.
        /// </returns>
        GraphNodeLinks<T> GetChildren(int index);

        /// <summary>
        /// Returns an enumerator that iterates through all nodes in the graph.
        /// </summary>
        /// <returns>A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the graph nodes.</returns>
        LinearEnumerator<IGraph<T>, GraphNode<T>> GetEnumerator() => new(this);
    }

    /// <summary>
    /// Represents a node within the graph, providing access to its associated data,
    /// parent nodes, and child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the node.</typeparam>
    /*public*/ readonly struct GraphNode<T>
    {
        /// <summary>
        /// Weak reference to the graph containing this node.
        /// Prevents ownership cycles and retains memory safety.
        /// </summary>
        readonly Handle<IGraph<T>> m_Owner;

        /// <summary>
        /// The index of this node within its graph.
        /// </summary>
        readonly int m_Index;

        /// <summary>
        /// Gets the graph to which this node belongs.
        /// </summary>
        public IGraph<T> Graph => m_Owner.Ref;

        /// <summary>
        /// Gets the data of type <typeparamref name="T"/> associated with this node.
        /// </summary>
        public T Data => Graph.GetData(m_Index);

        /// <summary>
        /// Gets the parent nodes linked to this node within the graph.
        /// </summary>
        public GraphNodeLinks<T> Parents => Graph.GetParents(m_Index);

        /// <summary>
        /// Gets the child nodes linked to this node within the graph.
        /// </summary>
        public GraphNodeLinks<T> Children => Graph.GetChildren(m_Index);

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNode{T}"/> struct.
        /// </summary>
        /// <param name="graph">The graph to which this node belongs.</param>
        /// <param name="index">The index of this node within the graph.</param>
        public GraphNode(IGraph<T> graph, int index)
        {
            m_Owner = new(graph);
            m_Index = index;
        }
    }

    /// <summary>
    /// Represents a collection of links (parents or children) associated with a graph node.
    /// Provides indexed access to linked nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the linked nodes.</typeparam>
    /*public*/ readonly struct GraphNodeLinks<T> : IIndexable<int, GraphNode<T>>, ICountable
    {
        /// <summary>
        /// Weak reference to the graph containing these links.
        /// </summary>
        readonly Handle<IGraph<T>> m_Owner;

        /// <summary>
        /// Source mapping indices of links (parent or child IDs) for resolution.
        /// </summary>
        readonly IIndexable<int, int, int> m_Source;

        /// <summary>
        /// The index of the current node within the graph.
        /// </summary>
        readonly int m_Index;

        /// <summary>
        /// The total number of links (parents or children) associated with the node.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the linked <see cref="GraphNode{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the link.</param>
        /// <value>The linked <see cref="GraphNode{T}"/>.</value>
        public GraphNode<T> this[int index] => m_Owner.Ref[m_Source[m_Index, index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodeLinks{T}"/> struct.
        /// </summary>
        /// <param name="owner">The graph containing these links.</param>
        /// <param name="source">
        /// The source mapping indices to resolve links (parent or child IDs).
        /// </param>
        /// <param name="index">The index of the current node within the graph.</param>
        /// <param name="count">The total number of links associated with this node.</param>
        public GraphNodeLinks(IGraph<T> owner, IIndexable<int, int, int> source, int index, int count)
        {
            m_Owner = new(owner);
            m_Source = source;
            m_Index = index;
            Count = count;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the links associated with this node.
        /// </summary>
        /// <returns>
        /// A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the linked nodes.
        /// </returns>
        public LinearEnumerator<GraphNodeLinks<T>, GraphNode<T>> GetEnumerator() => new(this);
    }

    /// <summary>
    /// Represents a multi-rooted hierarchical tree structure where nodes can be indexed, counted, and versioned.
    /// Provides methods to access node data, root nodes, and child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in each tree node.</typeparam>
    /*public*/ interface IMultiTree<T> : IIndexable<int, MultiTreeNode<T>>, ICountable, IVersioned
    {
        /// <summary>
        /// Gets the enumerable collection of root nodes in the tree.
        /// </summary>
        MultiTreeNodeEnumerable<IndirectEnumerable, T> RootNodes { get; }

        /// <summary>
        /// Gets the data associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The data of type <typeparamref name="T"/> stored in the specified node.</returns>
        T GetData(int index);

        /// <summary>
        /// Gets the data associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <param name="data">.</param>
        void SetData(int index, T data);

        /// <summary>
        /// Gets the root node associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The root node of type <see cref="MultiTreeNode{T}"/>.</returns>
        MultiTreeNode<T> GetRootNode(int index);

        /// <summary>
        /// Gets the parent node associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The parent node of type <see cref="MultiTreeNode{T}"/>.</returns>
        MultiTreeNode<T>? GetParentNode(int index);

        /// <summary>
        /// Gets the enumerable collection of children for the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>
        /// A <see cref="MultiTreeNodeEnumerable{TEnumerable, T}"/> containing the child nodes of the specified node.
        /// </returns>
        MultiTreeNodeEnumerable<SubEnumerable<int>, T> GetChildren(int index);

        /// <summary>
        /// Returns an enumerator that iterates through all nodes in the tree.
        /// </summary>
        /// <returns>A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the tree nodes.</returns>
        LinearEnumerator<IMultiTree<T>, MultiTreeNode<T>> GetEnumerator() => new(this);
    }

    /// <summary>
    /// Represents a node in a multi-rooted hierarchical tree.
    /// Provides access to the node's data, its root node, and its child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the tree node.</typeparam>
    /*public*/ readonly struct MultiTreeNode<T>
    {
        /// <summary>
        /// Weak reference to the tree containing this node.
        /// Prevents ownership cycles and retains memory safety.
        /// </summary>
        readonly Handle<IMultiTree<T>> m_Owner;

        /// <summary>
        /// The index of this node within its tree.
        /// </summary>
        readonly int m_Index;

        /// <summary>
        /// Gets the tree to which this node belongs.
        /// </summary>
        public IMultiTree<T> Tree => m_Owner.Ref;

        /// <summary>
        /// Gets the data of type <typeparamref name="T"/> associated with this node.
        /// </summary>
        public T Data => Tree.GetData(m_Index);

        /// <summary>
        /// Gets the root node associated with this node.
        /// </summary>
        public MultiTreeNode<T> Root => Tree.GetRootNode(m_Index);

        /// <summary>
        /// Gets the parent node associated with this node.
        /// </summary>
        public MultiTreeNode<T>? Parent => Tree.GetParentNode(m_Index);

        /// <summary>
        /// Gets the enumerable collection of child nodes associated with this node.
        /// </summary>
        public MultiTreeNodeEnumerable<SubEnumerable<int>, T> Children => Tree.GetChildren(m_Index);

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTreeNode{T}"/> struct.
        /// </summary>
        /// <param name="tree">The tree to which this node belongs.</param>
        /// <param name="index">The index of this node within the tree.</param>
        public MultiTreeNode(IMultiTree<T> tree, int index)
        {
            m_Owner = new(tree);
            m_Index = index;
        }
    }

    /// <summary>
    /// Represents an enumerable collection of tree nodes.
    /// Provides indexed and counted access to nodes.
    /// </summary>
    /// <typeparam name="TEnumerable">
    /// The type of the source providing indices for nodes.
    /// Must implement both <see cref="IIndexable{TIndex, TValue}"/> and <see cref="ICountable"/>.
    /// </typeparam>
    /// <typeparam name="T">The type of data stored in the nodes.</typeparam>
    /*public*/ readonly struct MultiTreeNodeEnumerable<TEnumerable, T> : IIndexable<int, MultiTreeNode<T>>, ICountable where TEnumerable : IIndexable<int, int>, ICountable
    {
        /// <summary>
        /// Weak reference to the tree containing these nodes.
        /// </summary>
        readonly Handle<IMultiTree<T>> m_Owner;

        /// <summary>
        /// The source providing indices for the nodes in the enumerable.
        /// </summary>
        readonly TEnumerable m_Source;

        /// <summary>
        /// Gets the tree to which this enumerable belongs.
        /// </summary>
        public IMultiTree<T> Tree => m_Owner.Ref;

        /// <summary>
        /// Gets the number of nodes in the enumerable.
        /// </summary>
        public int Count => m_Source.Count;

        /// <summary>
        /// Gets the <see cref="MultiTreeNode{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the node.</param>
        /// <value>The node of type <see cref="MultiTreeNode{T}"/> at the specified index.</value>
        public MultiTreeNode<T> this[int index] => Tree[m_Source[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTreeNodeEnumerable{TEnumerable, T}"/> struct.
        /// </summary>
        /// <param name="tree">The tree containing these nodes.</param>
        /// <param name="source">The source providing indices for the nodes.</param>
        public MultiTreeNodeEnumerable(IMultiTree<T> tree, TEnumerable source)
        {
            m_Owner = new(tree);
            m_Source = source;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the nodes in this enumerable.
        /// </summary>
        /// <returns>
        /// A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the nodes.
        /// </returns>
        public LinearEnumerator<MultiTreeNodeEnumerable<TEnumerable, T>, MultiTreeNode<T>> GetEnumerator() => new(this);
    }

    /// <summary>
    /// Represents an enumerable collection of data associated with tree nodes.
    /// Provides indexed and counted access to node data.
    /// </summary>
    /// <typeparam name="TEnumerable">
    /// The type of the source providing indices for the data.
    /// Must implement both <see cref="IIndexable{TIndex, TValue}"/> and <see cref="ICountable"/>.
    /// </typeparam>
    /// <typeparam name="T">The type of data stored in tree nodes.</typeparam>
    /*public*/ readonly struct MultiTreeDataEnumerable<TEnumerable, T> : IIndexable<int, T>, ICountable where TEnumerable : IIndexable<int, int>, ICountable
    {
        /// <summary>
        /// Weak reference to the tree containing these data nodes.
        /// </summary>
        readonly Handle<IMultiTree<T>> m_Owner;

        /// <summary>
        /// The source providing indices for the data nodes in the enumerable.
        /// </summary>
        readonly TEnumerable m_Source;

        /// <summary>
        /// Gets the tree to which this enumerable belongs.
        /// </summary>
        public IMultiTree<T> Tree => m_Owner.Ref;

        /// <summary>
        /// Gets the number of data nodes in the enumerable.
        /// </summary>
        public int Count => m_Source.Count;

        /// <summary>
        /// Gets the data of type <typeparamref name="T"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the data.</param>
        /// <value>The data of type <typeparamref name="T"/> at the specified index.</value>
        public T this[int index] => Tree[index].Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTreeDataEnumerable{TEnumerable, T}"/> struct.
        /// </summary>
        /// <param name="tree">The tree containing these data nodes.</param>
        /// <param name="source">The source providing indices for the data nodes.</param>
        public MultiTreeDataEnumerable(IMultiTree<T> tree, TEnumerable source)
        {
            m_Owner = new(tree);
            m_Source = source;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the data nodes in this enumerable.
        /// </summary>
        /// <returns>
        /// A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the data nodes.
        /// </returns>
        public LinearEnumerator<MultiTreeDataEnumerable<TEnumerable, T>, T> GetEnumerator() => new(this);
    }
}
