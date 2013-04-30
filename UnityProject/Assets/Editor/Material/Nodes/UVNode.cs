using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	[Title("Input/UV Node")]
	public class UVNode : BaseMaterialNode, IGeneratesVertexToFragmentBlock, IGeneratesVertexShaderBlock, IGeneratesBodyCode
	{
		private const string kOutputSlotName = "UV";

		public override bool hasPreview { get { return true; } }

		[SerializeField]
		private ShaderProperty m_BoundProperty;

		public override void Init ()
		{
			base.Init ();
			name = "UV";
			AddSlot (new Slot (SlotType.OutputSlot, kOutputSlotName));
		}

		public static void GenerateVertexToFragmentBlock (ShaderGenerator visitor)
		{
			visitor.AddShaderChunk("half4 meshUV0;", true);
		}

		public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
		{
			GenerateVertexToFragmentBlock (visitor);
		}

		public static void GenerateVertexShaderBlock(ShaderGenerator visitor)
		{
			visitor.AddShaderChunk("o.meshUV0 = v.texcoord;", true);
		}

		public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
		{
			GenerateVertexShaderBlock(visitor);
		}

		public void GenerateNodeCode (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputSlot = FindOutputSlot (kOutputSlotName);

			string uvValue = "IN.meshUV0";

			if (m_BoundProperty != null && m_BoundProperty is TextureProperty)
				uvValue = precision + "4 (TRANSFORM_TEX(IN.meshUV0, " + m_BoundProperty.name + "), 0.0, 0.0)";

			visitor.AddShaderChunk(precision + "4 " + GetOutputVariableNameForSlot(outputSlot, generationMode) + " = " + uvValue + ";", true);
		}

		// UI Shizz
		public override void NodeUI(GraphGUI host)
		{
			base.NodeUI(host);

			var configuredTextureProperties = new ShaderProperty[0];
			if (graph is IGenerateGraphProperties)
				configuredTextureProperties = (graph as IGenerateGraphProperties).GetPropertiesForPropertyType(PropertyType.Texture2D).ToArray ();

			var names = new List<string> { "none" };
			names.AddRange(configuredTextureProperties.Select(x => x.name));
			var currentIndex = names.IndexOf(m_BoundProperty == null ? "none" : m_BoundProperty.name);

			EditorGUI.BeginChangeCheck();
			currentIndex = EditorGUILayout.Popup("Bound Property", currentIndex, names.ToArray());
			if (EditorGUI.EndChangeCheck())
			{
				m_BoundProperty = null;
				if (currentIndex > 0)
					m_BoundProperty = configuredTextureProperties[currentIndex - 1];

				RegeneratePreviewShaders();
			}
		}

		public virtual void RefreshBoundProperty (ShaderProperty toRefresh, bool rebuildShader)
		{
			if (m_BoundProperty != null && m_BoundProperty == toRefresh)
			{
				if (rebuildShader)
					RegeneratePreviewShaders();
				else
					UpdatePreviewProperties();
			}
		}

		public void UnbindProperty (ShaderProperty prop)
		{
			if (m_BoundProperty != null && m_BoundProperty == prop)
			{
				m_BoundProperty = null;
				RegeneratePreviewShaders();
			}
		}
	}
}
