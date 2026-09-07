using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        // Per-node usage flags packed into a byte. Stored parallel to the cache tree slots.
        [System.Flags]
        enum UsageFlags : byte
        {
            None    = 0,
            Read    = 1 << 0,
            Written = 1 << 1,
        }

        sealed class DataNodeToDataViewsCache : ICopyableCache<DataNodeToDataViewsCache>, IDataNodeObserver, IDataBindingObserver, IDataViewObserver
        {
            LinearMultiTree<DataViewId> m_Tree = new();
            readonly List<UsageFlags> m_Flags = new();
            readonly TaskGraph m_Owner;
            readonly Dictionary<DataNodeId, int> m_DataNodeIdToSlotIndex = new();
            readonly Dictionary<(DataNodeId, DataViewId), int> m_PairToSlot = new();

            readonly DataPathSet m_ReadUsage = new();
            readonly DataPathSet m_WriteUsage = new();

            public LinearMultiTree<DataViewId> Data => m_Tree;

            public DataNodeToDataViewsCache(TaskGraph owner) => m_Owner = owner;

            public void OnDataNodeAdded(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            {
                int slot = m_Tree.AddItem(DataViewId.Invalid, -1, 0);
                m_DataNodeIdToSlotIndex.Add(id, slot);
                EnsureFlagsCapacity(slot + 1);
            }

            public void OnDataNodeRemoved(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
            {
                if (!m_DataNodeIdToSlotIndex.TryGetValue(id, out int rootSlot))
                    return;

                ResetDataNode(id, rootSlot);
                m_DataNodeIdToSlotIndex.Remove(id);
            }

            public void OnDataBindingAdded(DataBindingId id,  DataViewId dataViewId,
                DataNodeId dataNodeId, TaskNodeId taskNodeId, IDataKey bindingKey, BindingUsage usage)
            {
                var dataBinding = m_Owner.m_DataBindings[id];
                var task = m_Owner.m_TaskNodes[taskNodeId].Task;
                m_ReadUsage.Clear();
                m_WriteUsage.Clear();
                if (task.GetBindingUsage(dataBinding.BindingDataKey, m_ReadUsage, m_WriteUsage) == BindingUsage.Unknown)
                    return;

                bool hasRead = !m_ReadUsage.Empty;
                bool hasWritten = !m_WriteUsage.Empty;
                if (!hasRead && !hasWritten) return;

                var bindingDataView = m_Owner.DataViews[dataViewId];
                BeginBinding(bindingDataView.Root.Id, dataNodeId);

                if (hasRead)
                    foreach (var path in m_ReadUsage)
                        InsertPath(dataNodeId, bindingDataView, path, UsageFlags.Read);

                if (hasWritten)
                    foreach (var path in m_WriteUsage)
                        InsertPath(dataNodeId, bindingDataView, path, UsageFlags.Written);
            }

            public void OnDataBindingRemoved(DataBindingId id, DataViewId dataViewId, DataNodeId dataNodeId,
                TaskNodeId taskNodeId, BindingUsage usage)
            {
                // Flags are ORed from every binding of the node, so one binding's contribution cannot be
                // subtracted: drop the node's subtree and restamp it from the bindings that remain.
                if (!m_DataNodeIdToSlotIndex.TryGetValue(dataNodeId, out int rootSlot))
                    return;

                ResetDataNode(dataNodeId, rootSlot);

                var bindings = m_Owner.DataNodeToDataBindings.Data;
                int count = bindings.CountInList(dataNodeId.Index);
                for (int i = 0; i < count; i++)
                {
                    var bindingId = bindings[dataNodeId.Index, i];
                    if (bindingId.Equals(id))
                        continue;
                    var bindingInfo = m_Owner.m_DataBindings[bindingId];
                    OnDataBindingAdded(bindingId, bindingInfo.DataViewId, dataNodeId, taskNodeId,
                        bindingInfo.BindingDataKey, bindingInfo.Usage);
                }
            }

            void ResetDataNode(DataNodeId dataNodeId, int rootSlot)
            {
                RemoveCacheSubtree(dataNodeId, rootSlot, removeSlot: false);
                m_PairToSlot.Remove((dataNodeId, m_Tree[rootSlot].Data));
                m_Flags[rootSlot] = UsageFlags.None;
                m_Tree.SetRootData(dataNodeId.Index, DataViewId.Invalid);
            }

            public void OnDataViewRemoved(DataViewId id, DataViewId parentId, DataContainerId dataContainerId)
            {
                // m_PairToSlot is mutated below, so iterate the data node keys instead.
                foreach (var dataNodeId in m_DataNodeIdToSlotIndex.Keys)
                {
                    if (!m_PairToSlot.TryGetValue((dataNodeId, id), out int slot))
                        continue;

                    // A node's root slot is owned by OnDataNodeAdded/Removed: clear it, don't remove it.
                    if (slot == m_DataNodeIdToSlotIndex[dataNodeId])
                        ResetDataNode(dataNodeId, slot);
                    else
                        RemoveCacheSubtree(dataNodeId, slot, removeSlot: true);
                }
            }

            void RemoveCacheSubtree(DataNodeId dataNodeId, int slot, bool removeSlot)
            {
                using (ListPool<int>.Get(out var subtree))
                {
                    subtree.Add(slot);
                    for (int i = 0; i < subtree.Count; i++)
                    {
                        var children = m_Tree[subtree[i]].Children;
                        for (int c = 0; c < children.Count; c++)
                            subtree.Add(children[c].Index);
                    }

                    for (int i = subtree.Count - 1; i >= (removeSlot ? 0 : 1); i--)
                    {
                        int subtreeSlot = subtree[i];
                        m_PairToSlot.Remove((dataNodeId, m_Tree[subtreeSlot].Data));
                        m_Flags[subtreeSlot] = UsageFlags.None;
                        m_Tree.RemoveNode(subtreeSlot);
                    }
                }
            }

            public void OnGraphCleared()
            {
                m_Tree = new LinearMultiTree<DataViewId>();
                m_Flags.Clear();
                m_DataNodeIdToSlotIndex.Clear();
                m_PairToSlot.Clear();
            }

            public void RebuildFromPrimary()
            {
                m_Tree = new LinearMultiTree<DataViewId>();
                m_Flags.Clear();
                m_DataNodeIdToSlotIndex.Clear();
                m_PairToSlot.Clear();

                for (int slot = 0; slot < m_Owner.m_DataNodes.SlotCount; slot++)
                {
                    int rootSlot = m_Tree.AddItem(DataViewId.Invalid, -1, 0);
                    EnsureFlagsCapacity(rootSlot + 1);
                    if (m_Owner.m_DataNodes.IsAliveAtSlot(slot))
                    {
                        ref var info = ref m_Owner.m_DataNodes.AtSlot(slot);
                        m_DataNodeIdToSlotIndex[info.Id] = rootSlot;
                    }
                }


                for (int slot = 0; slot < m_Owner.m_DataBindings.SlotCount; slot++)
                {
                    if (!m_Owner.m_DataBindings.IsAliveAtSlot(slot)) continue;
                    ref var binding = ref m_Owner.m_DataBindings.AtSlot(slot);
                    var dataNodeId = binding.DataNodeId;
                    if (!m_Owner.m_DataNodes.IsAlive(dataNodeId)) continue;
                    OnDataBindingAdded(binding.Id, binding.DataViewId, dataNodeId,
                        m_Owner.m_DataNodes[dataNodeId].TaskNodeId, binding.BindingDataKey, binding.Usage);
                }
            }

            public void CopyFrom(DataNodeToDataViewsCache other)
            {
                m_Tree.CopyFrom(other.m_Tree);
                m_Flags.Clear();
                m_Flags.AddRange(other.m_Flags);
                m_DataNodeIdToSlotIndex.Clear();
                foreach (var kv in other.m_DataNodeIdToSlotIndex)
                    m_DataNodeIdToSlotIndex.Add(kv.Key, kv.Value);
                m_PairToSlot.Clear();
                foreach (var kv in other.m_PairToSlot)
                    m_PairToSlot.Add(kv.Key, kv.Value);
            }

            public bool IsRead(DataNodeId dataNode, DataViewId dataView)
                => (GetFlag(dataNode, dataView) & UsageFlags.Read) != 0;

            public bool IsWritten(DataNodeId dataNode, DataViewId dataView)
                => (GetFlag(dataNode, dataView) & UsageFlags.Written) != 0;

            UsageFlags GetFlag(DataNodeId dataNode, DataViewId dataView)
                => m_PairToSlot.TryGetValue((dataNode, dataView), out int slot)
                    ? (UsageFlags)m_Flags[slot]
                    : UsageFlags.None;

            void BeginBinding(DataViewId rootDataViewId, DataNodeId dataNodeId)
            {
                int rootSlot = m_DataNodeIdToSlotIndex[dataNodeId];
                m_Tree.SetRootData(dataNodeId.Index, rootDataViewId);
                // Pre-mark the root for this data node so InsertWithFlag anchors sub-views to
                // the existing root slot rather than creating a duplicate.
                m_PairToSlot.TryAdd((dataNodeId, rootDataViewId), rootSlot);
                EnsureFlagsCapacity(rootSlot + 1);
            }

            void InsertPath(DataNodeId dataNodeId, DataView dataView, DataPath path, UsageFlags flag)
            {
                if (!dataView.FindSubData(path, out var subDataView))
                    return;
                InsertWithFlag(dataNodeId, subDataView.Id, flag);
            }

            int InsertWithFlag(DataNodeId dataNodeId, DataViewId dataViewId, UsageFlags flag)
            {
                if (m_PairToSlot.TryGetValue((dataNodeId, dataViewId), out int existing))
                {
                    PropagateFlagUpward(existing, flag);
                    return existing;
                }

                int parentIndex = -1;
                var parentDataViewId = m_Owner.m_DataViews[dataViewId].ParentDataViewId;
                if (parentDataViewId.IsValid)
                    parentIndex = InsertWithFlag(dataNodeId, parentDataViewId, flag);

                int childCount = m_Owner.DataViewTrees.Data[dataViewId.Index].Children.Count;
                int slot = m_Tree.AddItem(dataViewId, parentIndex, childCount);
                m_PairToSlot.Add((dataNodeId, dataViewId), slot);
                EnsureFlagsCapacity(slot + 1);
                m_Flags[slot] = flag;
                return slot;
            }

            void PropagateFlagUpward(int slot, UsageFlags flag)
            {
                while (slot >= 0)
                {
                    var current = (UsageFlags)m_Flags[slot];
                    if ((current & flag) == flag) return;
                    m_Flags[slot] = (current | flag);
                    var parent = m_Tree[slot].Parent;
                    slot = parent.HasValue ? parent.Value.Index : -1;
                }
            }

            void EnsureFlagsCapacity(int requiredCount)
            {
                while (m_Flags.Count < requiredCount)
                    m_Flags.Add(0);
            }

            public override string ToString()
            {
                System.Text.StringBuilder sb = new();
                sb.AppendLine("DataNodeToDataViewsCache:");

                void DrawNode(MultiTreeNode<DataViewId> node, string indent, bool isLast)
                {
                    sb.Append(indent);
                    sb.Append(isLast ? "└── " : "├── ");
                    sb.Append(node.Data.ToString());
                    sb.Append(' ');
                    sb.AppendLine(FormatFlag((UsageFlags)m_Flags[node.Index]));

                    var children = node.Children;
                    int childCount = children.Count;
                    for (int i = 0; i < childCount; i++)
                        DrawNode(children[i], indent + (isLast ? "    " : "│   "), i == childCount - 1);
                }

                var roots = m_Tree.RootNodes;
                int rootCount = roots.Count;
                for (int i = 0; i < rootCount; i++)
                    DrawNode(roots[i], $"{i}", i == rootCount - 1);

                return sb.ToString();
            }

            static string FormatFlag(UsageFlags flag) => flag switch
            {
                UsageFlags.None              => "[ ]",
                UsageFlags.Read              => "[R]",
                UsageFlags.Written           => "[W]",
                UsageFlags.Read | UsageFlags.Written => "[RW]",
                _                            => $"[?{flag}]",
            };
        }
    }
}
