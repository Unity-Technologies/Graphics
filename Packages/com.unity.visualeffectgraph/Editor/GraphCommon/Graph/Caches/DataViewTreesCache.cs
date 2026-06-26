using System;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        // Parent/child relationships between data views.
        sealed class DataViewTreesCache : ICopyableCache<DataViewTreesCache>, IDataViewObserver
        {
            LinearMultiTree<DataViewId> m_Tree = new();
            readonly TaskGraph m_Graph;

            public LinearMultiTree<DataViewId> Data => m_Tree;

            public DataViewTreesCache(TaskGraph graph)
            {
                m_Graph = graph;
            }

            public void OnDataViewAdded(DataViewId id, DataViewId parentId, DataContainerId dataContainerId, IDataDescription dataDescription)
            {
                m_Tree.AddItem(id, parentId.IsValid ? parentId.Index : -1);
            }

            public void OnDataViewRemoved(DataViewId id, DataViewId parentDataViewId, DataContainerId dataContainerId)
            {
                m_Tree.RemoveNode(id.Index);
            }

            public void OnGraphCleared()
            {
                m_Tree = new LinearMultiTree<DataViewId>();
            }

            public void RebuildFromPrimary()
            {
                m_Tree = new LinearMultiTree<DataViewId>();
                for (int slot = 0; slot < m_Graph.m_DataViews.SlotCount; slot++)
                {
                    ref var dv = ref m_Graph.m_DataViews.AtSlot(slot);
                    m_Tree.AddItem(dv.Id, dv.ParentDataViewId.IsValid ? dv.ParentDataViewId.Index : -1);
                }
            }

            public void CopyFrom(DataViewTreesCache other) => m_Tree.CopyFrom(other.m_Tree);
        }
    }
}
