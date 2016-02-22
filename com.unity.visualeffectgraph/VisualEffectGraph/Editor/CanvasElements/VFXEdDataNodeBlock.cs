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
        VFXDataBlock m_DataBlock;
        VFXParamValue[] m_Params;

        public VFXEdDataNodeBlock(VFXDataBlock datablock, VFXEdDataSource dataSource) : base(dataSource)
        {
            m_Name = datablock.name;
            m_DataBlock = datablock;

            m_Params = new VFXParamValue[m_DataBlock.Parameters.Count];
            m_Fields = new VFXEdNodeBlockParameterField[m_DataBlock.Parameters.Count];

            int i = 0;
            foreach(KeyValuePair<string, VFXParam.Type> kvp in m_DataBlock.Parameters) {
                m_Params[i] = VFXParamValue.Create(kvp.Value);
                m_Fields[i] = new VFXEdNodeBlockParameterField(dataSource as VFXEdDataSource, kvp.Key, m_Params[i], true, Direction.Output, i);
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
                height += field.scale.y;
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
