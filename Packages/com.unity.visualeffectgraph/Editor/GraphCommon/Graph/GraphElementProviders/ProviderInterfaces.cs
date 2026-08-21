namespace Unity.GraphCommon.LowLevel.Editor
{

    /// <summary>
    /// Allocation-free struct enumerator over the live task nodes of a graph.
    /// </summary>
    /*public*/ struct TaskNodeEnumerator : IValueEnumerator<TaskNode>
    {
        GraphDataList<TaskNodeInfo>.Enumerator m_Inner;
        IIndexable<TaskNodeId, TaskNode> m_Provider;

        internal TaskNodeEnumerator(GraphDataList<TaskNodeInfo> source, IIndexable<TaskNodeId, TaskNode> provider)
        {
            m_Inner = source.GetEnumerator();
            m_Provider = provider;
        }

        public TaskNode Current => m_Provider[m_Inner.Current.Id];
        public bool MoveNext() => m_Inner.MoveNext();
    }

    /// <summary>
    /// Provides access to task nodes in a graph.
    /// </summary>
    /*public*/ interface ITaskNodeProvider : IIndexable<TaskNodeId, TaskNode>, ICountable
    {
        /// <summary>
        /// Returns an enumerator that iterates through the live task nodes.
        /// </summary>
        TaskNodeEnumerator GetEnumerator();
    }

    /// <summary>
    /// Allocation-free struct enumerator over the live data nodes of a graph.
    /// </summary>
    /*public*/ struct DataNodeEnumerator : IValueEnumerator<DataNode>
    {
        GraphDataList<DataNodeInfo>.Enumerator m_Inner;
        IIndexable<DataNodeId, DataNode> m_Provider;

        internal DataNodeEnumerator(GraphDataList<DataNodeInfo> source, IIndexable<DataNodeId, DataNode> provider)
        {
            m_Inner = source.GetEnumerator();
            m_Provider = provider;
        }

        public DataNode Current => m_Provider[m_Inner.Current.Id];
        public bool MoveNext() => m_Inner.MoveNext();
    }

    /// <summary>
    /// Provides access to data nodes in a graph.
    /// </summary>
    /*public*/ interface IDataNodeProvider : IIndexable<DataNodeId, DataNode>, ICountable
    {
        /// <summary>
        /// Returns an enumerator that iterates through the live data nodes.
        /// </summary>
        DataNodeEnumerator GetEnumerator();
    }

    /// <summary>
    /// Allocation-free struct enumerator over the live data views of a graph.
    /// </summary>
    /*public*/ struct DataViewEnumerator : IValueEnumerator<DataView>
    {
        GraphDataList<DataViewInfo>.Enumerator m_Inner;
        IIndexable<DataViewId, DataView> m_Provider;

        internal DataViewEnumerator(GraphDataList<DataViewInfo> source, IIndexable<DataViewId, DataView> provider)
        {
            m_Inner = source.GetEnumerator();
            m_Provider = provider;
        }

        public DataView Current => m_Provider[m_Inner.Current.Id];
        public bool MoveNext() => m_Inner.MoveNext();
    }

    /// <summary>
    /// Provides access to data views in a graph.
    /// </summary>
    /*public*/ interface IDataViewProvider : IIndexable<DataViewId, DataView>, ICountable
    {
        /// <summary>
        /// Returns an enumerator that iterates through the live data views.
        /// </summary>
        DataViewEnumerator GetEnumerator();
    }

    /// <summary>
    /// Allocation-free struct enumerator over the live data bindings of a graph.
    /// </summary>
    /*public*/ struct DataBindingEnumerator : IValueEnumerator<DataBinding>
    {
        GraphDataList<DataBindingInfo>.Enumerator m_Inner;
        IIndexable<DataBindingId, DataBinding> m_Provider;

        internal DataBindingEnumerator(GraphDataList<DataBindingInfo> source, IIndexable<DataBindingId, DataBinding> provider)
        {
            m_Inner = source.GetEnumerator();
            m_Provider = provider;
        }

        public DataBinding Current => m_Provider[m_Inner.Current.Id];
        public bool MoveNext() => m_Inner.MoveNext();
    }

    /// <summary>
    /// Provides access to data bindings in a graph.
    /// </summary>
    /*public*/ interface IDataBindingProvider : IIndexable<DataBindingId, DataBinding>, ICountable
    {
        /// <summary>
        /// Returns an enumerator that iterates through the live data bindings.
        /// </summary>
        DataBindingEnumerator GetEnumerator();
    }

    /// <summary>
    /// Allocation-free struct enumerator over the live data containers of a graph.
    /// </summary>
    /*public*/ struct DataContainerEnumerator : IValueEnumerator<DataContainer>
    {
        GraphDataList<DataContainerInfo>.Enumerator m_Inner;
        IIndexable<DataContainerId, DataContainer> m_Provider;

        internal DataContainerEnumerator(GraphDataList<DataContainerInfo> source, IIndexable<DataContainerId, DataContainer> provider)
        {
            m_Inner = source.GetEnumerator();
            m_Provider = provider;
        }

        public DataContainer Current => m_Provider[m_Inner.Current.Id];
        public bool MoveNext() => m_Inner.MoveNext();
    }

    /// <summary>
    /// Provides access to data containers in a graph.
    /// </summary>
    /*public*/ interface IDataContainerProvider : IIndexable<DataContainerId, DataContainer>, ICountable
    {
        /// <summary>
        /// Returns an enumerator that iterates through the live data containers.
        /// </summary>
        DataContainerEnumerator GetEnumerator();
    }
}
