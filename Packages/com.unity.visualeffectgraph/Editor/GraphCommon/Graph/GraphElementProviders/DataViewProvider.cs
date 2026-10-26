namespace Unity.GraphCommon.LowLevel.Editor
{
    class DataViewProvider : IDataViewProvider, IIndexable<MultiTreeNode<DataViewId>, DataView>
    {
        readonly TaskGraph m_Owner;
        readonly ReadOnlyTaskGraphStore m_Store;
        readonly DataViewTreesCache m_Trees;

        public int Count => m_Store.DataViews.Count;

        public DataView this[DataViewId dataViewId] => new(this, m_Trees.Data[dataViewId.Index], m_Owner, m_Store.DataViews[dataViewId]);
        public DataView this[MultiTreeNode<DataViewId> node] => new(this, node, m_Owner, m_Store.DataViews[node.Data]);

        public DataViewProvider(TaskGraph owner, ReadOnlyTaskGraphStore store, DataViewTreesCache trees)
        {
            m_Owner = owner;
            m_Store = store;
            m_Trees = trees;
        }

        public DataViewEnumerator GetEnumerator() => new(m_Store.DataViews, this);
    }
}
