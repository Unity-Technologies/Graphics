using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdEventNode : VFXEdNodeBase, VFXModelHolder 
    { 
        protected VFXEdFlowAnchor m_Output;

        public VFXElementModel GetAbstractModel() { return Model; }
        public VFXEventModel Model { get { return m_Model; } }
        private VFXEventModel m_Model;
        public Color m_ButtonColor;


        private Rect m_FireEventRect;

        internal VFXEdEventNode(VFXEventModel model, VFXEdDataSource dataSource) : base (model.UIPosition, dataSource)
        {
            m_Model = model;
            m_DataSource = dataSource;

            m_Outputs.Add(new VFXEdFlowAnchor(0, typeof(float), VFXContextDesc.Type.kTypeNone, m_DataSource, Direction.Output));
            m_Output = m_Outputs[0];
            AddChild(m_Output);
            m_ButtonColor = Color.white;
            MouseDown += VFXEdEventNode_MouseDown;

            target = ScriptableObject.CreateInstance<VFXEdEventNodeTarget>();
            (target as VFXEdEventNodeTarget).eventNode = this;

            ZSort();
            Layout();
        }

        private bool VFXEdEventNode_MouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            Rect absRect = new Rect(translation.x + m_FireEventRect.x, translation.y + m_FireEventRect.y, m_FireEventRect.width, m_FireEventRect.height);
            if(absRect.Contains(parent.MouseToCanvas(e.mousePosition)))
            {
                VFXEditor.ForeachComponents(c => c.SendEvent(Model.Name));
                parent.Animate(this).Lerp("m_ButtonColor", new Color(1.0f,0.5f,0.0f), Color.white).Then((elem, anim, userData) =>
                {
                    anim.Done();
                });
                Invalidate();
                parent.Repaint();

                e.Use();
                return true;
            }
            return false;
        }

        public override void UpdateModel(UpdateType t)
        {
            Model.UpdatePosition(translation);
        }

        public override void Layout()
        {
            base.Layout();

            Vector2 s = VFXEditorMetrics.EventNodeDefaultScale;

            // Measure Event Name, naively for now
            s.x = Model.Name.Length * 28 + 32;

            m_ClientArea = new Rect(Vector2.zero, s);
            m_FireEventRect = new Rect(16, 72, m_ClientArea.width - 32, 48);
            this.scale = s + new Vector2(0.0f,m_Output.scale.y);
            m_Output.translation = new Vector2((s.x / 2) - (m_Output.scale.x / 2), s.y - VFXEditor.styles.NodeSelected.border.bottom);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            GUI.color = new Color(0.8f,0.8f,0.8f);
            GUI.Box(m_ClientArea, "", VFXEditor.styles.EventNode);
            GUI.color = Color.white;

            Rect textrect = VFXEditorMetrics.EventNodeTextRectOffset.Remove(m_ClientArea);

            GUI.color = m_ButtonColor;
            GUI.Label(textrect, Model.Name, VFXEditor.styles.EventNodeText);
            GUI.Box(m_FireEventRect, "", VFXEditor.styles.EventNode);
            GUI.Label(m_FireEventRect, "Trigger Event", VFXEditor.styles.CenteredText);
            GUI.color = Color.white;


            if(selected) GUI.Box(m_ClientArea, "", VFXEditor.styles.NodeSelected);
            base.Render(parentRect, canvas);
        }
    }
}
