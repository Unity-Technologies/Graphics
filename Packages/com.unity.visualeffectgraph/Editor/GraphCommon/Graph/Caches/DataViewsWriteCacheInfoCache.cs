using System.Collections.Generic;
using Unity.Collections;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
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

            // Produces an independent copy, duplicating the SubWrites set so clones don't
            // share (and cross-contaminate) the source's HashSet instance.
            public DataViewWriteCacheInfo DeepCopy()
            {
                return new DataViewWriteCacheInfo
                {
                    LastWrite = LastWrite,
                    m_Subwrites = m_Subwrites != null ? new HashSet<DataViewId>(m_Subwrites) : null
                };
            }
        }

        sealed class DataViewsWriteCacheInfoCache : ICopyableCache<DataViewsWriteCacheInfoCache>, IDataViewObserver, IDataBindingObserver, IDataNodeObserver
        {
            readonly SubordinateList<DataViewWriteCacheInfo, DataViewInfo> m_Items;
            readonly TaskGraph m_Graph;

            public SubordinateList<DataViewWriteCacheInfo, DataViewInfo> Data => m_Items;

            public DataViewsWriteCacheInfoCache(GraphDataList<DataViewInfo> source, TaskGraph graph)
            {
                m_Items = new SubordinateList<DataViewWriteCacheInfo, DataViewInfo>(source);
                m_Graph = graph;
            }

            public void OnDataViewAdded(DataViewId id, DataViewId parentId, DataContainerId dataContainerId, IDataDescription dataDescription)
            {
                m_Items[id].LastWrite = DataNodeId.Invalid;
            }

            public void OnDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId, IDataKey bindingKey, BindingUsage usage)
            {
                if (!usage.HasFlag(BindingUsage.Write))
                    return;

                ref DataViewWriteCacheInfo cacheInfo = ref m_Items[dataViewId];
                cacheInfo.LastWrite = dataNodeId;
                cacheInfo.SubWrites.Clear();

                // Propagate upward: each ancestor records this sub-view in its SubWrites set
                // so AppendSubdataParentDataNodes can discover recent partial writes efficiently.
                var subDataViewId = dataViewId;
                var parentDataViewId = m_Graph.m_DataViews[subDataViewId].ParentDataViewId;
                while (parentDataViewId.IsValid)
                {
                    m_Items[parentDataViewId].SubWrites.Add(subDataViewId);
                    subDataViewId = parentDataViewId;
                    parentDataViewId = m_Graph.m_DataViews[subDataViewId].ParentDataViewId;
                }
            }

            public void OnDataNodeRemoved(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            {
                foreach (var dataView in m_Graph.m_DataViews)
                {
                    ref var info = ref m_Items[dataView.Id];
                    if (info.LastWrite.Equals(id))
                        info.LastWrite = DataNodeId.Invalid;
                }
            }

            public void OnGraphCleared() => m_Items.Release();

            public void CopyFrom(DataViewsWriteCacheInfoCache other)
            {
                m_Items.CopyFrom(other.m_Items, static info => info.DeepCopy());
            }

            public void RebuildFromPrimary()
            {
                m_Items.Resize(m_Graph.m_DataViews.Capacity);

                foreach (var dataView in m_Graph.m_DataViews)
                {
                    ref var info = ref m_Items[dataView.Id];
                    info.LastWrite = DataNodeId.Invalid;
                    info.SubWrites.Clear();
                }

                for (int slot = 0; slot < m_Graph.m_DataBindings.SlotCount; slot++)
                {
                    if (!m_Graph.m_DataBindings.IsAliveAtSlot(slot))
                        continue;

                    ref var dataBinding = ref m_Graph.m_DataBindings.AtSlot(slot);
                    if (!dataBinding.Usage.HasFlag(BindingUsage.Write))
                        continue;

                    var dataNodeId = dataBinding.DataNodeId;
                    OnDataBindingAdded(dataBinding.Id, dataBinding.DataViewId,
                        dataNodeId, dataBinding.BindingDataKey, dataBinding.Usage);
                }
            }
        }
    }
}
