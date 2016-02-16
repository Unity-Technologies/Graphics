using System;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;

namespace UnityEditor.Experimental
{
    class FlowEdge<T> : Edge<T> where T : VFXEdFlowAnchor
    {
        public Color EdgeColor { get { return new Color(m_edgeColor.r,m_edgeColor.g,m_edgeColor.b,0.5f); } }
        public Color EdgeColorSelected { get { return m_edgeColor; } }



        private Color m_edgeColor = Color.gray;
        public FlowEdge(ICanvasDataSource data, T source, T target)
            : base(data, source, target)
        {
            m_edgeColor = VFXEditor.styles.GetContextColor(source.context);
            caps = Capabilities.Normal;
        }

        public override bool Contains(Vector2 canvasPosition)
        {
            return base.Contains(canvasPosition);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Vector3 from = m_Left.ConnectPosition();
            Vector3 to = m_Right.ConnectPosition();
            Orientation orientation = m_Left.GetOrientation();

            Vector3[] points, tangents;
            EdgeConnector<T>.GetTangents(m_Left.GetDirection(), orientation, from, to, out points, out tangents);

            if (selected)
            {
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], EdgeColorSelected, VFXEditor.styles.FlowEdgeOpacity, VFXEditorMetrics.FlowEdgeWidth);
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], Color.white, VFXEditor.styles.FlowEdgeOpacitySelected, VFXEditorMetrics.FlowEdgeWidth);
            }
            else 
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], EdgeColor, VFXEditor.styles.FlowEdgeOpacity, VFXEditorMetrics.FlowEdgeWidth);

        }

        public static void DrawFlowEdgeConnector(Canvas2D parent, Direction direction, Vector3 from, Vector3 to, Color color)
        {
            Vector3[] points, tangents;
            if (Vector3.Distance(from, to) > 10.0f)
            {
                EdgeConnector<T>.GetTangents(direction, Orientation.Vertical, from, to, out points, out tangents);
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], color, VFXEditor.styles.FlowEdgeOpacity, VFXEditorMetrics.FlowEdgeWidth);
            }
        }


        public static void DrawFlowEdgeConnector(Canvas2D parent, IConnect source, IConnect target, Vector3 from, Vector3 to)
        {

            Direction d;
            if (source == null || target == null)
            {
                if (from.y > to.y)
                    d = Direction.Input;
                else
                    d = Direction.Output;
            }
            else
            {
                d = source.GetDirection();
            }

            DrawFlowEdgeConnector(parent, d, from, to, new Color(1.0f, 1.0f, 1.0f, 0.25f));

        }
    }




}
