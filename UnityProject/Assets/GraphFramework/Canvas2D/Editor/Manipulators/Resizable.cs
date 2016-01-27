using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class Resizable : IManipulate
    {
        private bool m_Active;
        private Vector2 m_Start;

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.MouseDown += OnMouseDown;
            element.MouseDrag += OnMouseDrag;
            element.MouseUp += OnMouseUp;
            element.OnWidget += DrawResizeWidget;
        }

        private bool OnMouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            Rect r = element.boundingRect;
            Rect widget = r;
            widget.min = new Vector2(r.max.x - 30.0f, r.max.y - 30.0f);

            if (widget.Contains(parent.MouseToCanvas(e.mousePosition)))
            {
                parent.StartCapture(this, element);
                parent.ClearSelection();
                m_Active = true;
                m_Start = parent.MouseToCanvas(e.mousePosition);
                e.Use();
            }

            return true;
        }

        private bool OnMouseDrag(CanvasElement element, Event e, Canvas2D parent)
        {
            if (!m_Active || e.type != EventType.MouseDrag)
                return false;

            Vector2 newPosition = parent.MouseToCanvas(e.mousePosition);
            Vector2 diff = newPosition - m_Start;
            m_Start = newPosition;
            Vector3 newScale = element.scale;
            newScale.x = Mathf.Max(0.1f, newScale.x + diff.x);
            newScale.y = Mathf.Max(0.1f, newScale.y + diff.y);

            element.scale = newScale;

            element.DeepInvalidate();

            e.Use();
            return true;
        }

        private bool OnMouseUp(CanvasElement element, Event e, Canvas2D parent)
        {
            if (m_Active)
            {
                parent.EndCapture();
                parent.RebuildQuadTree();
            }
            m_Active = false;

            return true;
        }

        private bool DrawResizeWidget(CanvasElement element, Event e, Canvas2D parent)
        {
            GUIStyle style = new GUIStyle("WindowBottomResize");

            Rect r = element.boundingRect;
            Rect widget = r;
            widget.min = new Vector2(r.max.x - 10.0f, r.max.y - 7.0f);
            GUI.Label(widget, GUIContent.none, style);
            return true;
        }
    };
}
