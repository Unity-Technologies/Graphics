using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class SubGraph : AbstractMaterialGraph
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IGenerateProperties
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

        private IEnumerable<AbstractMaterialNode> usedNodes
        {
            get
            {
                var nodes = new List<INode>();
                //Get the rest of the nodes for all the other slots
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode, null, NodeUtils.IncludeSelf.Exclude);
                return nodes.OfType<AbstractMaterialNode>();
            }
        }

        public PreviewMode previewMode
        {
            get { return usedNodes.Any(x => x.previewMode == PreviewMode.Preview3D) ? PreviewMode.Preview3D : PreviewMode.Preview2D; }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in usedNodes)
            {
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(visitor, generationMode);
            }
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in usedNodes)
            {
                if (node is IGeneratesFunction)
                    (node as IGeneratesFunction).GenerateNodeFunction(visitor, generationMode);
            }
        }

        public void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in usedNodes)
            {
                if (node is IGenerateProperties)
                    (node as IGenerateProperties).GeneratePropertyBlock(visitor, generationMode);
            }
        }

        public void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in usedNodes)
            {
                if (node is IGenerateProperties)
                    (node as IGenerateProperties).GeneratePropertyUsages(visitor, generationMode);
            }
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in usedNodes)
            {
                //TODO: Fix
                //if (node is IGeneratesVertexShaderBlock)
                //    (node as IGeneratesVertexShaderBlock).GenerateVertexShaderBlock(visitor, generationMode);
            }
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in usedNodes)
            {
                //TODO: Fix
                //if (node is IGeneratesVertexToFragmentBlock)
                //    (node as IGeneratesVertexToFragmentBlock).GenerateVertexToFragmentBlock(visitor, generationMode);
            }
        }

        public IEnumerable<PreviewProperty> GetPreviewProperties()
        {
            List<PreviewProperty> props = new List<PreviewProperty>();
            foreach (var node in usedNodes)
                node.CollectPreviewMaterialProperties(props);
            return props;
        }
    }
}
