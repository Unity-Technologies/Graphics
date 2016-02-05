using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class DragDrop : IManipulate
    {
        private ManipulateDelegate m_Callback;
        private Object m_CustomData;

        public DragDrop(ManipulateDelegate callback)
        {
            m_Callback = callback;
            m_CustomData = null;
        }

        public DragDrop(ManipulateDelegate callback, Object customData)
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
            element.DragPerform += OnDragAndDropEvent;
            element.DragUpdated += OnDragAndDropEvent;
            element.DragExited += OnDragAndDropEvent;
        }

        private bool OnDragAndDropEvent(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            return m_Callback(e, parent, m_CustomData);
        }
    };
}
