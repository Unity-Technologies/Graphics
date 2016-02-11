using System;
using UnityEngine;

namespace UnityEditor.Experimental.Graph.Examples
{
    class CustomEdge<T> : Edge<T> where T : CanvasElement, IConnect
    {
        static private Texture2D m_EdgeTexture = null;
        Color edgeColor = Color.white;
        public CustomEdge(ICanvasDataSource data, T source, T target)
            : base(data, source, target)
        {
            if (source.ParentCanvas() != null)
                source.ParentCanvas().Animate(this).Lerp("edgeColor", new Color(0.0f, 0.0f, 0.0f, 0.2f), Color.white, 0.02f);
            m_EdgeTexture = Resources.Load("edge") as Texture2D;
        }


        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Vector3 from = m_Left.ConnectPosition();
            Vector3 to = m_Right.ConnectPosition();
            Orientation orientation = m_Left.GetOrientation();

            Vector3[] points, tangents;
            EdgeConnector<T>.GetTangents(m_Left.GetDirection(), orientation, from, to, out points, out tangents);
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, m_EdgeTexture, 5f);
        }
    }

    class VerticalNode : CanvasElement
    {
        public VerticalNode(Vector2 position, float size, Type outputType, NodalDataSource data)
        {
            m_Translation = position;
            m_Scale = new Vector3(size, size, 1.0f);
            AddManipulator(new Draggable());
            AddChild(new VerticalNodeAnchor(0, new Vector3(size / 2.0f - (NodeAnchor.kNodeSize / 2.0f), 3.0f, 0.0f), outputType, data, Direction.Input));
            AddChild(new VerticalNodeAnchor(0, new Vector3(size / 2.0f - (NodeAnchor.kNodeSize / 2.0f), size - NodeAnchor.kNodeSize - 3.0f, 0.0f), outputType, data, Direction.Output));
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            EditorGUI.DrawRect(new Rect(0, 0, m_Scale.x, m_Scale.y), new Color(0.2f, 0.2f, 1.0f, 0.8f));
            base.Render(parentRect, canvas);
        }
    }

    internal class VerticalNodeAnchor : NodeAnchor
    {
        public VerticalNodeAnchor(int portIndex, Vector3 position, Type type, NodalDataSource data, Direction direction)
            : base(portIndex, position, type, data, DrawCustomEdgeConnector)
        {
            m_Orientation = Orientation.Vertical;
            m_Direction = direction;
        }

        static void DrawCustomEdgeConnector(Canvas2D parent, IConnect source, IConnect target, Vector3 from, Vector3 to)
        {
            Handles.DrawAAPolyLine(15.0f, new[] { from, to });
        }
    }

}
