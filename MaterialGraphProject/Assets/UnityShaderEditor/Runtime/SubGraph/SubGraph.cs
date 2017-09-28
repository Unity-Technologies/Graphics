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

        private IEnumerable<AbstractMaterialNode> usedNodes
        {
            get
            {
                var nodes = new List<INode>();
                //Get the rest of the nodes for all the other slots
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode, NodeUtils.IncludeSelf.Exclude);
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

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if we are previewing the graph we need to
            // export 'exposed props' if we are 'for real'
            // then we are outputting the graph in the
            // nested context and the needed values will
            // be copied into scope.
            if (generationMode == GenerationMode.Preview)
            {
                foreach (var prop in properties)
                    collector.AddShaderProperty(prop);
            }

            foreach (var node in usedNodes)
            {
                if (node is IGenerateProperties)
                    (node as IGenerateProperties).CollectShaderProperties(collector, generationMode);
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
