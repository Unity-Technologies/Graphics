using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEditorInternal.Experimental;

//#pragma warning disable 0414
//#pragma warning disable 0219

namespace UnityEditor.Experimental.Graph
{
    internal class Edge<T> : CanvasElement where T : CanvasElement, IConnect
    {
        private T m_Left;
        private T m_Right;
        private ICanvasDataSource m_Data;

        public Edge(ICanvasDataSource data, T left, T right)
        {
            m_Data = data;
            zIndex = 9999;
            m_SupportsRenderToTexture = false;
            left.AddDependency(this);
            right.AddDependency(this);
            m_Left = left;
            m_Right = right;

            UpdateModel(UpdateType.Update);

            KeyDown += OnDeleteEdge;
        }

        public T Left
        {
            get { return m_Left; }
        }

        public T Right
        {
            get { return m_Right; }
        }

        private bool OnDeleteEdge(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.keyCode == KeyCode.Delete)
            {
                m_Data.DeleteElement(this);
                return true;
            }
            return false;
        }

        public override bool Intersects(Rect rect)
        {
            // first check coarse bounding box
            if (!base.Intersects(rect))
                return false;

            // bounding box check succeeded, do more fine grained check by checking intersection between the rectangles' diagonal
            // and the line segments

            Vector3 from = m_Left.ConnectPosition();
            Vector3 to = m_Right.ConnectPosition();

            if (to.x < from.x)
            {
                Vector3 t = from;
                from = to;
                to = t;
            }

            Vector3[] points, tangents;
            EdgeConnector<T>.GetTangents(from, to, out points, out tangents);
            Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

            for (int a = 0; a < allPoints.Length; a++)
            {
                if (a >= allPoints.Length - 1)
                {
                    break;
                }

                Vector2 segmentA = new Vector2(allPoints[a].x, allPoints[a].y);
                Vector2 segmentB = new Vector2(allPoints[a + 1].x, allPoints[a + 1].y);

                if (RectUtils.IntersectsSegment(rect, segmentA, segmentB))
                    return true;
            }

            return false;
        }

        public override bool Contains(Vector2 canvasPosition)
        {
            // first check coarse bounding box
            if (!base.Contains(canvasPosition))
                return false;

            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points

            Vector3 from = m_Left.ConnectPosition();
            Vector3 to = m_Right.ConnectPosition();

            if (to.x < from.x)
            {
                Vector3 t = from;
                from = to;
                to = t;
            }

            Vector3[] points, tangents;
            EdgeConnector<T>.GetTangents(from, to, out points, out tangents);
            Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

            float minDistance = Mathf.Infinity;
            foreach (Vector3 currentPoint in allPoints)
            {
                float distance = Vector3.Distance(currentPoint, canvasPosition);
                minDistance = Mathf.Min(minDistance, distance);
                if (minDistance < 15.0f)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Color edgeColor = selected ? Color.yellow : Color.white;

            Vector3 from = m_Left.ConnectPosition();
            Vector3 to = m_Right.ConnectPosition();

            if (to.x < from.x)
            {
                Vector3 t = from;
                from = to;
                to = t;
            }

            Vector3[] points, tangents;
            EdgeConnector<T>.GetTangents(from, to, out points, out tangents);
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 5f);

            // little widget on the middle of the edge
            Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);
            Color oldColor = Handles.color;
            Handles.color = Color.blue;
            Handles.DrawSolidDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 6f);
            Handles.color = edgeColor;
            Handles.DrawWireDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 6f);
            Handles.DrawWireDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 5f);

            // dot on top of anchor showing it's connected
            Handles.color = new Color(0.3f, 0.4f, 1.0f, 1.0f);
            Handles.DrawSolidDisc(from, new Vector3(0.0f, 0.0f, -1.0f), 4f);
            Handles.DrawSolidDisc(to, new Vector3(0.0f, 0.0f, -1.0f), 4f);

            /*if (EditorApplication.isPlaying)
                    {
                        Handles.color = Color.red;
                        Handles.DrawSolidDisc(allPoints[m_RealtimeFeedbackPointIndex], new Vector3(0.0f, 0.0f, -1.0f), 6f);

                        m_RealtimeFeedbackPointIndex++;
                        if (m_RealtimeFeedbackPointIndex >= 20)
                        {
                            m_RealtimeFeedbackPointIndex = 0;
                        }
                    }*/
            Handles.color = oldColor;
        }

        public override void UpdateModel(UpdateType t)
        {
            Vector3 from = m_Left.ConnectPosition();
            Vector3 to = m_Right.ConnectPosition();

            Rect r = new Rect();
            r.min = new Vector2(Math.Min(from.x, to.x), Math.Min(from.y, to.y));
            r.max = new Vector2(Math.Max(from.x, to.x), Math.Max(from.y, to.y));

            translation = r.min;
            scale = new Vector3(r.width, r.height, 1.0f);

            base.UpdateModel(t);
        }
    }
}
