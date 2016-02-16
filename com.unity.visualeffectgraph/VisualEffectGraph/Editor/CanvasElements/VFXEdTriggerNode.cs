using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdTriggerNode : VFXEdNode
    {

        public VFXContextModel Model
        {
            get { return m_Model; }
        }

        protected VFXContextModel m_Model;

        internal VFXEdTriggerNode(Vector2 canvasPosition, VFXEdDataSource dataSource) 
            : base (canvasPosition, dataSource)
        {

            m_Title = "Trigger";

            m_Inputs.Add(new VFXEdFlowAnchor(1, typeof(float), VFXEdContext.Trigger, m_DataSource, Direction.Input));
            m_Outputs.Add(new VFXEdFlowAnchor(2, typeof(float), VFXEdContext.Trigger, m_DataSource, Direction.Output));

            AddChild(inputs[0]);
            AddChild(outputs[0]);
            ZSort();
            Layout();

        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            GUI.Box(r, "", VFXEditor.styles.Node);
            GUI.Label(new Rect(0, r.y, r.width, 24), title, VFXEditor.styles.NodeTitle);

            base.Render(parentRect, canvas);


        }
    }
}
