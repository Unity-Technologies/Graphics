using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    /// <summary>
    /// Helper for calculating checkbox states from child items
    /// </summary>
    internal static class CheckboxStateHelper
    {
        /// <summary>
        /// Calculate checkbox state from a collection of child items
        /// Returns: (isChecked, isIndeterminate)
        /// </summary>
        public static (bool isChecked, bool isIndeterminate) CalculateCheckboxState(IEnumerable<IConverterItemState> childItems)
        {
            bool hasSelected = false;
            bool hasUnselected = false;

            foreach (var child in childItems)
            {
                if (child.item.isEnabled)
                {
                    if (child.isSelected == true)
                        hasSelected = true;
                    else
                        hasUnselected = true;

                    // Early exit: mixed state found
                    if (hasSelected && hasUnselected)
                        return (false, true);
                }
            }

            // No enabled children
            if (!hasSelected && !hasUnselected)
                return (false, false);

            // All selected (hasSelected && !hasUnselected)
            if (hasSelected)
                return (true, false);

            // None selected (!hasSelected && hasUnselected)
            return (false, false);
        }

        /// <summary>
        /// Calculate selection state as nullable bool (used internally for folder state)
        /// Returns: true (all selected), false (none selected), null (indeterminate)
        /// </summary>
        public static bool? CalculateSelectionState(IEnumerable<IConverterItemState> childItems)
        {
            var (isChecked, isIndeterminate) = CalculateCheckboxState(childItems);

            if (isIndeterminate)
                return null; // Mixed state

            return isChecked;
        }
    }

    interface IConverterItemState
    {
        bool? isSelected { get; set; }
        bool isExpanded { get; set; }
        IRenderPipelineConverterItem item { get; }
        (Status Status, string Message) conversionResult { get; }
        bool hasConverted { get; }
        Action<bool> onIsSelectedChanged { get; set; }

        void OnSelectionChanged(ClickEvent _);
        void OnClicked(ClickEvent _);
        void SetSelectedWithoutNotify(bool value);
        void SetSelectionChangedHandler(Action<bool> handler);
        void Clear();
        int CountItemsWithStatus(Status status);
        int CountSelectedItemsWithStatus(Status status);
        int GetTotalItemCount();
        int GetSelectedItemCount();
        bool IsFolder();
        int CompareTo(IConverterItemState other);
        DisplayFilter GetDisplayFilter();
        IEnumerable<ConverterItemState> GetLeafItems();
    }

    [Serializable]
    class ConverterItemState : IConverterItemState
    {
        [NonSerialized]
        private Action<bool> m_OnIsSelectedChanged;
        public Action<bool> onIsSelectedChanged
        {
            get => m_OnIsSelectedChanged;
            set => m_OnIsSelectedChanged = value;
        }

        [SerializeField]
        private bool m_IsSelected;
        public bool? isSelected
        {
            get => m_IsSelected;
            set
            {
                if (m_IsSelected != value)
                {
                    m_IsSelected = value ?? false;
                    m_OnIsSelectedChanged?.Invoke(m_IsSelected);
                }
            }
        }

        // Leaf items are never expanded
        public bool isExpanded { get => false; set { } }

        [field:SerializeReference]
        public IRenderPipelineConverterItem item { get; set; }

        [SerializeField]
        private Status m_ConversionStatus = Status.Pending;
        [SerializeField]
        private string m_ConversionMessage = string.Empty;

        public (Status Status, string Message) conversionResult
        {
            get => (m_ConversionStatus, m_ConversionMessage);
            set
            {
                m_ConversionStatus = value.Status;
                m_ConversionMessage = value.Message;
            }
        }
        public bool hasConverted => m_ConversionStatus != Status.Pending;

        public void OnSelectionChanged(ClickEvent _)
        {
            isSelected = !isSelected;
        }

        public void OnClicked(ClickEvent _)
        {
            item.OnClicked();
        }

        public void SetSelectedWithoutNotify(bool value)
        {
            m_IsSelected = value;
        }

        public void SetSelectionChangedHandler(Action<bool> handler)
        {
            m_OnIsSelectedChanged = handler;
        }

        public int CountItemsWithStatus(Status status)
        {
            return m_ConversionStatus == status ? 1 : 0;
        }

        public int CountSelectedItemsWithStatus(Status status)
        {
            return (m_IsSelected && m_ConversionStatus == status) ? 1 : 0;
        }

        public int GetTotalItemCount()
        {
            return 1;
        }

        public int GetSelectedItemCount()
        {
            return m_IsSelected ? 1 : 0;
        }

        public void Clear()
        {
        }

        public bool IsFolder()
        {
            return false;
        }

        public int CompareTo(IConverterItemState other)
        {
            if (other == null)
                return 1;

            if (other.IsFolder())
                return 1;

            // Handle null items gracefully
            if (item == null && other.item == null)
                return 0;
            if (item == null)
                return -1;
            if (other.item == null)
                return 1;

            return string.Compare(item.name, other.item.name, StringComparison.Ordinal);
        }

        public DisplayFilter GetDisplayFilter()
        {
            return m_ConversionStatus switch
            {
                Status.Pending => DisplayFilter.Pending,
                Status.Warning => DisplayFilter.Warnings,
                Status.Error => DisplayFilter.Errors,
                Status.Success => DisplayFilter.Success,
                _ => DisplayFilter.All
            };
        }

        public IEnumerable<ConverterItemState> GetLeafItems()
        {
            yield return this;
        }
    }

    [Serializable]
    class FolderItemState : IConverterItemState
    {
        [NonSerialized]
        private Action<bool> m_OnIsSelectedChanged;
        public Action<bool> onIsSelectedChanged
        {
            get => m_OnIsSelectedChanged;
            set => m_OnIsSelectedChanged = value;
        }

        private bool? m_IsSelected;
        public bool? isSelected
        {
            get => m_IsSelected;
            set
            {
                if (m_IsSelected != value && value.HasValue)
                {
                    SetSelectedWithoutNotify(value.Value);
                    m_OnIsSelectedChanged?.Invoke(value.Value);
                }
            }
        }

        [SerializeField]
        private bool m_IsExpanded = true; // Default to true so folders are expanded by default
        public bool isExpanded
        {
            get => m_IsExpanded;
            set => m_IsExpanded = value;
        }

        [field:SerializeReference]
        public IRenderPipelineConverterItem item { get; set; }

        [SerializeField]
        private Status m_ConversionStatus = Status.Pending;
        [SerializeField]
        private string m_ConversionMessage = string.Empty;

        public (Status Status, string Message) conversionResult
        {
            get => (m_ConversionStatus, m_ConversionMessage);
            set
            {
                m_ConversionStatus = value.Status;
                m_ConversionMessage = value.Message;
            }
        }
        public bool hasConverted => false;

        [SerializeReference]
        public List<IConverterItemState> children = new List<IConverterItemState>();

        public void OnSelectionChanged(ClickEvent _)
        {
            isSelected = !(m_IsSelected ?? false);
        }

        public void OnClicked(ClickEvent _)
        {
            item.OnClicked();
        }

        public void SetSelectedWithoutNotify(bool selected)
        {
            m_IsSelected = selected;
            SetAllChildrenSelectWithoutNotify(selected);
        }

        public void SetSelectionChangedHandler(Action<bool> handler)
        {
            m_OnIsSelectedChanged = handler;
        }

        private void SetAllChildrenSelectWithoutNotify(bool selected)
        {
            foreach (var child in children)
            {
                if (child.item.isEnabled)
                    child.SetSelectedWithoutNotify(selected);
            }
        }

        public void SubscribeToChildren()
        {
            foreach (var child in children)
            {
                child.onIsSelectedChanged -= OnChildSelectionChanged;
                child.onIsSelectedChanged += OnChildSelectionChanged;

                if (child is FolderItemState childFolder)
                    childFolder.SubscribeToChildren();
            }
        }

        /// <summary>
        /// Recalculates folder selection state from children.
        /// Called after deserialization since nullable bool can't be serialized.
        /// </summary>
        public void RecalculateSelectionFromChildren()
        {
            // Recursively recalculate for nested folders first, so this folder
            // aggregates up-to-date child states
            foreach (var child in children)
            {
                if (child is FolderItemState childFolder)
                    childFolder.RecalculateSelectionFromChildren();
            }

            m_IsSelected = CheckboxStateHelper.CalculateSelectionState(children);

            // Recursively recalculate for nested folders
            foreach (var child in children)
            {
                if (child is FolderItemState childFolder)
                    childFolder.RecalculateSelectionFromChildren();
            }
        }

        public void UnsubscribeFromChildren()
        {
            foreach (var child in children)
            {
                child.onIsSelectedChanged -= OnChildSelectionChanged;

                if (child is FolderItemState childFolder)
                    childFolder.UnsubscribeFromChildren();
            }
        }

        private void OnChildSelectionChanged(bool childSelected)
        {
            m_IsSelected = CheckboxStateHelper.CalculateSelectionState(children);
            m_OnIsSelectedChanged?.Invoke(m_IsSelected ?? false);
        }

        public (bool isChecked, bool isIndeterminate) GetCheckboxState()
        {
            return CheckboxStateHelper.CalculateCheckboxState(children);
        }

        public List<ConverterItemState> GetAllLeafItems()
        {
            var leafItems = new List<ConverterItemState>();
            CollectLeafItemsRecursive(children, leafItems);
            return leafItems;
        }

        private void CollectLeafItemsRecursive(List<IConverterItemState> items, List<ConverterItemState> targetList)
        {
            foreach (var child in items)
            {
                if (child is FolderItemState childFolder)
                    CollectLeafItemsRecursive(childFolder.children, targetList);
                else if (child is ConverterItemState leafItem)
                    targetList.Add(leafItem);
            }
        }

        public int CountItemsWithStatus(Status status)
        {
            int count = 0;
            foreach (var child in children)
            {
                count += child.CountItemsWithStatus(status);
            }
            return count;
        }

        public int CountSelectedItemsWithStatus(Status status)
        {
            int count = 0;
            foreach (var child in children)
            {
                count += child.CountSelectedItemsWithStatus(status);
            }
            return count;
        }

        public int GetTotalItemCount()
        {
            int count = 0;
            foreach (var child in children)
            {
                count += child.GetTotalItemCount();
            }
            return count;
        }

        public int GetSelectedItemCount()
        {
            int count = 0;
            foreach (var child in children)
            {
                count += child.GetSelectedItemCount();
            }
            return count;
        }

        public void Clear()
        {
            UnsubscribeFromChildren();
        }

        public bool IsFolder()
        {
            return true;
        }

        public int CompareTo(IConverterItemState other)
        {
            if (other == null)
                return 1;

            if (!other.IsFolder())
                return -1;

            // Handle null items gracefully
            if (item == null && other.item == null)
                return 0;
            if (item == null)
                return -1;
            if (other.item == null)
                return 1;

            return string.Compare(item.name, other.item.name, StringComparison.Ordinal);
        }

        public DisplayFilter GetDisplayFilter()
        {
            return DisplayFilter.All;
        }

        public IEnumerable<ConverterItemState> GetLeafItems()
        {
            foreach (var child in children)
            {
                foreach (var leafItem in child.GetLeafItems())
                    yield return leafItem;
            }
        }
    }
}
