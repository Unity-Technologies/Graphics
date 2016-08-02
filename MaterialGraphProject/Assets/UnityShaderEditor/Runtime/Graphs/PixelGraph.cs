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

        public static void GenerateSurfaceShader(
            PixelShaderNode pixelNode,
            ShaderGenerator shaderBody,
            ShaderGenerator inputStruct,
            ShaderGenerator lightFunction,
            ShaderGenerator surfaceOutput,
            ShaderGenerator nodeFunction,
            PropertyGenerator shaderProperties,
            ShaderGenerator propertyUsages,
            ShaderGenerator vertexShader,
            bool isPreview)
        {
            pixelNode.GenerateLightFunction(lightFunction);
            pixelNode.GenerateSurfaceOutput(surfaceOutput);

            var genMode = isPreview ? GenerationMode.Preview3D : GenerationMode.SurfaceShader;

            var activeNodes = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodes, pixelNode);
            var activeMaterialNodes = activeNodes.OfType<AbstractMaterialNode>();

            foreach (var node in activeMaterialNodes)
            {
                if (node is IGeneratesFunction) (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, genMode);
                if (node is IGeneratesVertexToFragmentBlock) (node as IGeneratesVertexToFragmentBlock).GenerateVertexToFragmentBlock(inputStruct, genMode);
                if (node is IGeneratesVertexShaderBlock) (node as IGeneratesVertexShaderBlock).GenerateVertexShaderBlock(vertexShader, genMode);

                if (node is IGenerateProperties)
                {
                    (node as IGenerateProperties).GeneratePropertyBlock(shaderProperties, genMode);
                    (node as IGenerateProperties).GeneratePropertyUsages(propertyUsages, genMode);
                }
            }

            pixelNode.GenerateNodeCode(shaderBody, genMode);
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
