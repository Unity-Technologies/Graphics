using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    class VFXEdContextNodeBlock : VFXEdNodeBlock
    {
        public VFXEdNodeBlockHeader Header { get { return m_Header; } }
        public VFXContextModel Model { get { return m_Model; } }
        private VFXProperty[] Properties { get { return Model.Desc.m_Properties; } }

        private VFXEdNodeBlockHeader m_Header;
        private VFXContextModel m_Model;

        public VFXEdContextNodeBlock(VFXEdDataSource dataSource, VFXContextModel model)
            : base(dataSource)
        {
            m_Model = model;
            VFXContextDesc desc = m_Model.Desc;

            m_DataSource = dataSource;
            translation = Vector3.zero; 
            m_Caps = Capabilities.Normal;
            
            m_Header = new VFXEdNodeBlockHeader(desc.Name, VFXEditor.styles.GetIcon("Box"), false);
            AddChild(m_Header);

            if (desc.m_Properties != null && desc.m_Properties.Length > 0)
            {
                int nbProperties = Properties.Length;
                m_Fields = new VFXUIPropertySlotField[nbProperties];
                for (int i = 0; i < nbProperties; ++i)
                {
                    m_Fields[i] = new VFXUIPropertySlotField(dataSource, Model.GetSlot(i));
                    AddChild(m_Fields[i]);
                }
                Header.Collapseable = true;
            }
            else
                m_Fields = new VFXUIPropertySlotField[0];
        }

        public override void Layout()
        {
            base.Layout();

            if (collapsed)
            {
                scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);

                // if collapsed, rejoin all connectors on the middle of the header
                foreach (var field in m_Fields)
                {
                    field.translation = new Vector2(0.0f, (VFXEditorMetrics.NodeBlockHeaderHeight-VFXEditorMetrics.DataAnchorSize.y)/2);
                }
            }
            else
            {
                scale = new Vector2(scale.x, GetHeight());
                float curY = VFXEditorMetrics.NodeBlockHeaderHeight;

                foreach (var field in m_Fields)
                {
                    field.translation = new Vector2(0.0f, curY);
                    curY += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
                } 
            }
        }



        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();
            GUI.color = new Color(0.75f,0.75f,0.75f,1.0f);
            GUI.Box(r, "", VFXEditor.styles.DataNodeBlock);
            GUI.color = Color.white;
            base.Render(parentRect, canvas);
        }

        protected override GUIStyle GetNodeBlockStyle()
        {
            return VFXEditor.styles.DataNodeBlock;
        }

        protected override GUIStyle GetNodeBlockSelectedStyle()
        {
            return VFXEditor.styles.DataNodeBlock;
        }

        public override VFXPropertySlot GetSlot(string name)
        {
            for (int i = 0; i < Model.GetNbSlots(); ++i)
                if (Model.GetSlot(i).Name.Equals(name))
                    return Model.GetSlot(i);
            return null;
        }

        public override void SetSlotValue(string name, VFXValue value)
        {
            for (int i = 0; i < Model.GetNbSlots(); ++i)
                if (Model.GetSlot(i).Name.Equals(name))
                {
                    Model.GetSlot(i).Value = value;
                    break;
                }
        }
    }
}
