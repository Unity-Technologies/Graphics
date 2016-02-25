using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNodeBlock : VFXEdNodeBlock
    {
        public List<VFXDataParam> Params { get { return m_DataBlock.Parameters; } } 
        public VFXParamValue[] ParamValues { get { return m_ParamValues; } }
        protected VFXParamValue[] m_ParamValues;
        protected VFXDataBlock m_DataBlock;

        public VFXEdDataNodeBlock(VFXDataBlock datablock, VFXEdDataSource dataSource) : base(dataSource)
        {
            m_Name = datablock.name;
            m_DataBlock = datablock;

            m_ParamValues = new VFXParamValue[m_DataBlock.Parameters.Count];
            m_Fields = new VFXEdNodeBlockParameterField[m_DataBlock.Parameters.Count];
            
            // For selection
            target = ScriptableObject.CreateInstance<VFXEdDataNodeBlockTarget>();
            (target as VFXEdDataNodeBlockTarget).targetNodeBlock = this;

            int i = 0;
            foreach(VFXDataParam p in m_DataBlock.Parameters) {
                m_ParamValues[i] = VFXParamValue.Create(p.m_type);
                m_Fields[i] = new VFXEdNodeBlockParameterField(dataSource as VFXEdDataSource, p.m_name , m_ParamValues[i], true, Direction.Output, i);
                AddChild(m_Fields[i]);
                i++;
            }

            AddChild(new VFXEdNodeBlockHeader(m_Name, m_DataBlock.icon, datablock.Parameters.Count > 0));
            AddManipulator(new ImguiContainer());
            Layout();
        }


        protected override float GetHeight()
        {
            float height = VFXEditorMetrics.NodeBlockHeaderHeight;
            foreach(VFXEdNodeBlockParameterField field in m_Fields) {
                height += field.scale.y + VFXEditorMetrics.NodeBlockParameterSpacingHeight;
            }
            height += VFXEditorMetrics.NodeBlockFooterHeight;
            return height;
        }

        protected override GUIStyle GetNodeBlockStyle()
        {
            return VFXEditor.styles.DataNodeBlock;
        }

        protected override GUIStyle GetNodeBlockSelectedStyle()
        {
            return VFXEditor.styles.DataNodeBlockSelected;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
        }
    }
}
