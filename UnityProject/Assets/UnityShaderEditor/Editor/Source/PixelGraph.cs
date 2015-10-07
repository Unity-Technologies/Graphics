using System;
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
            ShaderGenerator vertexShader)
        {
            pixelMasterNode.GenerateLightFunction(lightFunction);
            pixelMasterNode.GenerateSurfaceOutput(surfaceOutput);

            owner.materialProperties.GenerateSharedProperties(shaderProperties, propertyUsages, GenerationMode.SurfaceShader);

            foreach (var node in activeNodes)
            {
                if (node is IGeneratesFunction) (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, GenerationMode.SurfaceShader);
                if (node is IGeneratesVertexToFragmentBlock) (node as IGeneratesVertexToFragmentBlock).GenerateVertexToFragmentBlock(inputStruct, GenerationMode.SurfaceShader);
                if (node is IGeneratesVertexShaderBlock) (node as IGeneratesVertexShaderBlock).GenerateVertexShaderBlock(vertexShader, GenerationMode.SurfaceShader);

                if (node is IGenerateProperties)
                {
                    (node as IGenerateProperties).GeneratePropertyBlock(shaderProperties, GenerationMode.SurfaceShader);
                    (node as IGenerateProperties).GeneratePropertyUsages(propertyUsages, GenerationMode.SurfaceShader);
                }
            }

            pixelMasterNode.GenerateNodeCode(shaderBody, GenerationMode.SurfaceShader);
        }

        public override void RemoveEdge(Edge e)
        {
            base.RemoveEdge(e);
            m_ActiveNodes = pixelMasterNode.CollectChildNodesByExecutionOrder();
        }

        public override Edge Connect(Slot fromSlot, Slot toSlot)
        {
            var ret = base.Connect(fromSlot, toSlot);
            m_ActiveNodes = pixelMasterNode.CollectChildNodesByExecutionOrder();
            return ret;
        }
    }
}
