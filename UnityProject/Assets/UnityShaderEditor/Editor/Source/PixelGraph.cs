using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    class PixelGraph : BaseMaterialGraph
    {
        private PixelShaderNode m_PixelMasterNode;
        
        public PixelShaderNode pixelMasterNode
        {
            get
            {
                ConfigureMasterNode(true);
                return m_PixelMasterNode;
            }
        }

        private void ConfigureMasterNode(bool addToAsset)
        {
            if (m_PixelMasterNode == null)
                m_PixelMasterNode = nodes.FirstOrDefault(x => x.GetType() == typeof(PixelShaderNode)) as PixelShaderNode;

            if (m_PixelMasterNode == null)
            {
                m_PixelMasterNode = CreateInstance<PixelShaderNode>();
                m_PixelMasterNode.OnCreate();
                m_PixelMasterNode.position = new Rect(700, m_PixelMasterNode.position.y, m_PixelMasterNode.position.width, m_PixelMasterNode.position.height);
                if (addToAsset)
                    AddNode(m_PixelMasterNode);
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            ConfigureMasterNode(false);
        }
        
        public void AddSubAssetsToAsset()
        {
            AddNodeNoValidate(m_PixelMasterNode);
        }

        private List<BaseMaterialNode> m_ActiveNodes = new List<BaseMaterialNode>();
        public IEnumerable<BaseMaterialNode> activeNodes
        {
            get
            {
                m_ActiveNodes.Clear();
                pixelMasterNode.CollectChildNodesByExecutionOrder(m_ActiveNodes);
                return m_ActiveNodes;
            }
        }

        public MaterialGraph owner { get; set; }
        public void GenerateSurfaceShader(
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
            pixelMasterNode.GenerateLightFunction(lightFunction);
            pixelMasterNode.GenerateSurfaceOutput(surfaceOutput);

            var genMode = isPreview ? GenerationMode.Preview3D : GenerationMode.SurfaceShader;
            
            foreach (var node in activeNodes)
            {
                if (node is IGeneratesFunction) (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, genMode);
                if (node is IGeneratesVertexToFragmentBlock) (node as IGeneratesVertexToFragmentBlock).GenerateVertexToFragmentBlock(inputStruct, genMode);
                if (node is IGeneratesVertexShaderBlock) (node as IGeneratesVertexShaderBlock).GenerateVertexShaderBlock(vertexShader, genMode);

                if (node is IGenerateProperties)
                {
                    (node as IGenerateProperties).GeneratePropertyBlock(shaderProperties, genMode);
                    (node as IGenerateProperties).GeneratePropertyUsages(propertyUsages, genMode, ConcreteSlotValueType.Vector4);
                }
            }

            pixelMasterNode.GenerateNodeCode(shaderBody, genMode);
        }

        public Material GetMaterial()
        {
            if (pixelMasterNode == null)
                return null;

            var material = pixelMasterNode.previewMaterial;
            BaseMaterialNode.UpdateMaterialProperties(pixelMasterNode, material);
            return material;
        }
    }
}
