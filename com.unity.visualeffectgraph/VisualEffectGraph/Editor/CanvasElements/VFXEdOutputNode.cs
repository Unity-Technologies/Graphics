using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdOutputNode : VFXEdContextNode
    {
        public VFXEdOutputNodeBlock OutputNodeBlock
        {
            get { return m_OutputNodeBlock; }
            set
            {
                if(m_OutputNodeBlock != null) RemoveChild(m_OutputNodeBlock);
                m_OutputNodeBlock = value;
                AddChild(m_OutputNodeBlock);
                value.BindTo(this);
            }
        }
        private VFXEdOutputNodeBlock m_OutputNodeBlock;

        public VFXEdOutputNode(Vector2 canvasPosition, VFXEdDataSource dataSource, VFXEdOutputNodeBlock outputNodeBlock) : base (canvasPosition,VFXEdContext.Output, dataSource)
        {
            OutputNodeBlock = outputNodeBlock;
            VFXEdFlowAnchor output = m_Outputs[0];
            RemoveChild(output);
            target = ScriptableObject.CreateInstance<VFXEdOutputNodeTarget>();
            (target as VFXEdOutputNodeTarget).targetNode = this;
            m_Outputs.Clear();
        }

        public override bool AcceptNodeBlock(VFXEdNodeBlock block)
        {
            return (block is VFXEdProcessingNodeBlock);
        }

        public void MenuAddOutputNode(object o)
        {
            int type = (int)o;
            OutputNodeBlock = new VFXEdOutputNodeBlock(m_DataSource, type);

        }

        public override void Layout()
        {
            m_HeaderOffset = m_OutputNodeBlock.GetHeight();

            base.Layout();

            m_OutputNodeBlock.translation = m_ClientArea.position + VFXEditorMetrics.NodeBlockContainerPosition;
            m_OutputNodeBlock.scale = new Vector2(m_NodeBlockContainer.scale.x, m_OutputNodeBlock.GetHeight());

        }

        protected override GenericMenu GetNodeMenu(Vector2 canvasClickPosition)
        {
            GenericMenu menu = base.GetNodeMenu(canvasClickPosition);

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Set Output/Point"), false, MenuAddOutputNode, 0);
            menu.AddItem(new GUIContent("Set Output/Billboard"), false, MenuAddOutputNode, 1);
            menu.AddItem(new GUIContent("Set Output/Velocity-Oriented Quad"), false, MenuAddOutputNode, 2);
            return menu;
        }
    }
}
