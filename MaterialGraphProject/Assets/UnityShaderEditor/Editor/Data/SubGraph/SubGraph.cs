using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SubGraph : AbstractSubGraph
    {
        [NonSerialized]
        private SubGraphOutputNode m_OutputNode;

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

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_OutputNode = null;
        }

        public override void AddNode(INode node)
        {
            if (outputNode != null && node is SubGraphOutputNode)
            {
                Debug.LogWarning("Attempting to add second SubGraphOutputNode to SubGraph. This is not allowed.");
                return;
            }

            var materialNode = node as AbstractMaterialNode;
            if (materialNode != null)
            {
                var amn = materialNode;
                if (!amn.allowedInSubGraph)
                {
                    Debug.LogWarningFormat("Attempting to add {0} to Sub Graph. This is not allowed.", amn.GetType());
                    return;
                }
            }
            base.AddNode(node);
        }

        public override IEnumerable<AbstractMaterialNode> activeNodes
        {
            get
            {
                List<INode> nodes = new List<INode>();
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode);
                return nodes.OfType<AbstractMaterialNode>();
            }
        }
    }
}
