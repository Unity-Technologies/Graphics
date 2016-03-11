using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    class VFXEdContextNodeBlock : VFXEdNodeBlock
    {
        public VFXEdNodeBlockHeader Header { get { return m_Header; } }
        public VFXContextModel Model { get { return m_Model; } }
        private VFXEdNodeBlockHeader m_Header;
        private VFXParamValue[] m_ParamValues;
        private VFXContextModel m_Model;

        public VFXEdContextNodeBlock(VFXEdDataSource dataSource, VFXContextModel model)
            : base(dataSource)
        {
            m_Model = model;
            VFXContextDesc desc = m_Model.Desc;

            m_DataSource = dataSource;
            translation = Vector3.zero; 
            m_Caps = Capabilities.Normal;
            m_Fields = new VFXEdNodeBlockParameterField[0];
            m_ParamValues = new VFXParamValue[0];
            m_Header = new VFXEdNodeBlockHeader(desc.Name, VFXEditor.styles.GetIcon("Box"), false);
            AddChild(m_Header);

            if (desc.m_Params != null && desc.m_Params.Length > 0)
            {
                int nbParams = desc.m_Params.Length;
                m_Fields = new VFXEdNodeBlockParameterField[nbParams];
                m_ParamValues = new VFXParamValue[nbParams];
                for (int i = 0; i < nbParams; ++i)
                {
                    m_ParamValues[i] = VFXParamValue.Create(desc.m_Params[i].m_Type);
                    m_Fields[i] = new VFXEdNodeBlockParameterField(dataSource, desc.m_Params[i].m_Name, m_ParamValues[i], true, Direction.Input, 0);
                    AddChild(m_Fields[i]);
                    m_Model.BindParam(m_ParamValues[i], i);
                }
                Header.Collapseable = true;
            }
        }

        public override void Layout()
        {
            base.Layout();

            if (collapsed)
            {
                scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);

                // if collapsed, rejoin all connectors on the middle of the header
                foreach(VFXEdNodeBlockParameterField field in m_Fields)
                {
                    field.translation = new Vector2(0.0f, (VFXEditorMetrics.NodeBlockHeaderHeight-VFXEditorMetrics.DataAnchorSize.y)/2);
                }
            }
            else
            {
                scale = new Vector2(scale.x, GetHeight());
                float curY = VFXEditorMetrics.NodeBlockHeaderHeight;

                foreach(VFXEdNodeBlockParameterField field in m_Fields)
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
    }
}
