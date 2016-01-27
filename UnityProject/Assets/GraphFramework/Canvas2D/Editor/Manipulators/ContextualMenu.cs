using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class ContextualMenu : IManipulate
    {
        private ManipulateDelegate m_Callback;
        private Object m_CustomData;

        public ContextualMenu(ManipulateDelegate callback)
        {
            m_Callback = callback;
            m_CustomData = null;
        }

        public ContextualMenu(ManipulateDelegate callback, Object customData)
        {
            m_Callback = callback;
            m_CustomData = customData;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.ContextClick += OnContextMenu;
        }

        private bool OnContextMenu(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            e.Use();
            return m_Callback(e, parent, m_CustomData);
        }
    };
}
