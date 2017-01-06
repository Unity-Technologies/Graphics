using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class MaterialNodeDrawData : NodeDrawData
    {
        NodePreviewDrawData m_NodePreviewDrawData;

        public bool requiresTime
        {
            get { return node is IRequiresTime; }
        }

        public override IEnumerable<GraphElementPresenter> elements
        {
            // TODO JOCE Sub ideal to use yield, but will do for now.
            get
            {
                foreach (var element in base.elements)
                {
                    yield return element;
                }
                yield return m_NodePreviewDrawData;
            }
        }

        public override void OnModified(ModificationScope scope)
        {
            base.OnModified(scope);
            // TODO: Propagate callback rather than setting property
            if (m_NodePreviewDrawData != null)
                m_NodePreviewDrawData.modificationScope = scope;
        }

        protected MaterialNodeDrawData()
        {}

        public override void Initialize(INode inNode)
        {
            base.Initialize(inNode);
            AddPreview(inNode);
        }

        private void AddPreview(INode inNode)
        {
            var materialNode = inNode as AbstractMaterialNode;
            if (materialNode == null || !materialNode.hasPreview)
                return;

            m_NodePreviewDrawData = CreateInstance<NodePreviewDrawData>();
            m_NodePreviewDrawData.Initialize(materialNode);
//            m_Children.Add(m_NodePreviewDrawData);
        }
    }
}
