using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    [Flags]
    internal enum DisplayFilter
    {
        None = 0,
        Pending = (1 << Status.Pending),
        Warnings = (1 << Status.Warning),
        Errors = (1 << Status.Error),
        Success = (1 << Status.Success),
        All = Pending | Warnings | Errors | Success
    }

    /// <summary>
    /// Represents a node in the tree view - can be either a folder or a leaf item
    /// </summary>
    [Serializable]
    internal class TreeNodeData
    {
        public string displayName;
        public string fullPath;

        [SerializeReference]
        public IConverterItemState itemState;

        public bool isFolder;

        // For folder nodes - track children for checkbox state calculation
        [SerializeReference]
        public List<IConverterItemState> childItems;

        public TreeNodeData(string displayName, string fullPath, bool isFolder, IConverterItemState itemState = null)
        {
            this.displayName = displayName;
            this.fullPath = fullPath;
            this.isFolder = isFolder;
            this.itemState = itemState;
            this.childItems = new List<IConverterItemState>();
        }

        /// <summary>
        /// For folder nodes: Calculate checkbox state based on children
        /// Returns: (isChecked, isIndeterminate)
        /// </summary>
        public (bool isChecked, bool isIndeterminate) GetFolderCheckboxState()
        {
            if (!isFolder || childItems.Count == 0)
                return (false, false);

            return CheckboxStateHelper.CalculateCheckboxState(childItems);
        }
    }

    // Each converter uses the active bool
    // Each converter has a list of active items/assets
    // We do this so that we can use the binding system of the UI Elements
    [Serializable]
    class ConverterState
    {
        public bool isExpanded;
        public bool isSelected;
        public bool isLoading; // to name
        public bool isInitialized;
        [SerializeReference]
        private List<IConverterItemState> items = new List<IConverterItemState>();
        [SerializeReference]
        public IRenderPipelineConverter converter;

        public DisplayFilter currentFilter = DisplayFilter.All;

        private readonly List<TreeViewItemData<TreeNodeData>> m_FilteredItemsTree = new List<TreeViewItemData<TreeNodeData>>();
        private readonly List<TreeViewItemData<TreeNodeData>> m_CachedFullTree = new List<TreeViewItemData<TreeNodeData>>();
        private bool m_TreeCacheDirty = true;

        public IList<TreeViewItemData<TreeNodeData>> filteredItemsTree => m_FilteredItemsTree;

        private int CountItemWithFlag(Status status)
        {
            int count = 0;
            foreach (var itemState in items)
            {
                count += itemState.CountItemsWithStatus(status);
            }
            return count;
        }
        public int pending => CountItemWithFlag(Status.Pending);
        public int warnings => CountItemWithFlag(Status.Warning);
        public int errors => CountItemWithFlag(Status.Error);
        public int success => CountItemWithFlag(Status.Success);

        public override string ToString()
        {
            return $"Warnings: {warnings} - Errors: {errors} - Ok: {success} - Total: {totalItemsCount}";
        }

        public void Clear()
        {
            foreach (var itemState in items)
            {
                itemState.Clear();
            }

            isInitialized = false;
            items.Clear();
            m_FilteredItemsTree.Clear();
            m_CachedFullTree.Clear();
            m_TreeCacheDirty = true;
        }

        private bool IsVisible(DisplayFilter filter)
        {
            return (currentFilter & filter) == filter;
        }

        internal void AddItem(IRenderPipelineConverterItem item)
        {
            var itemState = CreateItemState(item);
            items.Add(itemState);
            m_TreeCacheDirty = true;
        }

        internal void SetupRootEventHandlers(Action<bool> onRootSelectionChanged)
        {
            foreach (var itemState in items)
            {
                itemState.SetSelectionChangedHandler(onRootSelectionChanged);
            }
        }

        /// <summary>
        /// Recursively creates the appropriate state type (FolderItemState or ConverterItemState) for an item
        /// </summary>
        private IConverterItemState CreateItemState(IRenderPipelineConverterItem item)
        {
            if (item is IFolderRenderPipelineConverterItem folder)
            {
                // Create folder state with children
                var folderState = new FolderItemState
                {
                    item = folder
                };

                // Recursively create states for children
                if (folder.children != null)
                {
                    foreach (var child in folder.children)
                    {
                        var childState = CreateItemState(child);
                        folderState.children.Add(childState);
                    }
                }

                // Subscribe to children's events AFTER all children are added
                folderState.SubscribeToChildren();

                // Calculate initial state from children
                folderState.SetSelectedWithoutNotify(folder.isEnabled);

                return folderState;
            }
            else
            {
                // Create regular item state
                var leafState = new ConverterItemState
                {
                    item = item
                };

                // Set selection silently during initialization
                leafState.SetSelectedWithoutNotify(item.isEnabled);

                return leafState;
            }
        }

        internal void ApplyFilter()
        {
            if (m_TreeCacheDirty)
            {
                m_CachedFullTree.Clear();
                BuildFullTree(items, m_CachedFullTree);
                m_TreeCacheDirty = false;
            }

            m_FilteredItemsTree.Clear();
            FilterTreeView(m_CachedFullTree, m_FilteredItemsTree);
        }

        private void BuildFullTree(List<IConverterItemState> itemStates, List<TreeViewItemData<TreeNodeData>> target)
        {
            int nodeId = 0;
            var sortedItems = ListPool<IConverterItemState>.Get();
            sortedItems.AddRange(itemStates);

            sortedItems.Sort((a, b) => a.CompareTo(b));

            foreach (var itemState in sortedItems)
            {
                var treeNode = ConvertToTreeViewItemUncached(itemState, ref nodeId);
                if (treeNode.HasValue)
                    target.Add(treeNode.Value);
            }

            ListPool<IConverterItemState>.Release(sortedItems);
        }

        private TreeViewItemData<TreeNodeData>? ConvertToTreeViewItemUncached(IConverterItemState itemState, ref int nodeId)
        {
            if (itemState is FolderItemState folderState)
            {
                using (var sortedChildren = ListPool<IConverterItemState>.Get(out var sortedChildrenList))
                {
                    foreach (var child in folderState.children)
                        sortedChildrenList.Add(child);

                    sortedChildrenList.Sort((a, b) => a.CompareTo(b));

                    using (var children = ListPool<TreeViewItemData<TreeNodeData>>.Get(out var childrenList))
                    {
                        foreach (var child in sortedChildrenList)
                        {
                            var childNode = ConvertToTreeViewItemUncached(child, ref nodeId);
                            if (childNode.HasValue)
                                childrenList.Add(childNode.Value);
                        }

                        var folderNodeData = new TreeNodeData(
                            folderState.item.name,
                            "",
                            isFolder: true,
                            itemState: folderState
                        );

                        var leafItems = folderState.GetAllLeafItems();
                        folderNodeData.childItems.AddRange(leafItems);

                        var childrenListCopy = new List<TreeViewItemData<TreeNodeData>>(childrenList);

                        return new TreeViewItemData<TreeNodeData>(nodeId++, folderNodeData, childrenListCopy);
                    }
                }
            }
            else if (itemState is ConverterItemState leafItem)
            {
                var leafNodeData = new TreeNodeData(
                    leafItem.item.name,
                    "",
                    isFolder: false,
                    itemState: leafItem
                );

                return new TreeViewItemData<TreeNodeData>(nodeId++, leafNodeData);
            }

            return null;
        }

        private void FilterTreeView(List<TreeViewItemData<TreeNodeData>> sourceTree, List<TreeViewItemData<TreeNodeData>> target)
        {
            foreach (var node in sourceTree)
            {
                var filteredNode = FilterTreeNode(node);
                if (filteredNode.HasValue)
                    target.Add(filteredNode.Value);
            }
        }

        private TreeViewItemData<TreeNodeData>? FilterTreeNode(TreeViewItemData<TreeNodeData> node)
        {
            if (node.data.isFolder)
            {
                var filteredChildren = new List<TreeViewItemData<TreeNodeData>>();
                foreach (var child in node.children)
                {
                    var filteredChild = FilterTreeNode(child);
                    if (filteredChild.HasValue)
                        filteredChildren.Add(filteredChild.Value);
                }

                if (filteredChildren.Count == 0)
                    return null;

                return new TreeViewItemData<TreeNodeData>(node.id, node.data, filteredChildren);
            }
            else
            {
                if (!IsVisible(node.data.itemState.GetDisplayFilter()))
                    return null;

                return node;
            }
        }

        internal IEnumerable<ConverterItemState> GetAllLeafItems()
        {
            foreach (var itemState in items)
            {
                foreach (var leafItem in itemState.GetLeafItems())
                    yield return leafItem;
            }
        }

        public int totalItemsCount
        {
            get
            {
                int count = 0;
                foreach (var itemState in items)
                {
                    count += itemState.GetTotalItemCount();
                }
                return count;
            }
        }

        public int selectedItemsCount
        {
            get
            {
                int count = 0;
                foreach (var itemState in items)
                {
                    count += itemState.GetSelectedItemCount();
                }
                return count;
            }
        }

        public void SetAllItemsSelected(bool selected)
        {
            foreach (var itemState in items)
            {
                if (itemState.item.isEnabled)
                    itemState.SetSelectedWithoutNotify(selected);
            }
        }
    }
}
