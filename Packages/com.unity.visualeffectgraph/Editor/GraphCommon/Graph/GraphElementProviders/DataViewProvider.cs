namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        class DataViewProvider : IDataViewProvider, IIndexable<MultiTreeNode<DataViewId>, DataView>
        {
            TaskGraph m_Owner;

            public int Count => m_Owner.m_DataViews.Count;

            public DataView this[DataViewId dataViewId] => new(this, m_Owner.DataViewTrees.Data[dataViewId.Index], m_Owner, m_Owner.m_DataViews[dataViewId]);
            public DataView this[MultiTreeNode<DataViewId> node] => new(this, node, m_Owner, m_Owner.m_DataViews[node.Data]);

            public DataViewProvider(TaskGraph owner)
            {
                m_Owner = owner;
            }

            public DataViewEnumerator GetEnumerator() => new(m_Owner.m_DataViews, this);
        }
    }
}
