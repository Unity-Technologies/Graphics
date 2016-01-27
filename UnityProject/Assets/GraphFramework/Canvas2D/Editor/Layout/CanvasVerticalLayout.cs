using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal.Experimental;
using UnityEngine;

namespace UnityEditor.Experimental
{
    /// <summary>
    /// CanvasVerticalLayout : Helps layouting a group of canvas elements vertically
    /// </summary>
    internal class CanvasVerticalLayout : CanvasLayout
    {
        public CanvasVerticalLayout(CanvasElement e)
            : base(e)
        {
        }

        public override void LayoutElements(CanvasElement[] arrayOfElements)
        {
            for (int a = 0; a < arrayOfElements.Length; a++)
            {
                LayoutElement(arrayOfElements[a]);
            }
            base.LayoutElements(arrayOfElements);
        }

        public override void LayoutElement(CanvasElement c)
        {
            float collapsedFactor = m_Owner.IsCollapsed() ? 0.0f : 1.0f;
            if ((c.caps & CanvasElement.Capabilities.DoesNotCollapse) == CanvasElement.Capabilities.DoesNotCollapse)
            {
                collapsedFactor = 1.0f;
            }

            m_Height += m_Padding.x * collapsedFactor;
            //c.translation = new Vector3(m_Padding.y + c.translation.x, height*collapsedFactor, 0.0f);
            c.translation = new Vector3(c.translation.x, height * collapsedFactor, 0.0f);
            c.scale = new Vector3(c.scale.x, c.scale.y * collapsedFactor, 1.0f);
            m_Height += (c.scale.y + m_Padding.z) * collapsedFactor;
            m_Width = Mathf.Max(m_Width, c.scale.x);
            Owner().AddChild(c);
            base.LayoutElement(c);
        }

        public override void DebugDraw()
        {
            if (m_Elements.Count() == 0)
                return;

            Rect encompassingRect = m_Elements[0].canvasBoundingRect;
            List<Rect> elementRects = new List<Rect>();
            foreach (CanvasElement e in m_Elements)
            {
                elementRects.Add(e.canvasBoundingRect);
                encompassingRect = RectUtils.Encompass(encompassingRect, e.canvasBoundingRect);
            }

            Vector3[] points =
            {
                new Vector3(encompassingRect.xMin, encompassingRect.yMin, 0.0f),
                new Vector3(encompassingRect.xMax, encompassingRect.yMin, 0.0f),
                new Vector3(encompassingRect.xMax, encompassingRect.yMax, 0.0f),
                new Vector3(encompassingRect.xMin, encompassingRect.yMax, 0.0f)
            };

            Color prevColor = GUI.color;
            GUI.color = new Color(1.0f, 0.6f, 0.0f, 1.0f);
            Handles.DrawDottedLine(points[0], points[1], 5.0f);
            Handles.DrawDottedLine(points[1], points[2], 5.0f);
            Handles.DrawDottedLine(points[2], points[3], 5.0f);
            Handles.DrawDottedLine(points[3], points[0], 5.0f);
            GUI.color = prevColor;

            foreach (Rect r in elementRects)
            {
                Vector2 from = new Vector2(r.xMin, r.yMax);
                Vector2 to = new Vector2(encompassingRect.xMax, r.yMax);

                DrawDottedLine(from, to, 5.0f, new Color(1.0f, 0.6f, 0.0f, 1.0f));
            }
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
