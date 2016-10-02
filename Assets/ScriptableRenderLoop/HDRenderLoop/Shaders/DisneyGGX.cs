using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
internal class DisneyGGXGUI : ShaderGUI
{
	public enum SurfaceType
	{
		Opaque,
		Cutout,
		Transparent
	}
    public enum BlendMode
    {
        Lerp, 
        Add,
        SoftAdd,
        Multiply,
        Premultiply
    }

	private static class Styles
	{
        public static string OptionText = "Options";

        public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
        public static GUIContent doubleSidedText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling)");
        public static GUIContent doubleSidedLightingText = new GUIContent("Double Sided Lighting", "Enable lighting on both side of the objects (flip normal)");


        public static GUIContent baseColorText = new GUIContent("Base Color", "Base Color scale factor");
        public static GUIContent baseColorMapText = new GUIContent("Base Color Map", "Albedo (RGB) and Transparency (A)");
        public static GUIContent ambientOcclusionText = new GUIContent("Ambient Occlusion", "Ambient Occlusion (R)");

        public static GUIContent mettalicText = new GUIContent("Mettalic", "Mettalic scale factor");
        public static GUIContent mettalicMapText = new GUIContent("Mettalic Map", "Mettalic Map");
        public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
        public static GUIContent smoothnessMapText = new GUIContent("Smoothness Map", "Base Color scale factor");
        public static GUIContent specularOcclusionMapText = new GUIContent("Specular Occlusion Map", "Specular Occlusion Map");

        public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC5) - DXT5 for test");

        public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map");
        public static GUIContent heightScaleText = new GUIContent("Height Scale", "eight Map");
        public static GUIContent heightBiasText = new GUIContent("Height Bias", "eight Map");

         // public static GUIContent diffuseLightingMapText = new GUIContent("DiffuseLightingMap", "Lightmap/Lightprobe data (fill by system is not done");

        public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
        public static GUIContent emissiveColorMapText = new GUIContent("Emissive Color Map", "Emissive");
        public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");


        public static string SurfaceTypeText = "Surface Type";
        public static string BlendModeText = "Blend Mode";
        public static string InputsText = "Inputs";

        public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
        public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));
    }

    MaterialProperty surfaceType = null;
    MaterialProperty blendMode = null;
    MaterialProperty alphaCutoff = null;
    MaterialProperty doubleSided = null;
    MaterialProperty doubleSidedLighting = null;

    MaterialProperty baseColor = null;
	MaterialProperty baseColorMap = null;
	MaterialProperty ambientOcclusionMap = null;
	MaterialProperty mettalic = null;
	MaterialProperty mettalicMap = null;
	MaterialProperty smoothness = null;
	MaterialProperty smoothnessMap = null;
	MaterialProperty specularOcclusionMap = null;
	MaterialProperty normalMap = null;
	MaterialProperty heightMap = null;
	MaterialProperty heightScale = null;
	MaterialProperty heightBias = null;
//	MaterialProperty diffuseLightingMap = null;
	MaterialProperty emissiveColor = null;
	MaterialProperty emissiveColorMap = null;
	MaterialProperty emissiveIntensity = null;
