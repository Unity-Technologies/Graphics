using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    // Parent/child relationships between data views.
    sealed class DataViewTreesCache : ICopyableCache<DataViewTreesCache>, IDataViewObserver
    {
        LinearMultiTree<DataViewId> m_Tree = new();
        readonly Dictionary<(DataViewId parentId, IDataKey subDataKey), DataViewId> m_SubViewDictionary = new();
        readonly ReadOnlyTaskGraphStore m_Store;

        public LinearMultiTree<DataViewId> Data => m_Tree;

        public DataViewTreesCache(ReadOnlyTaskGraphStore store)
        {
            m_Store = store;
        }

        public bool TryGetSubView(DataViewId parentDataViewId, IDataKey subDataKey, out DataViewId dataViewId)
            => m_SubViewDictionary.TryGetValue((parentDataViewId, subDataKey), out dataViewId);

        public void OnDataViewAdded(DataViewId id, DataViewId parentId, DataContainerId dataContainerId, IDataDescription dataDescription)
        {
            m_Tree.AddItem(id, parentId.IsValid ? parentId.Index : -1);
            if (parentId.IsValid)
            {
                var subDataKey = m_Store.DataViews[id].SubDataKey;
                if (subDataKey != null)
                    m_SubViewDictionary[(parentId, subDataKey)] = id;
            }
        }

        public void OnDataViewRemoved(DataViewId id, DataViewId parentDataViewId, DataContainerId dataContainerId)
        {
            if (parentDataViewId.IsValid)
            {
                var subDataKey = m_Store.DataViews[id].SubDataKey;
                if (subDataKey != null)
                    m_SubViewDictionary.Remove((parentDataViewId, subDataKey));
            }
            m_Tree.RemoveNode(id.Index);
        }

        public void OnGraphCleared()
        {
            m_Tree = new LinearMultiTree<DataViewId>();
            m_SubViewDictionary.Clear();
        }

        public void RebuildFromPrimary()
        {
            m_Tree = new LinearMultiTree<DataViewId>();
            m_SubViewDictionary.Clear();
            for (int slot = 0; slot < m_Store.DataViews.SlotCount; slot++)
            {
                var dv = m_Store.DataViews.AtSlot(slot);
                m_Tree.AddItem(dv.Id, dv.ParentDataViewId.IsValid ? dv.ParentDataViewId.Index : -1);
                if (dv.ParentDataViewId.IsValid && dv.SubDataKey != null)
                    m_SubViewDictionary[(dv.ParentDataViewId, dv.SubDataKey)] = dv.Id;
            }
        }

        public void CopyFrom(DataViewTreesCache other)
        {
            m_Tree.CopyFrom(other.m_Tree);
            m_SubViewDictionary.Clear();
            foreach (var kv in other.m_SubViewDictionary)
                m_SubViewDictionary.Add(kv.Key, kv.Value);
        }
    }
}
