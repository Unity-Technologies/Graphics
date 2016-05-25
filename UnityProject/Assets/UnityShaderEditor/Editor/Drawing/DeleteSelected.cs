using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    internal class DeleteSelected : IManipulate
    {
        public delegate void DeleteElements(List<CanvasElement> elements);

        private readonly DeleteElements m_DeletionCallback;
        private readonly Canvas2D m_Canvas;

        public DeleteSelected(DeleteElements deletionCallback, Canvas2D canvas)
        {
            m_DeletionCallback = deletionCallback;
            m_Canvas = canvas;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.KeyDown += KeyDown;
        }

        private bool KeyDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.keyCode == KeyCode.Delete)
            {
                if (m_DeletionCallback != null)
                {
                    m_DeletionCallback(parent.selection);
                    m_Canvas.ReloadData();
                    m_Canvas.Repaint();
                    return true;
                }
            }
            return false;
        }
    }
}
