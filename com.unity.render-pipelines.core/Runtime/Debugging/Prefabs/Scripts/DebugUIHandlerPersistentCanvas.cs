using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.UI
{
    class DebugUIHandlerPersistentCanvas : MonoBehaviour
    {
        public RectTransform panel;
        public RectTransform valuePrefab;
        public RectTransform valueTuplePrefab;

        List<DebugUIHandlerWidget> m_Items = new List<DebugUIHandlerWidget>();

        internal void Toggle(DebugUI.Widget widget)
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
            GameObject go;
            DebugUIHandlerWidget uiHandler;

            if (widget is DebugUI.Value)
            {
                go = Instantiate(valuePrefab, panel, false).gameObject;
                uiHandler = go.GetComponent<DebugUIHandlerValue>();
            }
            else if (widget is DebugUI.ValueTuple)
            {
                go = Instantiate(valueTuplePrefab, panel, false).gameObject;
                uiHandler = go.GetComponent<DebugUIHandlerValueTuplePersistent>();
            }
            else
            {
                throw new NotSupportedException("Unsupported widget type");
            }

            go.name = widget.displayName;
            uiHandler.SetWidget(widget);
            m_Items.Add(uiHandler);
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
