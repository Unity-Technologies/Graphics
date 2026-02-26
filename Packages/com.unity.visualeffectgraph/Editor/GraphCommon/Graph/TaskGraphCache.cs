using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    partial class TaskGraph
    {
        DataViewId FindRootDataViewId(DataViewId dataViewId)
        {
            // Could be just a look-up if LinearTree enable O(1) root query
            var parentDataView = m_DataViews[dataViewId];
            while (parentDataView.ParentDataViewId.IsValid)
            {
                parentDataView = m_DataViews[parentDataView.ParentDataViewId];
            }
            return parentDataView.Id;
        }

        LinearMultiList<DataNodeId> BuildTaskNodeToDataNodes()
        {
            LinearMultiList<DataNodeId> taskNodeToDataNodes = new();
            foreach (var taskNode in m_TaskNodes)
            {
                var cacheInfo = TaskNodesCacheInfo.Valid ? TaskNodesCacheInfo.Data[taskNode.Id] : default;
                taskNodeToDataNodes.AddList(cacheInfo.BindingCount);
            }
            foreach (var dataNode in m_DataNodes)
            {
                taskNodeToDataNodes.AddItem(dataNode.TaskNodeId.Index, dataNode.Id);
            }
            taskNodeToDataNodes.Pack();
            return taskNodeToDataNodes;
        }

        LinearMultiList<DataBindingId> BuildTaskNodeToDataBindings()
        {
            LinearMultiList<DataBindingId> taskNodeToDataBindings = new();
            foreach (var taskNode in m_TaskNodes)
            {
                var cacheInfo = TaskNodesCacheInfo.Valid ? TaskNodesCacheInfo.Data[taskNode.Id] : default;
                taskNodeToDataBindings.AddList(cacheInfo.BindingCount);
            }
            foreach (var dataBinding in m_DataBindings)
            {
                int taskIndex = dataBinding.TaskNodeId.Index;
                taskNodeToDataBindings.AddItem(taskIndex, dataBinding.Id);
            }
            return taskNodeToDataBindings;
        }

        LinearMultiList<DataContainerId> BuildTaskNodeToDataContainers()
        {
            LinearMultiList<DataContainerId> taskNodeToDataContainers = new();
            foreach (var taskNode in m_TaskNodes)
            {
                //Reserve with decent estimate if info is available
                var cacheInfo = TaskNodesCacheInfo.Valid ? TaskNodesCacheInfo.Data[taskNode.Id] : default;
                taskNodeToDataContainers.AddList(cacheInfo.BindingCount);
            }
            foreach (var dataNode in m_DataNodes)
            {
                taskNodeToDataContainers.AddItem(dataNode.TaskNodeId.Index, dataNode.DataContainerId);
            }
            taskNodeToDataContainers.Pack();
            return taskNodeToDataContainers;
        }

        LinearMultiTree<DataViewId> BuildDataNodeToDataViews(Func<ITask, DataBindingInfo, (bool success, DataPathSet usage)> getUsage)
        {
            Dictionary<DataViewId, int> addedDataViewIds = new();
            LinearMultiTree<DataViewId> dataNodeToDataViews = new();

            int InsertDataView(DataViewId dataViewId, LinearMultiTree<DataViewId> dataNodeToDataViews, Dictionary<DataViewId, int> addedDataViewIds)
            {
                int index = -1;
                if (addedDataViewIds.TryGetValue(dataViewId, out index))
                    return index;

                int parentIndex = -1;
                var parentDataViewId = m_DataViews[dataViewId].ParentDataViewId;
                if (parentDataViewId.IsValid)
                {
                    parentIndex = InsertDataView(parentDataViewId, dataNodeToDataViews, addedDataViewIds);
                }

                int childCount = 0;
                if (DataViewTrees.Valid)
                {
                    childCount = DataViewTrees.Data[dataViewId.Index].Children.Count;
                }

                //Debug.Log($"Adding data view {dataViewId} with parent {parentDataViewId} and child count {childCount}");
                index = dataNodeToDataViews.AddItem(dataViewId, parentIndex, childCount);
                addedDataViewIds.Add(dataViewId, index);
                return index;
            }

            // Insert the root data views first, to enforce that the index of the dataNode matches the index in the LinearMultiTree
            foreach (var dataNode in m_DataNodes)
            {
                var dataViewId = m_DataContainers[dataNode.DataContainerId].RootDataViewId;

                int childCount = 0;
                if (DataViewTrees.Valid)
                {
                    childCount = DataViewTrees.Data[dataViewId.Index].Children.Count;
                }
                int index = dataNodeToDataViews.AddItem(dataViewId, -1, childCount);
                Debug.Assert(dataNode.Id.Index == index);
            }

            // Insert data views from bindings
            foreach (var dataNode in m_DataNodes)
            {
                addedDataViewIds.Clear();
                var dataNodeContainerDataView = m_DataContainers[dataNode.DataContainerId].RootDataViewId;
                addedDataViewIds.Add(dataNodeContainerDataView, dataNode.Id.Index);

                bool used = false;
                foreach (var dataBinding in m_DataBindings)
                {
                    if (!dataBinding.TaskNodeId.Equals(dataNode.TaskNodeId))
                        continue;

                    var bindingDataViewRoot = FindRootDataViewId(dataBinding.DataViewId);
                    if (!bindingDataViewRoot.Equals(dataNodeContainerDataView))
                        continue;

                    var dataViewId = dataBinding.DataViewId;
                    var task = m_TaskNodes[dataNode.TaskNodeId].Task;
                    var (success, usage) = getUsage(task, dataBinding);
                    if (success)
                    {
                        foreach (var path in usage.DataPaths)
                        {
                            var subdataViewId = GetSubdata(dataViewId, path);
                            if (!subdataViewId.IsValid)
                            {
                                Debug.LogWarning(
                                    $"Task {dataNode.TaskNodeId}({task})  expects to access a sub-data ({path}) that does not exist.");
                                break;
                            }
                            used = true;

                            InsertDataView(subdataViewId, dataNodeToDataViews, addedDataViewIds);
                        }
                    }
                }
                if (!used)
                {
                    // Remove root data view if there is no usage
                    dataNodeToDataViews.SetData(dataNode.Id.Index, DataViewId.Invalid);
                }
            }

            if (DataViewTrees.Valid)
            {
                //TODO: Fix
                //dataNodeToDataViews.Pack();
            }
            return dataNodeToDataViews;
        }

        LinearMultiTree<DataViewId> BuildDataNodeToReadDataViews()
        {
            return BuildDataNodeToDataViews((task, dataBinding) =>
            {
                bool success = task.GetDataUsage(dataBinding.BindingDataKey, out DataPathSet readUsage, out _);
                return (success, readUsage);
            });
        }

        LinearMultiTree<DataViewId> BuildDataNodeToWrittenDataViews()
        {
            return BuildDataNodeToDataViews((task, dataBinding) =>
            {
                bool success = task.GetDataUsage(dataBinding.BindingDataKey, out _, out DataPathSet writeUsage);
                return (success, writeUsage);
            });
        }

        LinearMultiTree<DataViewId> BuildDataNodeToUsedDataViews()
        {
            LinearMultiTree<DataViewId> dataNodeToDataViews = new();
            Dictionary<DataViewId, int> addedDataViewIds = new();

            void InsertDataViewTree(MultiTreeNode<DataViewId> dataViewTree, int parentIndex, LinearMultiTree<DataViewId> dataNodeToDataViews, Dictionary<DataViewId, int> addedDataViewIds)
            {
                if (!dataViewTree.Data.IsValid)
                    return;
                if (!addedDataViewIds.TryGetValue(dataViewTree.Data, out var index))
                {
                    index = dataNodeToDataViews.AddItem(dataViewTree.Data, parentIndex, dataViewTree.Children.Count);
                    addedDataViewIds.Add(dataViewTree.Data, index);
                }
                foreach (var child in dataViewTree.Children)
                {
                    InsertDataViewTree(child, index, dataNodeToDataViews, addedDataViewIds);
                }
            }

            var dataNodeToReadDataViews = DataNodeToReadDataViews.Data;
            var dataNodeToWrittenDataViews = DataNodeToWrittenDataViews.Data;

            foreach (var dataNode in m_DataNodes)
            {
                dataNodeToDataViews.AddItem(DataViewId.Invalid, -1);
            }
            foreach (var dataNode in m_DataNodes)
            {
                int dataNodeIndex = dataNode.Id.Index;
                var readRootDataView = dataNodeToReadDataViews[dataNodeIndex];
                var writtenRootDataView = dataNodeToWrittenDataViews[dataNodeIndex];

                var rootDataViewId = readRootDataView.Data.IsValid ? readRootDataView.Data : writtenRootDataView.Data;
                Debug.Assert(!readRootDataView.Data.IsValid || readRootDataView.Data.Index == rootDataViewId.Index);
                Debug.Assert(!writtenRootDataView.Data.IsValid || writtenRootDataView.Data.Index == rootDataViewId.Index);

                addedDataViewIds.Clear();
                if (rootDataViewId.IsValid)
                {
                    addedDataViewIds.Add(rootDataViewId, dataNodeIndex);
                    dataNodeToDataViews.SetData(dataNodeIndex, rootDataViewId);
                }

                InsertDataViewTree(readRootDataView, -1, dataNodeToDataViews, addedDataViewIds);
                InsertDataViewTree(writtenRootDataView, -1, dataNodeToDataViews, addedDataViewIds);
            }

            Debug.Assert(dataNodeToReadDataViews.RootNodes.Count == dataNodeToDataViews.RootNodes.Count);
            return dataNodeToDataViews;
        }

        LinearMultiList<DataBindingId> BuildDataNodeToDataBindings()
        {
            LinearMultiList<DataBindingId> dataNodeToDataBindings = new();
            foreach (var dataNode in m_DataNodes)
            {
                dataNodeToDataBindings.AddList();
                foreach (var dataBinding in m_DataBindings)
                {
                    if (dataBinding.TaskNodeId.Equals(dataNode.TaskNodeId)
                        && FindRootDataViewId(dataBinding.DataViewId).Equals(m_DataContainers[dataNode.DataContainerId].RootDataViewId))
                    {
                        dataNodeToDataBindings.AddItem(dataNode.Id.Index, dataBinding.Id);
                    }
                }
            }
            return dataNodeToDataBindings;
        }

        LinearMultiList<DataNodeId> BuildDataViewToDataNodes()
        {
            LinearMultiList<DataNodeId> dataViewToDataNodes = new();
            foreach (var dataView in m_DataViews)
            {
                dataViewToDataNodes.AddList();
            }

            void AddToDataViews(DataNodeId dataNodeId, MultiTreeNode<DataViewId> dataViewTreeNode)
            {
                dataViewToDataNodes.AddItem(dataViewTreeNode.Data.Index, dataNodeId);
                foreach (var child in dataViewTreeNode.Children)
                {
                    AddToDataViews(dataNodeId, dataViewTreeNode);
                }
            }

            foreach (var dataNode in m_DataNodes)
            {
                AddToDataViews(dataNode.Id, DataNodeToDataViews.Data[dataNode.Id.Index]);
            }
            return dataViewToDataNodes;
        }

        List<DataContainerId> BuildDataViewToDataContainers()
        {
            List<DataContainerId> dataViewToDataContainer = new(m_DataViews.Count);
            foreach (var dataView in m_DataViews)
            {
                DataContainerId dataContainerId = DataContainerId.Invalid;
                var rootDataViewId = FindRootDataViewId(dataView.Id);
                foreach (var dataContainer in m_DataContainers)
                {
                    if (rootDataViewId.Equals(dataContainer.RootDataViewId))
                    {
                        dataContainerId = dataContainer.Id;
                        break;
                    }
                }
                dataViewToDataContainer.Add(dataContainerId);
            }
            return dataViewToDataContainer;
        }

        List<DataNodeId> BuildDataBindingToDataNodes()
        {
            List<DataNodeId> dataBindingToDataNodes = new(m_DataBindings.Count);
            foreach (var dataBinding in m_DataBindings)
            {
                DataNodeId dataNodeId = DataNodeId.Invalid;
                DataViewId rootDataViewId = DataViewId.Invalid;
                foreach (var dataNode in m_DataNodes)
                {
                    if (dataBinding.TaskNodeId.Equals(dataNode.TaskNodeId))
                    {
                        if (!rootDataViewId.IsValid)
                        {
                            rootDataViewId = FindRootDataViewId(dataBinding.DataViewId);
                        }
                        var dataContainer = m_DataContainers[dataNode.DataContainerId];
                        if (rootDataViewId.Equals(dataContainer.RootDataViewId))
                        {
                            dataNodeId = dataNode.Id;
                            break;
                        }
                    }
                }
                Debug.Assert(dataNodeId.IsValid);
                Debug.Assert(dataBindingToDataNodes.Count == dataBinding.Id.Index);
                dataBindingToDataNodes.Add(dataNodeId);
            }
            return dataBindingToDataNodes;
        }

        LinearMultiList<DataViewId> BuildDataContainerToDataViews()
        {
            LinearMultiList<DataViewId> dataContainerToDataViews = new();
            foreach (var dataContainer in m_DataContainers)
            {
                dataContainerToDataViews.AddList();
                foreach (var dataView in m_DataViews)
                {
                    if(FindRootDataViewId(dataView.Id).Equals(dataContainer.RootDataViewId))
                        dataContainerToDataViews.AddItem(dataContainer.Id.Index, dataView.Id);
                }
            }
            return dataContainerToDataViews;
        }

        LinearMultiList<TaskNodeId> BuildDataContainerToTaskNodes()
        {
            LinearMultiList<TaskNodeId> dataContainerToTaskNodes = new();
            foreach (var dataContainer in m_DataContainers)
            {
                dataContainerToTaskNodes.AddList();
            }

            LinearMultiList<DataNodeId> taskNodeToDataNodes = TaskNodeToDataNodes;
            foreach (var dataNodes in taskNodeToDataNodes)
            {
                foreach (var dataNodeId in dataNodes)
                {
                    var dataNode = m_DataNodes[dataNodeId];
                    dataContainerToTaskNodes.AddItem(dataNode.DataContainerId.Index, dataNode.TaskNodeId);
                }
            }
            return dataContainerToTaskNodes;
        }

        LinearGraph<TaskNodeId> BuildTaskNodeGraph()
        {
            LinearGraph<TaskNodeId> graph = new();
            foreach (var taskNode in m_TaskNodes)
            {
                var cacheInfo = TaskNodesCacheInfo.Valid ? TaskNodesCacheInfo.Data[taskNode.Id] : default;
                graph.AddItem(taskNode.Id, cacheInfo.ParentCount, cacheInfo.ChildCount);
            }
            foreach (var taskDependency in m_TaskDependencies)
            {
                graph.Connect(taskDependency.ParentNodeId.Index, taskDependency.NodeId.Index); // Normally we should use the index returned by AddItem
            }
            return graph;
        }

        LinearGraph<DataNodeId> BuildDataNodeGraph()
        {
            LinearGraph<DataNodeId> graph = new();
            foreach (var dataNode in m_DataNodes)
            {
                var cacheInfo = DataNodesCacheInfo.Valid ? DataNodesCacheInfo.Data[dataNode.Id] : default;
                graph.AddItem(dataNode.Id, cacheInfo.ParentCount, cacheInfo.ChildCount);
            }
            foreach (var dataDependency in m_DataDependencies)
            {
                graph.Connect(dataDependency.ParentNodeId.Index, dataDependency.NodeId.Index); // Normally we should use the index returned by AddItem
            }

            return graph;
        }

        LinearMultiTree<DataViewId> BuildDataViewTrees()
        {
            LinearMultiTree<DataViewId> tree = new();
            foreach (var dataView in m_DataViews)
            {
                int childCapacity = DataViewsCacheInfo.Valid ? DataViewsCacheInfo.Data[dataView.Id].ChildCount : 0;
                tree.AddItem(dataView.Id, dataView.ParentDataViewId.Index, childCapacity); // Normally we should use the parent index returned by AddItem
            }

            return tree;
        }

        SubordinateList<DataNodeCacheInfo> BuildDataNodesCacheInfo()
        {
            SubordinateList<DataNodeCacheInfo> dataNodesCacheInfo = new(m_DataNodes);

            foreach (var dataDependency in m_DataDependencies)
            {
                dataNodesCacheInfo[dataDependency.NodeId].ParentCount++;
                dataNodesCacheInfo[dataDependency.ParentNodeId].ChildCount++;
            }
            return dataNodesCacheInfo;
        }

        SubordinateList<TaskNodeCacheInfo> BuildTaskNodesCacheInfo()
        {
            SubordinateList<TaskNodeCacheInfo> taskNodesCacheInfo = new(m_TaskNodes);

            foreach (var taskDependency in m_TaskDependencies)
            {
                taskNodesCacheInfo[taskDependency.NodeId].ParentCount++;
                taskNodesCacheInfo[taskDependency.ParentNodeId].ChildCount++;
            }

            foreach (var binding in m_DataBindings)
            {
                taskNodesCacheInfo[binding.TaskNodeId].BindingCount++;
            }
            return taskNodesCacheInfo;
        }

        SubordinateList<DataViewCacheInfo> BuildDataViewsCacheInfo()
        {
            SubordinateList<DataViewCacheInfo> dataViewsCacheInfo = new(m_DataViews);

            foreach (var dataView in m_DataViews)
            {
                if(dataView.ParentDataViewId.IsValid)
                    dataViewsCacheInfo[dataView.ParentDataViewId].ChildCount++;
            }
            return dataViewsCacheInfo;
        }

        SubordinateList<DataViewWriteCacheInfo> BuildDataViewsWriteCacheInfo()
        {
            SubordinateList<DataViewWriteCacheInfo> dataViewsCacheInfo = new(m_DataViews);

            /*foreach (var dataView in m_DataViews)
            {
                dataViewsCacheInfo[dataView.ParentDataViewId].ChildCount++;
            }*/
            return dataViewsCacheInfo;
        }
    }
}
