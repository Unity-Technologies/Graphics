using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class PixelGraph : AbstractMaterialGraph
    {
        [NonSerialized]
        private PixelShaderNode m_PixelMasterNode;

        public PixelShaderNode pixelMasterNode
        {
            get
            {
                // find existing node
                if (m_PixelMasterNode == null)
                    m_PixelMasterNode = GetNodes<AbstractMaterialNode>().FirstOrDefault(x => x.GetType() == typeof(PixelShaderNode)) as PixelShaderNode;

                return m_PixelMasterNode;
            }
        }

        [NonSerialized]
        private List<INode> m_ActiveNodes = new List<INode>();
        public IEnumerable<AbstractMaterialNode> activeNodes
        {
            get
            {
                m_ActiveNodes.Clear();
                NodeUtils.DepthFirstCollectNodesFromNode(m_ActiveNodes, pixelMasterNode);
                return m_ActiveNodes.OfType<AbstractMaterialNode>();
            }
        }

        public string name
        {
            get { return "Graph_ " + pixelMasterNode.GetVariableNameForNode(); }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_PixelMasterNode = null;
        }

        public override void AddNode(INode node)
        {
            if (pixelMasterNode != null && node is PixelShaderNode)
            {
                Debug.LogWarning("Attempting to add second PixelShaderNode to PixelGraph. This is not allowed.");
                return;
            }
            base.AddNode(node);
        }

        
        /*
        public Material GetMaterial()
        {
            if (pixelMasterNode == null)
                return null;

            var material = pixelMasterNode.previewMaterial;
            AbstractMaterialNode.UpdateMaterialProperties(pixelMasterNode, material);
            return material;
        }*/
    }
}
