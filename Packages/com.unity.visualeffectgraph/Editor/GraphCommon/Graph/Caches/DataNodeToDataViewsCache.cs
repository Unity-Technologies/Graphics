using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    [Flags]
    enum UsageFlags : byte
    {
        None    = 0,
        Read    = 1 << 0,
        Written = 1 << 1,
        Any     = Read | Written,
    }

    /// <summary>
    /// Ref counted usage of a data view. Enables correct removal.
    /// </summary>
    struct DataViewUsage
    {
        public DataViewId ViewId;
        public int ReadRefs;
        public int WriteRefs;

        public UsageFlags Flags =>
            (ReadRefs > 0 ? UsageFlags.Read : UsageFlags.None) |
            (WriteRefs > 0 ? UsageFlags.Written : UsageFlags.None);

        public bool Declares(UsageFlags flags) => (Flags & flags) != 0;
    }

    /// <summary>
    /// Caches which data views each data node's bindings declared through <see cref="ITask.GetBindingUsage"/>.
    /// </summary>
    sealed class DataNodeToDataViewsCache : ICopyableCache<DataNodeToDataViewsCache>, IDataNodeObserver, IDataBindingObserver
    {
        const int k_ViewsPerNodeCapacity = 4;
        const int k_ViewsPerBindingCapacity = 2;

        LinearMultiList<DataViewUsage> m_NodeUsage = new();
        LinearMultiList<DataViewUsage> m_BindingUsage = new();

        readonly ReadOnlyTaskGraphStore m_Store;

        readonly IDataViewProvider m_DataViews;

        public DataNodeToDataViewsCache(ReadOnlyTaskGraphStore store, IDataViewProvider dataViews)
        {
            m_Store = store;
            m_DataViews = dataViews;
        }

        public void OnDataNodeAdded(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            => m_NodeUsage.AddList(k_ViewsPerNodeCapacity);

        public void OnDataNodeRemoved(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            => m_NodeUsage.ClearList(id.Index);

        public void OnDataBindingAdded(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage,
            DataPathSet readPaths, DataPathSet writePaths)
        {
            m_BindingUsage.AddList(k_ViewsPerBindingCapacity);
            AddBinding(id, dataViewId, dataNodeId, readPaths, writePaths);
        }

        public void OnDataBindingRemoved(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            TaskNodeId taskNodeId, BindingUsage usage)
        {
            // A data node is removed before the bindings it carried, so its sublist may already be
            // cleared - releasing against an empty one is a no-op.
            int count = m_BindingUsage.CountInList(id.Index);
            for (int i = 0; i < count; i++)
            {
                var contribution = m_BindingUsage[id.Index, i];
                AddRefs(m_NodeUsage, dataNodeId.Index, contribution.ViewId,
                    -contribution.ReadRefs, -contribution.WriteRefs);
            }

            m_BindingUsage.ClearList(id.Index);
        }

        void AddBinding(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
            DataPathSet readPaths, DataPathSet writePaths)
        {
            var bindingDataView = m_DataViews[dataViewId];
            if (readPaths != null)
            {
                foreach (var path in readPaths)
                    Declare(id.Index, bindingDataView, path, UsageFlags.Read);
            }

            if (writePaths != null)
            {
                foreach (var path in writePaths)
                    Declare(id.Index, bindingDataView, path, UsageFlags.Written);
            }

            int count = m_BindingUsage.CountInList(id.Index);
            for (int i = 0; i < count; i++)
            {
                var contribution = m_BindingUsage[id.Index, i];
                AddRefs(m_NodeUsage, dataNodeId.Index, contribution.ViewId,
                    contribution.ReadRefs, contribution.WriteRefs);
            }
        }

        // Resolves one declared path against the binding's own view, as this binding's contribution;
        // folding those into the data node comes after, so the two stay in step by construction.
        void Declare(int bindingListIndex, DataView bindingDataView, DataPath path, UsageFlags flag)
        {
            if (!bindingDataView.FindSubData(path, out var subDataView))
                return;

            AddRefs(m_BindingUsage, bindingListIndex, subDataView.Id,
                flag == UsageFlags.Read ? 1 : 0, flag == UsageFlags.Written ? 1 : 0);
        }

        public void OnGraphCleared()
        {
            m_NodeUsage = new LinearMultiList<DataViewUsage>();
            m_BindingUsage = new LinearMultiList<DataViewUsage>();
        }

        public void RebuildFromPrimary()
        {
            m_NodeUsage = new LinearMultiList<DataViewUsage>();
            m_BindingUsage = new LinearMultiList<DataViewUsage>();

            // A sublist per slot, dead ones included, so that a sublist index stays an id index.
            for (int slot = 0; slot < m_Store.DataNodes.SlotCount; slot++)
                m_NodeUsage.AddList(k_ViewsPerNodeCapacity);

            var readPaths = new DataPathSet();
            var writePaths = new DataPathSet();
            for (int slot = 0; slot < m_Store.DataBindings.SlotCount; slot++)
            {
                m_BindingUsage.AddList(k_ViewsPerBindingCapacity);

                if (!m_Store.DataBindings.IsAliveAtSlot(slot)) continue;

                var binding = m_Store.DataBindings.AtSlot(slot);
                if (!m_Store.DataNodes.IsAlive(binding.DataNodeId)) continue;

                readPaths.Clear();
                writePaths.Clear();
                var taskNodeId = m_Store.DataNodes[binding.DataNodeId].TaskNodeId;
                m_Store.TaskNodes[taskNodeId].Task.GetBindingUsage(binding.BindingDataKey, readPaths, writePaths);

                AddBinding(binding.Id, binding.DataViewId, binding.DataNodeId, readPaths, writePaths);
            }
        }

        public void CopyFrom(DataNodeToDataViewsCache other)
        {
            m_NodeUsage.CopyFrom(other.m_NodeUsage);
            m_BindingUsage.CopyFrom(other.m_BindingUsage);
        }

        /// <summary>The views this data node declared, in declaration order.</summary>
        public bool TryGetDeclaredViews(DataNodeId dataNodeId, out LinearMultiList<DataViewUsage>.Sublist views)
        {
            if (dataNodeId.Index >= m_NodeUsage.ListCount)
            {
                views = default;
                return false;
            }

            views = m_NodeUsage[dataNodeId.Index];
            return true;
        }

        public bool HasUsage(DataNodeId dataNodeId, DataViewId dataViewId, UsageFlags flags)
            => HasUsage(m_NodeUsage, dataNodeId.Index, dataViewId, flags);

        public bool HasUsage(DataBindingId dataBindingId, DataViewId dataViewId, UsageFlags flags)
            => HasUsage(m_BindingUsage, dataBindingId.Index, dataViewId, flags);

        bool HasUsage(LinearMultiList<DataViewUsage> lists, int listIndex, DataViewId dataViewId, UsageFlags flags)
        {
            if (listIndex >= lists.ListCount)
                return false;

            int count = lists.CountInList(listIndex);
            for (int i = 0; i < count; i++)
            {
                var usage = lists[listIndex, i];
                if (!usage.Declares(flags))
                    continue;

                if (IsAncestorOrSelf(usage.ViewId, dataViewId) || IsAncestorOrSelf(dataViewId, usage.ViewId))
                    return true;
            }
            return false;
        }

        bool IsAncestorOrSelf(DataViewId ancestorViewId, DataViewId dataViewId)
        {
            for (var current = dataViewId; current.IsValid; current = ParentOf(current))
            {
                if (current.Equals(ancestorViewId))
                    return true;
            }
            return false;
        }

        DataViewId ParentOf(DataViewId dataViewId)
            => m_Store.DataViews.IsAlive(dataViewId)
                ? m_Store.DataViews[dataViewId].ParentDataViewId
                : DataViewId.Invalid;

        // The one mutation: takes references on a view's entry, or releases them with negative
        // deltas, creating the entry on its first reference and dropping it with its last.
        static void AddRefs(LinearMultiList<DataViewUsage> lists, int listIndex, DataViewId viewId,
            int readRefs, int writeRefs)
        {
            int count = lists.CountInList(listIndex);
            int itemIndex = IndexOf(lists, listIndex, count, viewId);
            if (itemIndex < 0)
            {
                if (readRefs <= 0 && writeRefs <= 0)
                    return;

                lists.AddItem(listIndex, new DataViewUsage { ViewId = viewId });
                itemIndex = count;
            }

            var usage = lists[listIndex, itemIndex];
            usage.ReadRefs += readRefs;
            usage.WriteRefs += writeRefs;

            if (usage.ReadRefs <= 0 && usage.WriteRefs <= 0)
                lists.RemoveItem(listIndex, itemIndex);
            else
                lists[listIndex, itemIndex] = usage;
        }

        // By view, where LinearMultiList.FindItem would compare whole entries, ref-counts included.
        static int IndexOf(LinearMultiList<DataViewUsage> lists, int listIndex, int count, DataViewId viewId)
        {
            for (int i = 0; i < count; i++)
            {
                if (lists[listIndex, i].ViewId.Equals(viewId))
                    return i;
            }
            return -1;
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new();
            sb.AppendLine("DataNodeToDataViewsCache:");

            for (int listIndex = 0; listIndex < m_NodeUsage.ListCount; listIndex++)
            {
                int count = m_NodeUsage.CountInList(listIndex);
                if (count == 0)
                    continue;

                sb.Append("Data node ").Append(listIndex).AppendLine(":");
                for (int i = 0; i < count; i++)
                {
                    var usage = m_NodeUsage[listIndex, i];
                    sb.Append("    ").Append(usage.ViewId).Append(' ')
                        .Append(FormatFlags(usage.Flags))
                        .Append(" R:").Append(usage.ReadRefs)
                        .Append(" W:").Append(usage.WriteRefs)
                        .AppendLine();
                }
            }

            return sb.ToString();
        }

        static string FormatFlags(UsageFlags flags) => flags switch
        {
            UsageFlags.None    => "[ ]",
            UsageFlags.Read    => "[R]",
            UsageFlags.Written => "[W]",
            UsageFlags.Any     => "[RW]",
            _                  => $"[?{flags}]",
        };
    }
}
