using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdProcessingNodeBlock : VFXEdNodeBlock
    {

        public VFXBlockModel Model
        {
            get { return m_Model; }
        }
        private VFXBlockModel m_Model;
        private VFXParamValue[] m_Params;

        public VFXEdProcessingNodeBlock(VFXBlock block, VFXEdDataSource dataSource) : base(dataSource)
        {
            m_Model = new VFXBlockModel(block);
            m_Params = new VFXParamValue[block.m_Params.Length];
            m_Fields = new VFXEdNodeBlockParameterField[block.m_Params.Length];

            for (int i = 0; i < m_Params.Length; ++i)
            {
                m_Params[i] = VFXParamValue.Create(block.m_Params[i].m_Type);
                m_Fields[i] = new VFXEdNodeBlockParameterField(dataSource as VFXEdDataSource, block.m_Params[i].m_Name, m_Params[i], true, Direction.Input);
                AddChild(m_Fields[i]);
                m_Model.BindParam(m_Params[i], i);
            }

            m_Name = block.m_Name;

            AddChild(new VFXEdNodeBlockHeader(m_Name, VFXEditor.styles.GetIcon(block.m_IconPath == "" ? "Default" : block.m_IconPath), block.m_Params.Length > 0));
            AddManipulator(new ImguiContainer());

            Layout();
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            Model.Detach();
        }

        protected override float GetHeight()
        {
            float height = VFXEditorMetrics.NodeBlockHeaderHeight;
            foreach(VFXEdNodeBlockParameterField field in m_Fields) {
                height += field.scale.y;
            }
            return height;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
        }

    }
}
