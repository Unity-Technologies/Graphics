using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.UI
{
    class DebugUIHandlerPersistentCanvas : MonoBehaviour
    {
        public RectTransform panel;
        public RectTransform valuePrefab;

        List<DebugUIHandlerValue> m_Items = new List<DebugUIHandlerValue>();

        // Toggles persistent value widget on/off.
        internal void Toggle(DebugUI.Value widget, string displayName = null)
        {
            int index = m_Items.FindIndex(x => x.GetWidget() == widget);

            // Remove
            if (index > -1)
            {
                var item = m_Items[index];
                CoreUtils.Destroy(item.gameObject);
                m_Items.RemoveAt(index);
                return;
            }

            // Add
            var go = Instantiate(valuePrefab, panel, false).gameObject;
            var uiHandler = go.GetComponent<DebugUIHandlerValue>();
            uiHandler.SetWidget(widget);
            uiHandler.nameLabel.text = string.IsNullOrEmpty(displayName) ? widget.displayName : displayName;
            m_Items.Add(uiHandler);
        }

        List<DebugUI.ValueTuple> m_ValueTupleWidgets = new();

        // For ValueTuples (multiple values on one row), we cycle through the columns, and turn the widget
        // off after the last column.
        internal void Toggle(DebugUI.ValueTuple widget, int? forceTupleIndex = null)
        {
            var val = m_ValueTupleWidgets.Find(x => x == widget);
            int tupleIndex = val?.pinnedElementIndex ?? -1;

            // Clear old widget
            if (val != null)
            {
                m_ValueTupleWidgets.Remove(val);
                Toggle(widget.values[tupleIndex]);
            }

            if (forceTupleIndex != null)
                tupleIndex = forceTupleIndex.Value;

            // Enable next widget (unless at the last index)
            if (tupleIndex + 1 < widget.numElements)
            {
                widget.pinnedElementIndex = tupleIndex + 1;
                // Add column to name
                string displayName = widget.displayName;
                if (widget.parent is DebugUI.Foldout)
                {
                    var columnLabels = (widget.parent as DebugUI.Foldout).columnLabels;
                    if (columnLabels != null && widget.pinnedElementIndex < columnLabels.Length)
                    {
                        displayName += $" ({columnLabels[widget.pinnedElementIndex]})";
                    }
                }

                Toggle(widget.values[widget.pinnedElementIndex], displayName);
                m_ValueTupleWidgets.Add(widget);
            }
            else
            {
                widget.pinnedElementIndex = -1;
            }
        }

        internal bool IsEmpty()
        {
            return m_Items.Count == 0;
        }

        internal void Clear()
        {
            foreach (var item in m_Items)
                CoreUtils.Destroy(item.gameObject);

            m_Items.Clear();
        }
    }
}
