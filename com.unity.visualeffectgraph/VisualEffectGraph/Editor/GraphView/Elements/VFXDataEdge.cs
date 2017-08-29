using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
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

        protected virtual Color edgeColor
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

                Vector3 prevPos = start;
                Vector3 border = Vector2.zero;
                Vector3 dir = (tStart - start).normalized;
                Vector3 norm = new Vector2(dir.y, -dir.x);

                int cpt = (int)((start - end).magnitude / 5);
                if (cpt < 3)
                    cpt = 3;

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

                for (int i = 1; i < cpt; ++i)
                {
                    float t = (float)i / (float)cpt;

                    float minT = 1 - t;

                    Vector3 pos = t * t * t * end +
                        3 * minT * t * t * tEnd +
                        3 * minT * minT * t * tStart +
                        minT * minT * minT * start;

                    border = norm * vertexHalfWidth;

                    index = i - 1;
                    uvs[index * 2] = new Vector2(-vertexHalfWidth, halfWidth);
                    vertices[index * 2] = prevPos - border;

                    uvs[index * 2 + 1] = new Vector2(vertexHalfWidth, halfWidth);
                    vertices[index * 2 + 1] = prevPos + border;

                    dir = (pos - prevPos).normalized;
                    norm = new Vector3(dir.y, -dir.x, 0);

                    prevPos = pos;
                }

                dir = (end - prevPos).normalized;
                norm = new Vector2(dir.y, -dir.x);
                border = norm * vertexHalfWidth;

                index = cpt - 1;
                uvs[index * 2] = new Vector2(-vertexHalfWidth, halfWidth);
                vertices[index * 2] = prevPos - border;

                uvs[index * 2 + 1] = new Vector2(vertexHalfWidth, halfWidth);
                vertices[index * 2 + 1] = prevPos + border;

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
                    //fill triange indices as it is a triangle strip
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

    internal class VFXDataEdgeControl : VFXEdgeControl
    {
    }


    internal class VFXDataEdge : Edge
    {
        public VFXDataEdge()
        {
        }

        public override int layer
        {
            get
            {
                return -1;
            }
        }


        public override void OnDataChanged()
        {
            base.OnDataChanged();

            foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
                RemoveFromClassList(cls);


            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;


            if (outputPresenter == null && inputPresenter == null)
                return;
            if (outputPresenter != null && panel != null)
                panel.dataWatch.ForceDirtyNextPoll(outputPresenter);

            if (inputPresenter != null && panel != null)
                panel.dataWatch.ForceDirtyNextPoll(inputPresenter);

            System.Type type = inputPresenter != null ? inputPresenter.anchorType : outputPresenter.anchorType;

            AddToClassList(VFXTypeDefinition.GetTypeCSSClass(type));
        }

        protected override EdgeControl CreateEdgeControl()
        {
            return new VFXDataEdgeControl
            {
                capRadius = 4,
                interceptWidth = 3
            };
        }

#if false
        protected override void DrawEdge()
        {
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            VFXDataAnchorPresenter inputPresenter = edgePresenter.input as VFXDataAnchorPresenter;

            if (outputPresenter == null && inputPresenter == null)
                return;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);
            Color edgeColor = style.borderColor;

            if (inputPresenter != null && inputPresenter.sourceNode is VFXBlockPresenter)
            {
                to = to + new Vector2(-10, 0);
            }


            Orientation orientation = Orientation.Horizontal;
            Vector3[] points, tangents;
            GetTangents(orientation, from, to, out points, out tangents);


            GraphView view = this.GetFirstAncestorOfType<GraphView>();

            float realWidth = edgePresenter.selected ? edgeWidth * 2 : edgeWidth;
            if (realWidth * view.scale < 1.5f)
            {
                realWidth = 1.5f / view.scale;
            }
            VFXFlowEdge.RenderBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, realWidth);
            /*if (edgePresenter.selected)
            {
                Handles.DrawBezier(points[0] + Vector3.down, points[1] + Vector3.down , tangents[0] + Vector3.down , tangents[1] + Vector3.down , edgeColor, null, 2f);
                Handles.DrawBezier(points[0] + Vector3.up , points[1] + Vector3.up , tangents[0] + Vector3.up , tangents[1] + Vector3.up , edgeColor, null, 2f);
            }
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 2f);*/
        }

#endif
    }
}
