using System;
using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class RectangleSelect : IManipulate
    {
        private Vector2 m_Start = Vector2.zero;
        private Vector2 m_End = Vector2.zero;
        private bool m_SelectionActive;

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.MouseDown += MouseDown;
            element.MouseUp += MouseUp;
            element.MouseDrag += MouseDrag;
        }

        private bool MouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            parent.ClearSelection();
            if (e.button == 0)
            {
                element.OnWidget += DrawSelection;
                m_Start = parent.MouseToCanvas(e.mousePosition);
                m_End = m_Start;
                m_SelectionActive = true;
                e.Use();
                return true;
            }
            return false;
        }

        private bool MouseUp(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            bool handled = false;

            if (m_SelectionActive)
            {
                element.OnWidget -= DrawSelection;
                m_End = parent.MouseToCanvas(e.mousePosition);

                Rect selection = new Rect();
                selection.min = new Vector2(Math.Min(m_Start.x, m_End.x), Math.Min(m_Start.y, m_End.y));
                selection.max = new Vector2(Math.Max(m_Start.x, m_End.x), Math.Max(m_Start.y, m_End.y));

                selection.width = Mathf.Max(selection.width, 5.0f);
                selection.height = Mathf.Max(selection.height, 5.0f);

                foreach (CanvasElement child in parent.elements)
                {
                    if (child.Intersects(selection))
                    {
                        parent.AddToSelection(child);
                    }
                }

                handled = true;
                e.Use();
            }
            m_SelectionActive = false;

            return handled;
        }

        private bool MouseDrag(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.button == 0)
            {
                m_End = parent.MouseToCanvas(e.mousePosition);
                e.Use();
                return true;
            }

            return false;
        }

        private bool DrawSelection(CanvasElement element, Event e, Canvas2D parent)
        {
            if (!m_SelectionActive)
                return false;

            Rect r = new Rect();
            r.min = new Vector2(Math.Min(m_Start.x, m_End.x), Math.Min(m_Start.y, m_End.y));
            r.max = new Vector2(Math.Max(m_Start.x, m_End.x), Math.Max(m_Start.y, m_End.y));

            Color lineColor = new Color(1.0f, 0.6f, 0.0f, 1.0f);
            float segmentSize = 5f;

            Vector3[] points =
            {
                new Vector3(r.xMin, r.yMin, 0.0f),
                new Vector3(r.xMax, r.yMin, 0.0f),
                new Vector3(r.xMax, r.yMax, 0.0f),
                new Vector3(r.xMin, r.yMax, 0.0f)
            };

            DrawDottedLine(points[0], points[1], segmentSize, lineColor);
            DrawDottedLine(points[1], points[2], segmentSize, lineColor);
            DrawDottedLine(points[2], points[3], segmentSize, lineColor);
            DrawDottedLine(points[3], points[0], segmentSize, lineColor);

            return true;
        }

        private void DrawDottedLine(Vector3 p1, Vector3 p2, float segmentsLength, Color col)
        {
            UIHelpers.ApplyWireMaterial();

            GL.Begin(GL.LINES);
            GL.Color(col);

            float length = Vector3.Distance(p1, p2); // ignore z component
            int count = Mathf.CeilToInt(length / segmentsLength);
            for (int i = 0; i < count; i += 2)
            {
                GL.Vertex((Vector3.Lerp(p1, p2, i * segmentsLength / length)));
                GL.Vertex((Vector3.Lerp(p1, p2, (i + 1) * segmentsLength / length)));
            }

            GL.End();
        }
    };
}
