using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    class PixelGraph : BaseMaterialGraph, IGenerateGraphProperties
    {
        private PixelShaderNode m_PixelMasterNode;

        public PreviewState previewState { get; set; }

        public PixelShaderNode pixelMasterNode
        {
            get
            {
                if (m_PixelMasterNode == null)
                    m_PixelMasterNode = nodes.FirstOrDefault(x => x.GetType() == typeof(PixelShaderNode)) as PixelShaderNode;

                if (m_PixelMasterNode == null)
                {
                    m_PixelMasterNode = CreateInstance<PixelShaderNode>();
                    m_PixelMasterNode.hideFlags = HideFlags.HideInHierarchy;
                    m_PixelMasterNode.OnCreate();
                    m_PixelMasterNode.position = new Rect(700, pixelMasterNode.position.y, pixelMasterNode.position.width, pixelMasterNode.position.height);
                    AddNode(m_PixelMasterNode);
                }

                return m_PixelMasterNode;
            }
        }

        private IEnumerable<BaseMaterialNode> m_ActiveNodes;
        public IEnumerable<BaseMaterialNode> activeNodes
        {
            get
            {
               if (m_ActiveNodes == null)
                    m_ActiveNodes = pixelMasterNode.CollectChildNodesByExecutionOrder();
                return m_ActiveNodes;
            }
        }
        
        public void GenerateSharedProperties(PropertyGenerator shaderProperties, ShaderGenerator propertyUsages, GenerationMode generationMode)
        {
            owner.GenerateSharedProperties(shaderProperties, propertyUsages, generationMode);
        }

        public IEnumerable<ShaderProperty> GetPropertiesForPropertyType(PropertyType propertyType)
        {
            return owner.GetPropertiesForPropertyType(propertyType);
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

            owner.materialProperties.GenerateSharedProperties(shaderProperties, propertyUsages, genMode);

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

        protected override void RecacheActiveNodes()
        {
            m_ActiveNodes = pixelMasterNode.CollectChildNodesByExecutionOrder();
        }

        public Material GetMaterial()
        {
            if (pixelMasterNode == null)
                return null;

            return pixelMasterNode.previewMaterial;
        }

        protected override void UpdateNodeErrorState()
        {
            var bmns = nodes.Where(x => x is BaseMaterialNode).Cast<BaseMaterialNode>().ToList();

            foreach (var node in bmns)
                node.errorsCalculated = false;
        }
    }
}
