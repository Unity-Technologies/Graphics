using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	class PixelGraph : BaseMaterialGraph, IGenerateGraphProperties
	{
		[SerializeField]
		private PixelShaderNode m_MasterNode;

		public PreviewState previewState { get; set; }

		public override BaseMaterialNode masterNode
		{
			get { return m_MasterNode; }
		}

		new void OnEnable ()
		{
			base.OnEnable ();
			if (m_MasterNode == null)
			{
				m_MasterNode = CreateInstance<PixelShaderNode> ();
				m_MasterNode.hideFlags = HideFlags.HideInHierarchy;
				m_MasterNode.Init ();
				m_MasterNode.position = new Rect(700, m_MasterNode.position.y, m_MasterNode.position.width, m_MasterNode.position.height);
				AddMasterNodeNoAddToAsset (m_MasterNode);
			}
		}

		private IEnumerable<BaseMaterialNode> m_ActiveNodes;
		public IEnumerable<BaseMaterialNode> activeNodes
		{
			get
			{
				if (m_ActiveNodes == null)
					m_ActiveNodes = m_MasterNode.CollectChildNodesByExecutionOrder();
				return m_ActiveNodes;
			}
		}

		public void GenerateSharedProperties (PropertyGenerator shaderProperties, ShaderGenerator propertyUsages, GenerationMode generationMode)
		{
			owner.GenerateSharedProperties (shaderProperties, propertyUsages, generationMode);
		}

		public IEnumerable<ShaderProperty> GetPropertiesForPropertyType (PropertyType propertyType) 
		{
			return owner.GetPropertiesForPropertyType(propertyType);
		}


		public MaterialGraph owner { get; set; }
		public void GenerateSurfaceShader(
			ShaderGenerator shaderBody,
			ShaderGenerator inputStruct,
			ShaderGenerator lightFunction,
			ShaderGenerator nodeFunction,
			PropertyGenerator shaderProperties,
			ShaderGenerator propertyUsages,
			ShaderGenerator vertexShader)
		{
			m_MasterNode.GenerateLightFunction(lightFunction);

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

			m_MasterNode.GenerateNodeCode(shaderBody, GenerationMode.SurfaceShader);

			if (m_MasterNode.IsSpecularConnected())
				shaderProperties.AddShaderProperty(new ColorPropertyChunk("_SpecColor", "Specular Color", Color.grey, false));
		}

		public void AddMasterNodeToAsset ()
		{
			AssetDatabase.AddObjectToAsset (m_MasterNode, this);
		}

		public override void RemoveEdge (Edge e)
		{
			base.RemoveEdge(e);
			m_ActiveNodes = m_MasterNode.CollectChildNodesByExecutionOrder();
		}

		public override Edge Connect (Slot fromSlot, Slot toSlot)
		{
			var ret = base.Connect(fromSlot, toSlot);
			m_ActiveNodes = m_MasterNode.CollectChildNodesByExecutionOrder();
			return ret;
		}
	}
}