//	MaterialProperty subSurfaceRadius = null;
//	MaterialProperty subSurfaceRadiusMap = null;

	MaterialEditor m_MaterialEditor;
	ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, 99f, 1/99f, 3f);

	bool m_FirstTimeApply = true;

	public void FindProperties (MaterialProperty[] props)
	{
        surfaceType = FindProperty("_SurfaceType", props);
        blendMode = FindProperty("_BlendMode", props);
        alphaCutoff = FindProperty("_Cutoff", props);
        doubleSided = FindProperty("_DoubleSided", props);
        doubleSidedLighting = FindProperty("_DoubleSidedLigthing", props);

        baseColor = FindProperty("_BaseColor", props);
        baseColorMap = FindProperty("_BaseColorMap", props);
        ambientOcclusionMap = FindProperty("_AmbientOcclusionMap", props);
        mettalic = FindProperty("_Mettalic", props);
        mettalicMap = FindProperty("_MettalicMap", props);
        smoothness = FindProperty("_Smoothness", props);
        smoothnessMap = FindProperty("_SmoothnessMap", props);
        specularOcclusionMap = FindProperty("_SpecularOcclusionMap", props);
        normalMap = FindProperty("_NormalMap", props);
        heightMap = FindProperty("_HeightMap", props);
        heightScale = FindProperty("_HeightScale", props);
        heightBias = FindProperty("_HeightBias", props);
        // diffuseLightingMap = FindProperty("_DiffuseLightingMap", props);
        emissiveColor = FindProperty("_EmissiveColor", props);
        emissiveColorMap = FindProperty("_EmissiveColorMap", props);
        emissiveIntensity = FindProperty("_EmissiveIntensity", props);
    }

    public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
	{
		FindProperties (props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		m_MaterialEditor = materialEditor;
		Material material = materialEditor.target as Material;

		ShaderPropertiesGUI (material);
	}

	public void ShaderPropertiesGUI (Material material)
	{
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
            GUILayout.Label(Styles.OptionText, EditorStyles.boldLabel);
            SurfaceTypePopup();
            BlendModePopup();

            GUILayout.Label(Styles.InputsText, EditorStyles.boldLabel);

            /*
			m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
			m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
			m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
			DoEmissionArea(material);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
			EditorGUI.BeginChangeCheck();
			m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
			if (EditorGUI.EndChangeCheck())
				emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

			EditorGUILayout.Space();

			// Secondary properties
			GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
			m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
			m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);

			// Third properties
            
			if (highlights != null)
				m_MaterialEditor.ShaderProperty(highlights, Styles.highlightsText);
			if (reflections != null)
				m_MaterialEditor.ShaderProperty(reflections, Styles.reflectionsText);
            */
			
		}
		if (EditorGUI.EndChangeCheck())
		{
			//foreach (var obj in blendMode.targets)
			//	MaterialChanged((Material)obj, m_WorkflowMode);
		}
	}

    // TODO: try to setup minimun value to fall back to standard shaders and reverse
	public override void AssignNewShaderToMaterial (Material material, Shader oldShader, Shader newShader)
	{
		base.AssignNewShaderToMaterial(material, oldShader, newShader);
	}

	void SurfaceTypePopup()
	{
		EditorGUI.showMixedValue = surfaceType.hasMixedValue;
		var mode = (SurfaceType)surfaceType.floatValue;

		EditorGUI.BeginChangeCheck();
        mode = (SurfaceType)EditorGUILayout.Popup(Styles.SurfaceTypeText, (int)mode, Styles.surfaceTypeNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Surface Type");
			surfaceType.floatValue = (float)mode;
		}

		EditorGUI.showMixedValue = false;
	}

	void BlendModePopup()
	{
		EditorGUI.showMixedValue = blendMode.hasMixedValue;
		var mode = (BlendMode)blendMode.floatValue;

		EditorGUI.BeginChangeCheck();
		mode = (BlendMode)EditorGUILayout.Popup(Styles.BlendModeText, (int)mode, Styles.blendModeNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
			blendMode.floatValue = (float)mode;
		}

		EditorGUI.showMixedValue = false;
	}

	public static void SetupMaterialWithBlendMode(Material material, SurfaceType surfaceType, BlendMode blendMode)
	{
        if (surfaceType == SurfaceType.Opaque)
        {
            material.SetOverrideTag("RenderType", "");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }
        else if (surfaceType == SurfaceType.Cutout)
        {
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        else
        {
            switch (blendMode)
            {
                case BlendMode.Lerp:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;

                case BlendMode.Add:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;

                case BlendMode.SoftAdd:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;

                case BlendMode.Multiply:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;

                case BlendMode.Premultiply:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }
        }
	}

	static bool ShouldEmissionBeEnabled(Material mat, Color color)
	{
            //var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            //return color.maxColorComponent > 0.1f / 255.0f || realtimeEmission;

            return false;
	}

	static void SetMaterialKeywords(Material material)
	{
            /*
		// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
		// (MaterialProperty value might come from renderer material property block)
		SetKeyword (material, "_NORMALMAP", material.GetTexture ("_BumpMap") || material.GetTexture ("_DetailNormalMap"));
		if (workflowMode == WorkflowMode.Specular)
			SetKeyword (material, "_SPECGLOSSMAP", material.GetTexture ("_SpecGlossMap"));
		else if (workflowMode == WorkflowMode.Metallic)
			SetKeyword (material, "_METALLICGLOSSMAP", material.GetTexture ("_MetallicGlossMap"));
		SetKeyword (material, "_PARALLAXMAP", material.GetTexture ("_ParallaxMap"));
		SetKeyword (material, "_DETAIL_MULX2", material.GetTexture ("_DetailAlbedoMap") || material.GetTexture ("_DetailNormalMap"));

		bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled (material, material.GetColor("_EmissionColor"));
		SetKeyword (material, "_EMISSION", shouldEmissionBeEnabled);

		if (material.HasProperty("_SmoothnessTextureChannel"))
		{
			SetKeyword (material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
		}

		// Setup lightmap emissive flags
		MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
		if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
		{
			flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
			if (!shouldEmissionBeEnabled)
				flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

			material.globalIlluminationFlags = flags;
		}
        */
	}

	bool HasValidEmissiveKeyword (Material material)
	{
            /*
		// Material animation might be out of sync with the material keyword.
		// So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
		// (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
		bool hasEmissionKeyword = material.IsKeywordEnabled ("_EMISSION");
		if (!hasEmissionKeyword && ShouldEmissionBeEnabled (material, emissionColorForRendering.colorValue))
			return false;
		else
			return true;
            */

            return true;
	}

	static void MaterialChanged(Material material)
	{
		SetupMaterialWithBlendMode(material, (SurfaceType)material.GetFloat("_SurfaceType"), (BlendMode)material.GetFloat("_BlendMode"));

		SetMaterialKeywords(material);
	}

	static void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}
}

} // namespace UnityEditor
