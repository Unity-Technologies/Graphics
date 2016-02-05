using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Graph
{
    internal class EdgeConnector<T> : IManipulate where T : IConnect
    {
        private static readonly Color s_EdgeColor = new Color(1.0f, 1.0f, 1.0f, 0.8f);
        private static readonly Color s_ActiveEdgeColor = new Color(0.2f, 0.4f, 1.0f, 0.8f);

        private Vector2 m_Start = Vector2.zero;
        private Vector2 m_End = Vector2.zero;
        private Color m_Color = s_EdgeColor;
        private IConnect m_SnappedTarget;
        private IConnect m_SnappedSource;

        private List<IConnect> m_CompatibleAnchors = new List<IConnect>();

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.MouseUp += EndDrag;
            element.MouseDown += StartDrag;
            element.MouseDrag += MouseDrag;
        }

        private bool StartDrag(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
            {
                return false;
            }

            if (e.button != 0)
            {
                return false;
            }

            element.OnWidget += DrawEdge;

            if (element.collapsed)
                return false;

            canvas.StartCapture(this, element);
            m_Start = m_End = element.canvasBoundingRect.center;

            e.Use();

            IConnect cnx = element as IConnect;
            if (cnx != null)
            {
                cnx.Highlight(true);
            }
            EndSnap();

            // find compatible anchors
            m_CompatibleAnchors.Clear();

            Rect screenRect = new Rect
            {
                min = canvas.MouseToCanvas(new Vector2(0.0f, 0.0f)),
                max = canvas.MouseToCanvas(new Vector2(Screen.width, Screen.height))
            };

            CanvasElement[] visibleAnchors = canvas.Pick<T>(screenRect);
            NodeAdapter nodeAdapter = new NodeAdapter();
            foreach (CanvasElement anchor in visibleAnchors)
            {
                IConnect toCnx = anchor as IConnect;
                if (toCnx == null)
                    continue;

                bool isBidirectional = ((cnx.GetDirection() == Direction.Bidirectional) ||
                                        (toCnx.GetDirection() == Direction.Bidirectional));

                if (cnx.GetDirection() != toCnx.GetDirection() || isBidirectional)
                {
                    if (nodeAdapter.GetAdapter(cnx.Source(), toCnx.Source()) != null)
                    {
                        m_CompatibleAnchors.Add(toCnx);
                    }
                }
            }

            canvas.OnOverlay += HighlightCompatibleAnchors;

            return true;
        }

        private bool EndDrag(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
                return false;

            if (!canvas.IsCaptured(this))
            {
                return false;
            }

            element.OnWidget -= DrawEdge;

            canvas.EndCapture();
            IConnect cnx = element as IConnect;
            if (cnx != null)
            {
                cnx.Highlight(false);
            }

            if (m_SnappedSource == null && m_SnappedTarget == null)
            {
                cnx.OnConnect(null);
            }
            else if (m_SnappedSource != null && m_SnappedTarget != null)
            {
                NodeAdapter nodeAdapter = new NodeAdapter();
                if (nodeAdapter.CanAdapt(m_SnappedSource.Source(), m_SnappedTarget.Source()))
                {
                    nodeAdapter.Connect(m_SnappedSource.Source(), m_SnappedTarget.Source());
                    cnx.OnConnect(m_SnappedTarget);
                }
            }

            EndSnap();
            e.Use();
            canvas.OnOverlay -= HighlightCompatibleAnchors;
            return true;
        }

        private bool MouseDrag(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
            {
                return false;
            }

            if (!canvas.IsCaptured(this))
            {
                return false;
            }

            m_End = canvas.MouseToCanvas(e.mousePosition);
            e.Use();

            m_Color = s_EdgeColor;

            IConnect thisCnx = element as IConnect;
            // find target anchor under us
            CanvasElement elementUnderMouse = canvas.PickSingle<T>(e.mousePosition);
            if (elementUnderMouse != null)
            {
                IConnect cnx = elementUnderMouse as IConnect;
                if (cnx == null)
                {
                    Debug.LogError("PickSingle returned an incompatible element: does not support IConnect interface");
                    return true;
                }

                if (m_CompatibleAnchors.Exists(ic => ic == cnx))
                {
                    StartSnap(thisCnx, cnx);
                    m_Color = s_ActiveEdgeColor;
                }
            }
            else
            {
                EndSnap();
            }

            return true;
        }

        private void StartSnap(IConnect from, IConnect to)
        {
            EndSnap();
            m_SnappedTarget = to;
            m_SnappedSource = from;
            m_SnappedTarget.Highlight(true);
        }

        private void EndSnap()
        {
            if (m_SnappedTarget != null)
            {
                m_SnappedTarget.Highlight(false);
                m_SnappedTarget = null;
            }
        }

        private bool DrawEdge(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (!canvas.IsCaptured(this))
            {
                return false;
            }

            bool invert = m_End.x < m_Start.x;
            Vector3[] points, tangents;
            GetTangents(invert ? m_End : m_Start, invert ? m_Start : m_End, out points, out tangents);
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], m_Color, null, 5f);

            // little widget on the middle of the edge
            Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);
            Color oldColor = Handles.color;
            Handles.color = m_Color;
            Handles.DrawSolidDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 6f);
            Handles.color = oldColor;
            return true;
        }

        private bool HighlightCompatibleAnchors(CanvasElement element, Event e, Canvas2D canvas)
        {
            foreach (IConnect visible in m_CompatibleAnchors)
            {
                visible.RenderOverlay(canvas);
            }
            return false;
        }

        public static void GetTangents(Vector2 start, Vector2 end, out Vector3[] points, out Vector3[] tangents)
        {
            points = new Vector3[] {start, end};
            tangents = new Vector3[2];

            const float minTangent = 30;

            float weight = (start.y < end.y) ? .3f : .7f;
            weight = .5f;
            float weight2 = 1 - weight;
            float y = 0;

            if (start.x > end.x)
            {
                weight2 = weight = -.25f;
                float aspect = (start.x - end.x) / (start.y - end.y);
                if (Mathf.Abs(aspect) > .5f)
                {
                    float asp = (Mathf.Abs(aspect) - .5f) / 8;
                    asp = Mathf.Sqrt(asp);
                    y = Mathf.Min(asp * 80, 80);
                    if (start.y > end.y)
                        y = -y;
                }
            }
            float cleverness = Mathf.Clamp01(((start - end).magnitude - 10) / 50);

            tangents[0] = start + new Vector2((end.x - start.x) * weight + minTangent, y) * cleverness;
            tangents[1] = end + new Vector2((end.x - start.x) * -weight2 - minTangent, -y) * cleverness;
        }
    };
}
