using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    internal class BezierSubdiv
    {
        public static void GetBezierSubDiv(List<Vector2> points, Vector2 start, Vector2 end, Vector2 tStart, Vector2 tEnd)
        {
            points.Clear();

            points.Add(start);

            AddBezierRecurse(points, start, tStart, tEnd, end, 0);

            points.Add(end);
        }

        const float curve_collinearity_epsilon = 1e-5f;
        const float curve_angle_tolerance_epsilon = 1e-5f;

        const float distance_tolerance = 2.0f;
        const float distance_tolerance_square = distance_tolerance * distance_tolerance;
        const float angle_tolerance = 0.01f;
        const float cusp_limit = 0.00f;

        static void AddBezierRecurse(List<Vector2> points, Vector2 start, Vector2 tStart, Vector2 tEnd, Vector2 end, int level)
        {
            if (level > 20)
            {
                return;
            }

            float x1 = start.x;
            float y1 = start.y;
            float x2 = tStart.x;
            float y2 = tStart.y;
            float x3 = tEnd.x;
            float y3 = tEnd.y;
            float x4 = end.x;
            float y4 = end.y;
            // Calculate all the mid-points of the line segments
            //----------------------
            float x12 = (x1 + x2) / 2;
            float y12 = (y1 + y2) / 2;
            float x23 = (x2 + x3) / 2;
            float y23 = (y2 + y3) / 2;
            float x34 = (x3 + x4) / 2;
            float y34 = (y3 + y4) / 2;
            float x123 = (x12 + x23) / 2;
            float y123 = (y12 + y23) / 2;
            float x234 = (x23 + x34) / 2;
            float y234 = (y23 + y34) / 2;
            float x1234 = (x123 + x234) / 2;
            float y1234 = (y123 + y234) / 2;

            if (level > 0) // Enforce subdivision first time
            {
                // Try to approximate the full cubic curve by a single straight line
                //------------------
                float dx = x4 - x1;
                float dy = y4 - y1;

                float d2 = Mathf.Abs(((x2 - x4) * dy - (y2 - y4) * dx));
                float d3 = Mathf.Abs(((x3 - x4) * dy - (y3 - y4) * dx));

                if ((d2 + d3) * (d2 + d3) <= distance_tolerance * (dx * dx + dy * dy))
                {
                    points.Add(new Vector2(x1234, y1234));
                    return;
                }
            }

            // Continue subdivision
            //----------------------
            AddBezierRecurse(points, new Vector2(x1, y1), new Vector2(x12, y12), new Vector2(x123, y123), new Vector2(x1234, y1234), level + 1);
            AddBezierRecurse(points, new Vector2(x1234, y1234), new Vector2(x234, y234), new Vector2(x34, y34), new Vector2(x4, y4), level + 1);
        }
    }


    internal class VFXEdgeControl : EdgeControl
    {
        Vector3[] m_PrevControlPoints;
        float m_PrevRealWidth = Mathf.Infinity;

        Mesh m_Mesh;


        public new int edgeWidth
        {
            get
            {
                Edge edge = this.GetFirstAncestorOfType<Edge>();


                return edge.edgeWidth;
            }
        }

        Color m_InputColor = Color.grey;
        Color m_OutputColor = Color.grey;

        public Color inputColor
        {
            get
            {
                return m_InputColor;
            }
            set
            {
                if (m_InputColor != value)
                {
                    m_InputColor = value;
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        public Color outputColor
        {
            get
            {
                return m_OutputColor;
            }
            set
            {
                if (m_OutputColor != value)
                {
                    m_OutputColor = value;
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        protected virtual bool selected
        {
            get
            {
                Edge edge = this.GetFirstAncestorOfType<Edge>();
                EdgePresenter edgePresenter = edge.GetPresenter<EdgePresenter>();
                //VFXDataAnchorPresenter inputPresenter = edgePresenter.input as VFXDataAnchorPresenter;

                return edgePresenter.selected;
            }
        }


        public override bool Overlaps(Rect rect)
        {
            if (m_Mesh == null)
                return false;

            Vector3[] meshPoints = m_Mesh.vertices;

            for (int i = 0; i < meshPoints.Length / 2 - 1; ++i)
            {
                Vector3 a = (meshPoints[i * 2] + meshPoints[i * 2 + 1]) * 0.5f;
                Vector3 b = (meshPoints[(i + 1) * 2] + meshPoints[(i + 1) * 2 + 1]) * 0.5f;

                if (RectUtils.IntersectsSegment(rect, a, b))
                    return true;
            }

            return false;
        }

        List<Vector2> m_CurvePoints = new List<Vector2>();


        protected override void PointsChanged()
        {
            base.PointsChanged();
            VFXEdge edge = this.GetFirstAncestorOfType<VFXEdge>();
            if (edge != null)
                edge.OnDisplayChanged();
        }

        const float MinEdgeWidth = 1.75f;


        // This method should feed m_CurvePoints list with the wanted points. It will only be called if one of the control points changed.
        public virtual void ComputePolyLine()
        {
            Vector3 start = controlPoints[0];
            Vector3 tStart = controlPoints[1];
            Vector3 end = controlPoints[3];
            Vector3 tEnd = controlPoints[2];

            BezierSubdiv.GetBezierSubDiv(m_CurvePoints, start, end, tStart, tEnd);
        }

        protected override void DrawEdge()
        {
            Vector3[] points = controlPoints;

            Color inputColor = this.inputColor;
            Color outputColor = this.outputColor;

            GraphView view = this.GetFirstAncestorOfType<GraphView>();

            float realWidth = edgeWidth;
            if (realWidth * view.scale < MinEdgeWidth)
            {
                realWidth = MinEdgeWidth / view.scale;

                inputColor.a = outputColor.a = edgeWidth / realWidth;
            }

            if (m_PrevControlPoints == null
                || (m_PrevControlPoints[0] - points[0]).sqrMagnitude > 0.25
                || (m_PrevControlPoints[1] - points[1]).sqrMagnitude > 0.25
                || (m_PrevControlPoints[2] - points[2]).sqrMagnitude > 0.25
                || (m_PrevControlPoints[3] - points[3]).sqrMagnitude > 0.25
                || edgeWidth != m_PrevRealWidth
                || m_Mesh == null)
            {
                m_PrevControlPoints = (Vector3[])points.Clone();
                m_PrevRealWidth = realWidth;

                Vector3 start = points[0];
                Vector3 tStart = points[1];
                Vector3 end = points[3];
                Vector3 tEnd = points[2];

                ComputePolyLine();

                int cpt = m_CurvePoints.Count;


                float polyLineLength = 0;

                for (int i = 1; i < cpt; ++i)
                {
                    polyLineLength += (m_CurvePoints[i - 1] - m_CurvePoints[i]).magnitude;
                }

                if (m_Mesh == null)
                {
                    m_Mesh = new Mesh();
                    m_Mesh.hideFlags = HideFlags.HideAndDontSave;
                }


                Vector3[] vertices = m_Mesh.vertices;
                Vector2[] uvs = m_Mesh.uv;
                Vector3[] normals = m_Mesh.normals;
                bool newIndices = false;
                int wantedLength = (cpt) * 2;
                if (vertices == null || vertices.Length != wantedLength)
                {
                    vertices = new Vector3[wantedLength];
                    uvs = new Vector2[wantedLength];
                    normals = new Vector3[wantedLength];
                    newIndices = true;
                    m_Mesh.triangles = new int[] {};
                }

                float halfWidth = edgeWidth * 0.5f;

                float vertexHalfWidth = halfWidth + 2;


                float currentLength = 0;

                for (int i = 0; i < cpt; ++i)
                {
                    Vector2 dir;
                    if (i > 0 && i < cpt - 1)
                    {
                        dir = (m_CurvePoints[i] - m_CurvePoints[i - 1]).normalized + (m_CurvePoints[i + 1] - m_CurvePoints[i]).normalized;
                        dir.Normalize();
                    }
                    else if (i > 0)
                    {
                        dir = (m_CurvePoints[i] - m_CurvePoints[i - 1]).normalized;
                    }
                    else
                    {
                        dir = (m_CurvePoints[i + 1] - m_CurvePoints[i]).normalized;
                    }


                    Vector2 norm = new Vector3(dir.y, -dir.x, 0);


                    Vector2 border = -norm * vertexHalfWidth;

                    uvs[i * 2] = new Vector2(-vertexHalfWidth, halfWidth);
                    vertices[i * 2] = m_CurvePoints[i];
                    normals[i * 2] = new Vector3(-border.x, -border.y, currentLength / polyLineLength);

                    uvs[i * 2 + 1] = new Vector2(vertexHalfWidth, halfWidth);
                    vertices[i * 2 + 1] = m_CurvePoints[i];
                    normals[i * 2 + 1] = new Vector3(border.x, border.y, currentLength / polyLineLength);


                    if (i < cpt - 2)
                    {
                        currentLength += (m_CurvePoints[i + 1] - m_CurvePoints[i]).magnitude;
                    }
                    else
                    {
                        currentLength = polyLineLength;
                    }
                }

                m_Mesh.vertices = vertices;
                m_Mesh.normals = normals;
                m_Mesh.uv = uvs;

                if (newIndices)
                {
                    //fill triangle indices as it is a triangle strip
                    int[] indices = new int[(wantedLength - 2) * 3];


                    for (int i = 0; i < wantedLength - 2; ++i)
                    {
                        if ((i % 2) == 0)
                        {
                            indices[i * 3] = i;
                            indices[i * 3 + 1] = i + 1;
                            indices[i * 3 + 2] = i + 2;
                        }
                        else
                        {
                            indices[i * 3] = i + 1;
                            indices[i * 3 + 1] = i;
                            indices[i * 3 + 2] = i + 2;
                        }
                    }

                    m_Mesh.triangles = indices;
                }

                m_Mesh.RecalculateBounds();
            }


            VFXEdgeUtils.lineMat.SetFloat("_ZoomFactor", view.scale * realWidth / edgeWidth);
            VFXEdgeUtils.lineMat.SetFloat("_ZoomCorrection", realWidth / edgeWidth);
            VFXEdgeUtils.lineMat.SetColor("_InputColor", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? inputColor.gamma : inputColor);
            VFXEdgeUtils.lineMat.SetColor("_OutputColor", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? outputColor.gamma : outputColor);
            VFXEdgeUtils.lineMat.SetPass(0);

            Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (m_Mesh != null)
            {
                Object.DestroyImmediate(m_Mesh);
                m_Mesh = null;
            }
        }

        public VFXEdgeControl()
        {
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }
    }
}
