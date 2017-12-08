using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    internal static class VFXEdgeUtils
    {
        static Material s_LineMat;
        static Material s_CircleMat;

        public static Material lineMat
        {
            get
            {
                if (s_LineMat == null)
                    s_LineMat = new Material(Shader.Find("Unlit/AALine"));
                return s_LineMat;
            }
        }
        public static Material shapesMat
        {
            get
            {
                if (s_CircleMat == null)
                    s_CircleMat = new Material(Shader.Find("Unlit/VColor"));
                return s_CircleMat;
            }
        }


        public static void RenderDisc(Vector2 center, float radius, Color color)
        {
            shapesMat.SetPass(0);
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.Color(color);

            float prec = 5;


            GL.Vertex3(center.x - radius, center.y, 0);

            for (float f = -prec + 1; f < prec; f += 1)
            {
                float phi = f * Mathf.PI / (2.0f * prec);

                float x = center.x + Mathf.Sin(phi) * radius;
                float y = Mathf.Cos(phi) * radius;

                //GL.TexCoord3(f,)
                GL.Vertex3(x, center.y - y, 0);
                GL.Vertex3(x, center.y + y, 0);
            }

            GL.Vertex3(center.x + radius, center.y, 0);

            GL.End();
        }

        public static void RenderTriangle(Vector2 to, float arrowHeight, Color color)
        {
            shapesMat.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex3(to.x - arrowHeight * .5f, to.y - arrowHeight * .5f, 0);
            GL.Vertex3(to.x + arrowHeight * .5f, to.y - arrowHeight * .5f, 0);
            GL.Vertex3(to.x, to.y + arrowHeight * 0.5f, 0);
            GL.End();
        }

        public static void RenderLine(Vector2 start, Vector2 end, Color color, float edgeWidth, float viewScale)
        {
            lineMat.SetFloat("_ZoomFactor", viewScale);
            lineMat.SetColor("_Color", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.gamma : color);

            lineMat.SetPass(0);


            GL.Begin(GL.TRIANGLE_STRIP);

            Vector2 dir = (end - start).normalized;
            Vector2 norm = new Vector2(dir.y, -dir.x);

            float halfWidth = edgeWidth * 0.5f;

            float vertexHalfWidth = halfWidth + 2;
            Vector2 edge = norm * vertexHalfWidth;

            GL.TexCoord3(-vertexHalfWidth, halfWidth, 0);
            GL.Vertex(start - edge);
            GL.TexCoord3(vertexHalfWidth, halfWidth, 0);
            GL.Vertex(start + edge);

            GL.TexCoord3(-vertexHalfWidth, halfWidth, 1);
            GL.Vertex(end - edge);
            GL.TexCoord3(vertexHalfWidth, halfWidth, 1);
            GL.Vertex(end + edge);

            GL.End();
            GL.sRGBWrite = false;
        }

        public static void RenderBezier(Vector2 start, Vector2 end, Vector2 tStart, Vector2 tEnd, Color color, float edgeWidth)
        {
            lineMat.SetPass(0);
            lineMat.SetColor("_Color", color);
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.Color(color);

            Vector2 prevPos = start;
            Vector2 edge = Vector2.zero;
            Vector2 dir = (tStart - start).normalized;
            Vector2 norm = new Vector2(dir.y, -dir.x);
            //tStart = start + tStart;
            //tEnd +=  end + tEnd;

            //GL.Vertex(start);

            float cpt = (start - end).magnitude / 5;
            if (cpt < 3)
                cpt = 3;


            float halfWidth = edgeWidth * 0.5f + 0.5f;

            float vertexHalfWidth = halfWidth + 2;

            for (float t = 1 / cpt; t < 1; t += 1 / cpt)
            {
                float minT = 1 - t;

                Vector2 pos = t * t * t * end +
                    3 * minT * t * t * tEnd +
                    3 * minT * minT * t * tStart +
                    minT * minT * minT * start;

                edge = norm * vertexHalfWidth;

                GL.TexCoord3(-vertexHalfWidth, halfWidth, t);
                GL.Vertex(prevPos - edge);
                GL.TexCoord3(vertexHalfWidth, halfWidth, t);
                GL.Vertex(prevPos + edge);

                dir = (pos - prevPos).normalized;
                norm = new Vector2(dir.y, -dir.x);

                prevPos = pos;
            }

            dir = (end - prevPos).normalized;
            norm = new Vector2(dir.y, -dir.x);
            edge = norm * vertexHalfWidth;

            GL.TexCoord3(-vertexHalfWidth, halfWidth, 1);
            GL.Vertex(end - edge);
            GL.TexCoord3(vertexHalfWidth, halfWidth, 1);
            GL.Vertex(end + edge);

            GL.End();
        }
    }

    internal class VFXFlowEdgeControl : VFXEdgeControl
    {
        public VFXFlowEdgeControl()
        {
            orientation = Orientation.Vertical;
        }

        protected override void DrawEdge()
        {
            base.DrawEdge();
            DrawEndpoint(parent.ChangeCoordinatesTo(this, from), true);
            DrawEndpoint(parent.ChangeCoordinatesTo(this, to), false);
        }

        protected void DrawEndpoint(Vector2 pos, bool start)
        {
            if (start)
            {
                VFXEdgeUtils.RenderDisc(pos - new Vector2(0, 6), 6, edgeColor);
            }
            else
            {
                VFXEdgeUtils.RenderTriangle(pos, 12, edgeColor);
            }
        }
    }


    internal class VFXFlowEdge : Edge, IControlledElement<VFXFlowEdgePresenter>
    {
        public VFXFlowEdge()
        {
            RegisterCallback<ControllerChangedEvent>(OnChange);
        }

        protected virtual void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        protected virtual void SelfChange()
        {
            if (controller != null)
            {
                VFXView view = GetFirstAncestorOfType<VFXView>();
                base.input = view.GetFlowAnchorByPresenter(controller.input);
                base.output = view.GetFlowAnchorByPresenter(controller.output);
            }
            edgeControl.UpdateLayout();
        }

        VFXFlowEdgePresenter m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXFlowEdgePresenter controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        protected override EdgeControl CreateEdgeControl()
        {
            return new VFXFlowEdgeControl
            {
                capRadius = 4,
                interceptWidth = 3
            };
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();


            VFXEdgeControl edgeControl = this.edgeControl as VFXEdgeControl;

            edgeControl.outputColor = edgeControl.inputColor = GetPresenter<EdgePresenter>().selected ? selectedColor : defaultColor;
        }

        public new VFXFlowAnchor input
        {
            get { return base.input as VFXFlowAnchor; }
        }
        public new VFXFlowAnchor output
        {
            get { return base.output as VFXFlowAnchor; }
        }
    }
}
