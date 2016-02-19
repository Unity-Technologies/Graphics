using System;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;

namespace UnityEditor.Experimental
{
    class DataEdge : Edge<VFXEdDataAnchor>
    {

        private VFXParam.Type m_Type;
        private Color m_Color;
        private Color m_SelectedColor;
        public DataEdge(ICanvasDataSource data, VFXEdDataAnchor source, VFXEdDataAnchor target)
            : base(data, source, target)
        {
            m_Type = source.ParamType;
            m_SelectedColor = VFXEditor.styles.GetTypeColor(m_Type) * VFXEditor.styles.DataEdgeSelectedTint;
            m_Color = VFXEditor.styles.GetTypeColor(m_Type) * VFXEditor.styles.DataEdgeTint ;

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
            EdgeConnector<VFXEdDataAnchor>.GetTangents(m_Left.GetDirection(), orientation, from, to, out points, out tangents);

            if (selected)
            {
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], m_SelectedColor, VFXEditor.styles.DataEdgeOpacity, VFXEditorMetrics.DataEdgeWidth);
                //Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], Color.white, VFXEditor.styles.DataEdgeOpacitySelected, VFXEditorMetrics.DataEdgeWidth);
            }
            else 
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], m_Color, VFXEditor.styles.DataEdgeOpacity, VFXEditorMetrics.DataEdgeWidth);

        }

        public static void DrawDataEdgeConnector(Canvas2D parent, Direction direction, Vector3 from, Vector3 to, Color color)
        {
            Vector3[] points, tangents;
            if (Vector3.Distance(from, to) > 10.0f)
            {
                EdgeConnector<VFXEdDataAnchor>.GetTangents(direction, Orientation.Horizontal, from, to, out points, out tangents);
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], color, VFXEditor.styles.DataEdgeOpacity, VFXEditorMetrics.DataEdgeWidth);
            }
        }


        public static void DrawDataEdgeConnector(Canvas2D parent, IConnect source, IConnect target, Vector3 from, Vector3 to)
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

            DrawDataEdgeConnector(parent, d, from, to, new Color(1.0f, 1.0f, 1.0f, 0.25f));

        }
    }




}
