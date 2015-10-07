using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    class PixelGraph : BaseMaterialGraph, IGenerateGraphProperties
    {
        private PixelShaderNode m_PixelMasterNode;

        public PreviewState previewState { get; set; }

        public override BaseMaterialNode masterNode
        {
            get { return pixelMasterNode; }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_PixelMasterNode == null)
                m_PixelMasterNode = nodes.FirstOrDefault(x => x.GetType() == typeof (PixelShaderNode)) as PixelShaderNode;

            if (m_PixelMasterNode == null)
            {
                m_PixelMasterNode = CreateInstance<PixelShaderNode>();
                m_PixelMasterNode.hideFlags = HideFlags.HideInHierarchy;
                m_PixelMasterNode.Init();
                m_PixelMasterNode.position = new Rect(700, m_PixelMasterNode.position.y, m_PixelMasterNode.position.width, m_PixelMasterNode.position.height);
                AddMasterNodeNoAddToAsset(m_PixelMasterNode);
            }
        }

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
                    m_PixelMasterNode.Init();
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
                    (node as IGenerateProperties).GeneratePropertyUsages(propertyUsages, genMode);
                }
            }

            pixelMasterNode.GenerateNodeCode(shaderBody, genMode);
        }

        public void AddMasterNodeToAsset()
        {
            AssetDatabase.AddObjectToAsset(pixelMasterNode, this);
        }

        protected override void RecacheActiveNodes()
        {
            m_ActiveNodes = pixelMasterNode.CollectChildNodesByExecutionOrder();
        }
    }
}
