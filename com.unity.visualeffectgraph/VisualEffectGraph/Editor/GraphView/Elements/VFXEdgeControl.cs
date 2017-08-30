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
        Color32 m_PrevColor;

        Mesh m_Mesh;


        public new int edgeWidth
        {
            get
            {
                Edge edge = this.GetFirstAncestorOfType<Edge>();


                return edge.edgeWidth;
            }
        }

        protected new virtual Color edgeColor
        {
            get
            {
                Edge edge = this.GetFirstAncestorOfType<Edge>();

                return edge.style.borderColor;
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


        protected override void DrawEdge()
        {
            Vector3[] points = controlPoints;

            Color edgeColor = this.edgeColor;

            GraphView view = this.GetFirstAncestorOfType<GraphView>();

            float realWidth = selected ? edgeWidth * 2 : edgeWidth;
            if (realWidth * view.scale < 1.5f)
            {
                realWidth = 1.5f / view.scale;
            }

            if (m_PrevControlPoints == null
                || (m_PrevControlPoints[0] - points[0]).sqrMagnitude > 0.25
                || (m_PrevControlPoints[1] - points[1]).sqrMagnitude > 0.25
                || (m_PrevControlPoints[2] - points[2]).sqrMagnitude > 0.25
                || (m_PrevControlPoints[3] - points[3]).sqrMagnitude > 0.25
                || realWidth != m_PrevRealWidth)
            {
                m_PrevControlPoints = (Vector3[])points.Clone();
                m_PrevRealWidth = realWidth;

                Vector3 start = points[0];
                Vector3 tStart = points[1];
                Vector3 end = points[3];
                Vector3 tEnd = points[2];


                List<Vector2> pointResult = new List<Vector2>();
                BezierSubdiv.GetBezierSubDiv(pointResult, start, end, tStart, tEnd);

                int cpt = pointResult.Count;

                if (m_Mesh == null)
                    m_Mesh = new Mesh();


                Vector3[] vertices = m_Mesh.vertices;
                Vector2[] uvs = m_Mesh.uv;
                bool newIndices = false;
                int wantedLength = (cpt) * 2;
                if (vertices == null || vertices.Length != wantedLength)
                {
                    vertices = new Vector3[wantedLength];
                    uvs = new Vector2[wantedLength];
                    newIndices = true;
                    m_Mesh.triangles = new int[] {};
                }

                float halfWidth = realWidth * 0.5f + 0.5f;

                float vertexHalfWidth = halfWidth + 2;
                int index;

                for (int i = 0; i < cpt; ++i)
                {
                    Vector2 dir;
                    if (i > 0 && i < cpt - 1)
                    {
                        dir = (pointResult[i] - pointResult[i - 1]).normalized + (pointResult[i + 1] - pointResult[i]).normalized;
                        dir.Normalize();
                    }
                    else if (i > 0)
                    {
                        dir = (pointResult[i] - pointResult[i - 1]).normalized;
                    }
                    else
                    {
                        dir = (pointResult[i + 1] - pointResult[i]).normalized;
                    }


                    Vector2 norm = new Vector3(dir.y, -dir.x, 0);


                    Vector2 border = -norm * vertexHalfWidth;

                    uvs[i * 2] = new Vector2(-vertexHalfWidth, halfWidth);
                    vertices[i * 2] = pointResult[i] - border;

                    uvs[i * 2 + 1] = new Vector2(vertexHalfWidth, halfWidth);
                    vertices[i * 2 + 1] = pointResult[i] + border;
                }

                m_Mesh.vertices = vertices;
                m_Mesh.uv = uvs;

                Color32 color32 = edgeColor;
                if (newIndices || !m_PrevColor.Equals(color32))
                {
                    m_PrevColor = color32;
                    Color32[] colors = new Color32[wantedLength];
                    for (int i = 0; i < wantedLength; ++i)
                    {
                        colors[i] = color32;
                    }
                    m_Mesh.colors32 = colors;
                }

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


            /*
            Vector3 trueEnd = points[3];
            if (inputPresenter != null && inputPresenter.sourceNode is VFXBlockPresenter)
            {
                trueEnd = trueEnd + new Vector3(-10, 0, 0);
            }*/

            //VFXEdgeUtils.RenderBezier(points[0], trueEnd, points[1], points[2], edgeColor, realWidth);

            VFXEdgeUtils.lineMat.SetPass(0);
            Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
        }

        protected override void DrawEndpoint(Vector2 pos, bool start)
        {
        }
    }
}
