using System;
using UnityEditor.Graphing.Drawing;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class MaterialNodeDrawData : NodeDrawData
    {
        public bool requiresTime
        {
            get { return node is IRequiresTime; }
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

            var previewData = CreateInstance<NodePreviewDrawData>();
            previewData.Initialize(materialNode);
            m_Children.Add(previewData);
        }
    }
}
