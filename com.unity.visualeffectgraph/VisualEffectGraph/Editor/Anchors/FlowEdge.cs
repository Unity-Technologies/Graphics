using System;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;

namespace UnityEditor.Experimental
{
    class FlowEdge : Edge<VFXEdFlowAnchor>
    {
        private Color m_edgeColor;
        private Color m_edgeColorSelected;

        public FlowEdge(ICanvasDataSource data, VFXEdFlowAnchor source, VFXEdFlowAnchor target)
            : base(data, source, target)
        {
            m_edgeColor = VFXEditor.styles.GetContextColor(source.context) * VFXEditor.styles.FlowEdgeTint;
            m_edgeColorSelected = VFXEditor.styles.GetContextColor(source.context) * VFXEditor.styles.FlowEdgeSelectedTint;

            caps = Capabilities.Normal;
        }

        public override bool Contains(Vector2 canvasPosition)
        {
            return base.Contains(canvasPosition);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Vector3 from = canvas.ProjectToScreen(m_Left.ConnectPosition());
            Vector3 to = canvas.ProjectToScreen(m_Right.ConnectPosition());
            Orientation orientation = m_Left.GetOrientation();

            Vector3[] points, tangents;
            EdgeConnector<VFXEdFlowAnchor>.GetTangents(m_Left.GetDirection(), orientation, from, to, out points, out tangents);

            if (selected)
            {
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], m_edgeColorSelected, VFXEditor.styles.FlowEdgeOpacity, VFXEditorMetrics.FlowEdgeWidth);
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], Color.white, VFXEditor.styles.FlowEdgeOpacitySelected, VFXEditorMetrics.FlowEdgeWidth);
            }
            else 
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], m_edgeColor, VFXEditor.styles.FlowEdgeOpacity, VFXEditorMetrics.FlowEdgeWidth);

        }

        public static void DrawFlowEdgeConnector(Canvas2D parent, Direction direction, Vector3 from, Vector3 to, Color color)
        {
            Vector3[] points, tangents;
            if (Vector3.Distance(from, to) > 10.0f)
            {
                EdgeConnector<VFXEdFlowAnchor>.GetTangents(direction, Orientation.Vertical, from, to, out points, out tangents);
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
