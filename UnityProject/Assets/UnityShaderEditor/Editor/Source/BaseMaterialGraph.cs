using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{

	public interface IGenerateGraphProperties
	{
		void GenerateSharedProperties (PropertyGenerator shaderProperties, ShaderGenerator propertyUsages, GenerationMode generationMode);
		IEnumerable<ShaderProperty> GetPropertiesForPropertyType (PropertyType propertyType);
	}

	public abstract class BaseMaterialGraph : Graph
	{
		public abstract BaseMaterialNode masterNode { get; }

		private PreviewRenderUtility m_PreviewUtility;

		public PreviewRenderUtility previewUtility
		{
			get 
			{
				if (m_PreviewUtility == null)
					m_PreviewUtility = new PreviewRenderUtility ();

				return m_PreviewUtility;
			}
		}

		public bool requiresRepaint
		{
			get { return isAwake && nodes.Any (x => x is IRequiresTime); }
		}

		public override void RemoveEdge (Edge e)
		{
			base.RemoveEdge (e);

			var toNode = e.toSlot.node as BaseMaterialNode;
			if (toNode == null)
				return;

			toNode.RegeneratePreviewShaders ();
		}

		public override Edge Connect(Slot fromSlot, Slot toSlot)
		{
			var edge = base.Connect (fromSlot, toSlot);
			var toNode = toSlot.node as BaseMaterialNode;
			var fromNode = fromSlot.node as BaseMaterialNode;

			if (fromNode == null || toNode == null)
				return edge;

			toNode.RegeneratePreviewShaders();
			fromNode.CollectChildNodesByExecutionOrder().ToList ().ForEach (s => s.UpdatePreviewProperties());

			return edge;
		}

		public override void AddNode(Node node)
		{
			base.AddNode(node);
			AssetDatabase.AddObjectToAsset (node, this);

			var bmn = node as BaseMaterialNode;
			if (bmn != null && bmn.hasPreview )
				bmn.UpdatePreviewMaterial ();
		}

		protected void AddMasterNodeNoAddToAsset (Node node)
		{
			base.AddNode (node);
		}

		public void GeneratePreviewShaders ()
		{
			MaterialWindow.DebugMaterialGraph ("Generating preview shaders on: " + name);
		
			// 2 passes...
			// 1 create the shaders
			foreach (var node in nodes)
			{
				var bmn = node as BaseMaterialNode;
				if (bmn != null && bmn.hasPreview)
				{
					bmn.UpdatePreviewMaterial ();
				}
			}

			// 2 set the properties
			foreach (var node in nodes)
			{
				var pNode = node as BaseMaterialNode;
				if (pNode != null)
				{
					MaterialWindow.DebugMaterialGraph ("Updating preview Properties on Node: " + pNode);
					pNode.UpdatePreviewProperties ();
				}
			}
		}
	}
}
