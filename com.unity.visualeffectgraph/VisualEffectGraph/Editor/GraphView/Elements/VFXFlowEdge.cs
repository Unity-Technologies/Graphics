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
            GL.Vertex3(to.x - arrowHeight * .5f, to.y - arrowHeight, 0);
            GL.Vertex3(to.x + arrowHeight * .5f, to.y - arrowHeight, 0);
            GL.Vertex3(to.x, to.y, 0);
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
            if (!start)
            {
                VFXEdgeUtils.RenderTriangle(pos, 8, edgeColor);
            }
        }
    }


    internal class VFXFlowEdge : Edge, IControlledElement<VFXFlowEdgeController>
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

                var newInput = view.GetFlowAnchorByController(controller.input);

                if (base.input != newInput)
                {
                    if (base.input != null)
                    {
                        base.input.Disconnect(this);
                    }
                    base.input = newInput;
                    base.input.Connect(this);
                }

                var newOutput = view.GetFlowAnchorByController(controller.output);

                if (base.output != newOutput)
                {
                    if (base.output != null)
                    {
                        base.output.Disconnect(this);
                    }
                    base.output = newOutput;
                    base.output.Connect(this);
                }
            }
            edgeControl.UpdateLayout();
        }

        VFXFlowEdgeController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXFlowEdgeController controller
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
