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

    public enum SmoothnessMapChannel
    {
        MaskAlpha,
        AlbedoAlpha,
    }
    public enum EmissiveColorMode
    {
        UseEmissiveColor,
        UseEmissiveMask,
    }
    public enum DoubleSidedMode
    {
        None,
        DoubleSided,
        DoubleSidedLightingFlip,
        DoubleSidedLightingMirror,
    }

	private static class Styles
	{
        public static string OptionText = "Options";
        public static string SurfaceTypeText = "Surface Type";
        public static string BlendModeText = "Blend Mode";

        public static GUIContent alphaCutoffEnableText = new GUIContent("Alpha Cutoff Enable", "Threshold for alpha cutoff");        
        public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
        public static GUIContent doubleSidedModeText = new GUIContent("Double Sided", "This will render the two face of the objects (disable backface culling)");

        public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
        public static readonly string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

        public static string InputsOptionsText = "Inputs options";

        public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
        public static GUIContent emissiveColorModeText = new GUIContent("Emissive Color Usage", "Use emissive color or emissive mask");

        public static string InputsText = "Inputs";

        public static string InputsMapText = "";

        public static GUIContent baseColorText = new GUIContent("Base Color", "Albedo (RGB) and Smoothness (A)");
        public static GUIContent baseColorSmoothnessText = new GUIContent("Base Color + Smoothness", "Albedo (RGB) and Smoothness (A)");
 
        public static GUIContent mettalicText = new GUIContent("Mettalic", "Mettalic scale factor");
        public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
        public static GUIContent maskMapESText = new GUIContent("Mask Map - M(R), AO(G), E(B), S(A)", "Mask map");
        public static GUIContent maskMapEText = new GUIContent("Mask Map - M(R), AO(G), E(B)", "Mask map");
        public static GUIContent maskMapText = new GUIContent("Mask Map - M(R), AO(G)", "Mask map");
        public static GUIContent maskMapSText = new GUIContent("Mask Map - M(R), AO(G), S(A)", "Mask map");

        public static GUIContent specularOcclusionMapText = new GUIContent("Specular Occlusion Map (RGBA)", "Specular Occlusion Map");

        public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC5) - DXT5 for test");

        public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map");

         // public static GUIContent diffuseLightingMapText = new GUIContent("DiffuseLightingMap", "Lightmap/Lightprobe data (fill by system is not done");

        public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
        public static GUIContent emissiveIntensityText = new GUIContent("Emissive Intensity", "Emissive");
    }

    MaterialProperty surfaceType = null;
    MaterialProperty blendMode = null;
    MaterialProperty alphaCutoff = null;
    MaterialProperty alphaCutoffEnable = null;    
    MaterialProperty doubleSidedMode = null;
    MaterialProperty smoothnessMapChannel = null;
    MaterialProperty emissiveColorMode = null;

    MaterialProperty baseColor = null;
	MaterialProperty baseColorMap = null;
	MaterialProperty mettalic = null;
	MaterialProperty smoothness = null;
    MaterialProperty maskMap = null;
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
        alphaCutoffEnable = FindProperty("_AlphaCutoffEnable", props);
        doubleSidedMode = FindProperty("_DoubleSidedMode", props);
        smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", props);
        emissiveColorMode = FindProperty("_EmissiveColorMode", props);

        baseColor = FindProperty("_BaseColor", props);
        baseColorMap = FindProperty("_BaseColorMap", props);
        mettalic = FindProperty("_Mettalic", props);
        smoothness = FindProperty("_Smoothness", props);
        maskMap = FindProperty("_MaskMap", props);
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
            m_MaterialEditor.ShaderProperty(alphaCutoffEnable, Styles.alphaCutoffEnableText.text);
            if (alphaCutoffEnable.floatValue == 1.0)
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text);
            }
            if ((SurfaceType)surfaceType.floatValue == SurfaceType.Transparent)
            {
                BlendModePopup();
            }
            m_MaterialEditor.ShaderProperty(doubleSidedMode, Styles.doubleSidedModeText.text);

            GUILayout.Label(Styles.InputsOptionsText, EditorStyles.boldLabel);
            m_MaterialEditor.ShaderProperty(smoothnessMapChannel, Styles.smoothnessMapChannelText.text);
            m_MaterialEditor.ShaderProperty(emissiveColorMode, Styles.emissiveColorModeText.text);

            bool isAlbedoAlpha = (SmoothnessMapChannel)smoothnessMapChannel.floatValue == SmoothnessMapChannel.AlbedoAlpha;
            bool useEmissiveMask = (EmissiveColorMode)emissiveColorMode.floatValue == EmissiveColorMode.UseEmissiveMask;

            GUILayout.Label(Styles.InputsText, EditorStyles.boldLabel);
            m_MaterialEditor.TexturePropertySingleLine(isAlbedoAlpha ? Styles.baseColorSmoothnessText : Styles.baseColorText, baseColorMap, baseColor);
            m_MaterialEditor.ShaderProperty(mettalic, Styles.mettalicText);
            m_MaterialEditor.ShaderProperty(smoothness, Styles.smoothnessText);

            if (isAlbedoAlpha && useEmissiveMask)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapESText, maskMap);
            else if (useEmissiveMask)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapEText, maskMap);
            else if (isAlbedoAlpha)
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapSText, maskMap);
            else
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapText, maskMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.specularOcclusionMapText, specularOcclusionMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);

            m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightScale, heightBias);
            
            if (!useEmissiveMask)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            }
            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);	
		}

		if (EditorGUI.EndChangeCheck())
		{
			foreach (var obj in blendMode.targets)
				MaterialChanged((Material)obj);
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

	public void SetupMaterialWithBlendMode(Material material, bool alphaTestEnable, SurfaceType surfaceType, BlendMode blendMode, DoubleSidedMode doubleSidedMode)
	{
        if (alphaTestEnable)
            material.EnableKeyword("_ALPHATEST_ON");

        if (surfaceType == SurfaceType.Opaque)
        {
            material.SetOverrideTag("RenderType", alphaTestEnable ? "TransparentCutout" : "");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = alphaTestEnable ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
        }
        else
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            switch (blendMode)
            {
                case BlendMode.Lerp: 
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;

                case BlendMode.Add:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    break;

                case BlendMode.SoftAdd:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    break;

                case BlendMode.Multiply:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;

                case BlendMode.Premultiply:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);                   
                    break;
            }
        }

        if (doubleSidedMode == DoubleSidedMode.DoubleSided)
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
        }
        else if (doubleSidedMode == DoubleSidedMode.DoubleSided)
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            material.EnableKeyword("_DOUBLESIDED_LIGHTING_FLIP");
        }
        else
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
            material.EnableKeyword("_DOUBLESIDED_LIGHTING_MIRROR");
        }        
	}

	static bool ShouldEmissionBeEnabled(Material mat, Color color)
	{
            //var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            //return color.maxColorComponent > 0.1f / 255.0f || realtimeEmission;

            return false;
	}

	void SetMaterialKeywords(Material material)
	{
        // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
        // (MaterialProperty value might come from renderer material property block)
        SetKeyword(material, "_NORMALMAP", material.GetTexture("_NormalMap"));
        SetKeyword(material, "_MASKMAP", material.GetTexture("_MaskMap"));
        SetKeyword(material, "_SPECULAROCCLUSIONMAP", material.GetTexture("_SpecularOcclusionMap"));
        SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", ((SmoothnessMapChannel)material.GetFloat("_SmoothnessTextureChannel")) == SmoothnessMapChannel.AlbedoAlpha);
        SetKeyword(material, "_EMISSIVE_COLOR", ((EmissiveColorMode)material.GetFloat("_EmissiveColorMode")) == EmissiveColorMode.UseEmissiveColor);

        /*
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

	void MaterialChanged(Material material)
	{
        SetupMaterialWithBlendMode(material, alphaCutoffEnable.floatValue == 1.0, (SurfaceType)surfaceType.floatValue, (BlendMode)blendMode.floatValue, (DoubleSidedMode)doubleSidedMode.floatValue);

		SetMaterialKeywords(material);
	}

	void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}
}

} // namespace UnityEditor
