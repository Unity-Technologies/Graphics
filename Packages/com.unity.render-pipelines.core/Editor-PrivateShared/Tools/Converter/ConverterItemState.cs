using System;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    // This is the serialized class that stores the state of each item in the list of items to convert
    [Serializable]
    class ConverterItemState
    {
        public Action<bool> onIsSelectedChanged;
        private bool m_IsSelected;
        public bool isSelected
        {
            get => m_IsSelected;
            set
            {
                if (m_IsSelected != value)
                {
                    m_IsSelected = value;
                    onIsSelectedChanged?.Invoke(m_IsSelected);
                }
            }
        }
        public IRenderPipelineConverterItem item;
        [NonSerialized]
        public (Status Status, string Message) conversionResult = (Status.Pending, string.Empty);
        internal bool hasConverted => conversionResult.Status != Status.Pending;

        public void OnSelectionChanged(ClickEvent _)
        {
            isSelected = !isSelected;
        }

        public void OnClicked(ClickEvent _)
        {
            item.OnClicked();
        }
    }
}
