using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdEventNode : VFXEdNodeBase 
    { 

        protected string m_EventName;
        protected VFXEdFlowAnchor m_Output;

        internal VFXEdEventNode(Vector2 canvasposition, VFXEdDataSource dataSource, string eventname) : base (canvasposition, dataSource)
        {
            m_EventName = eventname;
            m_DataSource = dataSource;

            m_Outputs.Add(new VFXEdFlowAnchor(0, typeof(float), VFXEdContext.Trigger, m_DataSource, Direction.Output));
            m_Output = m_Outputs[0];
            AddChild(m_Output);
            ZSort();
            Layout();
        }

        public override void Layout()
        {
            base.Layout();
            Vector2 s = VFXEditorMetrics.EventNodeDefaultScale;
            m_ClientArea = new Rect(Vector2.zero, VFXEditorMetrics.EventNodeDefaultScale);
            this.scale = s + new Vector2(0.0f,m_Output.scale.y);
            m_Output.translation = new Vector2((s.x / 2) - (m_Output.scale.x / 2), s.y - VFXEditor.styles.NodeSelected.border.bottom);
        }

        public override void OnRemove()
        {

        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
            GUI.Box(m_ClientArea, "", VFXEditor.styles.EventNode);
            Rect textrect = VFXEditorMetrics.EventNodeTextRectOffset.Remove(m_ClientArea);
            GUI.Label(textrect, m_EventName, VFXEditor.styles.EventNodeText);
            if(selected) GUI.Box(m_ClientArea, "", VFXEditor.styles.NodeSelected);
        }
    }
}
