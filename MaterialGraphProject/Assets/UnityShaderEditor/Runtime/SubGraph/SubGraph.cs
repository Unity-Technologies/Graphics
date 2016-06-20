using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class SubGraph : AbstractMaterialGraph
    {
        [NonSerialized]
        private SubGraphInputNode m_InputNode;
        [NonSerialized]
        private SubGraphOutputNode m_OutputNode;

        public SubGraphInputNode inputNode
        {
            get
            {
                // find existing node
                if (m_InputNode == null)
                    m_InputNode = GetNodes<SubGraphInputNode>().FirstOrDefault();

                return m_InputNode;
            }
        }

        public SubGraphOutputNode outputNode
        {
            get
            {
                // find existing node
                if (m_OutputNode == null)
                    m_OutputNode = GetNodes<SubGraphOutputNode>().FirstOrDefault();

                return m_OutputNode;
            }
        }
 
        [NonSerialized]
        private List<INode> m_ActiveNodes = new List<INode>();
        public IEnumerable<AbstractMaterialNode> activeNodes
        {
            get
            {
                m_ActiveNodes.Clear();
                NodeUtils.DepthFirstCollectNodesFromNode(m_ActiveNodes, outputNode);
                return m_ActiveNodes.OfType<AbstractMaterialNode>();
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_InputNode = null;
            m_OutputNode = null;
        }

        public override void AddNode(INode node)
        {
            if (inputNode != null && node is SubGraphInputNode)
            {
                Debug.LogWarning("Attempting to add second SubGraphInputNode to SubGraph. This is not allowed.");
                return;
            }

            if (outputNode != null && node is SubGraphOutputNode)
            {
                Debug.LogWarning("Attempting to add second SubGraphOutputNode to SubGraph. This is not allowed.");
                return;
            }
            base.AddNode(node);
        }

        public void PostCreate()
        {
            AddNode(new SubGraphInputNode());
            AddNode(new SubGraphOutputNode());
        }
    }
}
