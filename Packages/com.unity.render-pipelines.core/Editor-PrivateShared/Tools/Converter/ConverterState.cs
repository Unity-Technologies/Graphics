using System;
using System.Collections.Generic;
using UnityEngine;
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
        public List<ConverterItemState> items = new List<ConverterItemState>();
        [SerializeReference]
        public IRenderPipelineConverter converter;

        public DisplayFilter currentFilter = DisplayFilter.All;
        public IList<TreeViewItemData<ConverterItemState>> filteredItems {get; private set; } = new List<TreeViewItemData<ConverterItemState>>();

        private int CountItemWithFlag(Status status)
        {
            int count = 0;
            foreach (ConverterItemState itemState in items)
            {
                if (itemState.conversionResult.Status == status)
                {
                    count++;
                }
            }
            return count;
        }
        public int pending => CountItemWithFlag(Status.Pending);
        public int warnings => CountItemWithFlag(Status.Warning);
        public int errors => CountItemWithFlag(Status.Error);
        public int success => CountItemWithFlag(Status.Success);

        public override string ToString()
        {
            return $"Warnings: {warnings} - Errors: {errors} - Ok: {success} - Total: {items?.Count ?? 0}";
        }

        public void Clear()
        {
            isInitialized = false;
            items.Clear();
            filteredItems.Clear();
        }

        private bool IsVisible(DisplayFilter filter)
        {
            return (currentFilter & filter) == filter;
        }

        internal bool ShouldInclude(ConverterItemState converterItemState)
        {
            return converterItemState.conversionResult.Status switch
            {
                Status.Pending => IsVisible(DisplayFilter.Pending),
                Status.Warning => IsVisible(DisplayFilter.Warnings),
                Status.Error => IsVisible(DisplayFilter.Errors),
                Status.Success => IsVisible(DisplayFilter.Success),
                _ => false
            };
        }

        internal void AddItem(ConverterItemState converterItemState)
        {
            items.Add(converterItemState);
            if (ShouldInclude(converterItemState))
            {
                filteredItems.Add(new TreeViewItemData<ConverterItemState>(filteredItems.Count, converterItemState));
            }
        }

        internal void ApplyFilter()
        {
            filteredItems.Clear();

            foreach (var item in items)
            {
                if (IsVisible((DisplayFilter)(1 << (int)item.conversionResult.Status)))
                    filteredItems.Add(new TreeViewItemData<ConverterItemState>(filteredItems.Count, item));
            }
        }

        public int selectedItemsCount
        {
            get
            {
                int count = 0;
                foreach (ConverterItemState itemState in items)
                {
                    if (itemState.isSelected)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
}
