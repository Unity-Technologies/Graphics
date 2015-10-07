using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	public class MaterialGraph : ScriptableObject, IGenerateGraphProperties
	{
		[SerializeField]
		private MaterialProperties m_MaterialProperties;

		[SerializeField]
		private MaterialOptions m_MaterialOptions;

		[SerializeField]
		private PixelGraph m_PixelGraph;
		
		[SerializeField]
		private Shader m_Shader;

	    public int GetShaderInstanceID()
	    {
            Debug.Log("Returning: " + m_Shader.GetInstanceID()); 
	        return m_Shader.GetInstanceID();
	    }
		
		public MaterialProperties materialProperties { get { return m_MaterialProperties; } }
		public MaterialOptions materialOptions { get { return m_MaterialOptions; } }

		public BaseMaterialGraph currentGraph { get { return m_PixelGraph; } }

		public void GenerateSharedProperties(PropertyGenerator shaderProperties, ShaderGenerator propertyUsages, GenerationMode generationMode)
		{
			m_MaterialProperties.GenerateSharedProperties (shaderProperties, propertyUsages, generationMode);
		}

		public IEnumerable<ShaderProperty> GetPropertiesForPropertyType (PropertyType propertyType)
		{
			return m_MaterialProperties.GetPropertiesForPropertyType(propertyType);
		}

		public void OnEnable ()
		{
			if (m_MaterialProperties == null)
			{
				m_MaterialProperties = CreateInstance<MaterialProperties>();
				m_MaterialProperties.hideFlags = HideFlags.HideInHierarchy;
			}

			if (m_MaterialOptions == null)
			{
				m_MaterialOptions = CreateInstance<MaterialOptions> ();
				m_MaterialOptions.Init ();
				m_MaterialOptions.hideFlags = HideFlags.HideInHierarchy;
			}

			if (m_PixelGraph == null)
			{
				m_PixelGraph = CreateInstance<PixelGraph>();
				m_PixelGraph.hideFlags = HideFlags.HideInHierarchy;
				m_PixelGraph.name = name;
			}

			m_PixelGraph.owner = this;
		}

		public void OnDisable ()
		{
	//		if (m_MaterialProperties != null)
		//		m_MaterialProperties.OnChangePreviewState -= OnChangePreviewState;
		}

		void OnChangePreviewState (object sender, EventArgs eventArgs)
		{
			m_PixelGraph.previewState = (PreviewState)sender;
		}
		
		public void UpdateShaderSource (string src, Dictionary<string, Texture> defaultTexutres)
		{
			UnityEditor.ShaderUtil.UpdateShaderAsset (m_Shader, src);
			EditorMaterialUtility.SetShaderDefaults (m_Shader, defaultTexutres.Keys.ToArray (), defaultTexutres.Values.ToArray ());
		}

		public void CreateSubAssets ()
		{
			AssetDatabase.AddObjectToAsset (m_MaterialProperties, this);
			AssetDatabase.AddObjectToAsset (m_MaterialOptions, this);
			AssetDatabase.AddObjectToAsset (m_PixelGraph, this);

			if (m_Shader == null)
			{
				const string shaderSource = "Shader \"Graphs/Dummy\" {" +
					"Properties { _Color (\"Main Color\", Color) = (1,1,1,0) }" +
					"SubShader {" +
					"    Tags { \"Queue\" = \"Transparent\" }" +
					"    Pass {" +
					"        Blend One One ZWrite Off ColorMask RGB" +
					"        Material { Diffuse [_Color] Ambient [_Color] }" +
					"        Lighting On" +
					"        SetTexture [_Dummy] { combine primary double, primary }" +
					"    }" +
					"}" +
					"}";

				m_Shader = UnityEditor.ShaderUtil.CreateShaderAsset(shaderSource);
				m_Shader.name = name;
				m_Shader.hideFlags = HideFlags.HideInHierarchy;
			}
			AssetDatabase.AddObjectToAsset (m_Shader, this);
		}
	}
}
