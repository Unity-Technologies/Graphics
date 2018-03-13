using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Experimental.Rendering;

public class LightweightStandardSimpleLightingGUI : LightweightShaderGUI
{
    private const float kMinShininessValue = 0.01f;
    private MaterialProperty surfaceTypeProp;
    private MaterialProperty blendModeProp;
    private MaterialProperty culling;
    private MaterialProperty alphaClip;
    private MaterialProperty albedoMapProp;
    private MaterialProperty albedoColorProp;
    private MaterialProperty alphaThresholdProp;
    private MaterialProperty specularSourceProp;
    private MaterialProperty glossinessSourceProp;
    private MaterialProperty specularGlossMapProp;
    private MaterialProperty specularColorProp;
    private MaterialProperty shininessProp;
    private MaterialProperty bumpMapProp;
    private MaterialProperty emissionMapProp;
    private MaterialProperty emissionColorProp;

    private static class Styles
    {
        public static GUIContent twoSidedLabel = new GUIContent("Two Sided", "Render front and back faces");
        public static GUIContent alphaClipLabel = new GUIContent("Alpha Clip", "Enable Alpha Clip");

        public static GUIContent[] albedoGlosinessLabels =
        {
            new GUIContent("Base (RGB) Glossiness (A)", "Base Color (RGB) and Glossiness (A)"),
            new GUIContent("Base (RGB)", "Base Color (RGB)")
        };

        public static GUIContent albedoAlphaLabel = new GUIContent("Base (RGB) Alpha (A)",
                "Base Color (RGB) and Transparency (A)");

        public static GUIContent[] specularGlossMapLabels =
        {
            new GUIContent("Specular Map (RGB)", "Specular Color (RGB)"),
            new GUIContent("Specular Map (RGB) Glossiness (A)", "Specular Color (RGB) Glossiness (A)")
        };

        public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
        public static GUIContent emissionMapLabel = new GUIContent("Emission Map", "Emission Map");

        public static readonly string[] surfaceNames = Enum.GetNames(typeof(SurfaceType));
        public static readonly string[] blendNames = Enum.GetNames(typeof(UpgradeBlendMode));
        public static readonly string[] glossinessSourceNames = Enum.GetNames(typeof(GlossinessSource));

        public static string surfaceTypeLabel = "Surface Type";
        public static string blendingModeLabel = "Blending Mode";
        public static string specularSourceLabel = "Specular";
        public static string glossinessSourceLabel = "Glossiness Source";
        public static string glossinessSource = "Glossiness Source";
        public static string albedoColorLabel = "Base Color";
        public static string albedoMapAlphaLabel = "Base(RGB) Alpha(A)";
        public static string albedoMapGlossinessLabel = "Base(RGB) Glossiness (A)";
        public static string clipThresholdLabel = "Clip Threshold";
        public static string shininessLabel = "Shininess";
        public static string normalMapLabel = "Normal map";
        public static string emissionColorLabel = "Emission Color";
        public static string advancedText = "Advanced Options";
    }

    public override void FindProperties(MaterialProperty[] properties)
    {
        surfaceTypeProp = FindProperty("_Surface", properties);
        blendModeProp = FindProperty("_Blend", properties);
        culling = FindProperty("_Cull", properties);
        alphaClip  = FindProperty("_AlphaClip", properties);
        albedoMapProp = FindProperty("_MainTex", properties);
        albedoColorProp = FindProperty("_Color", properties);
        alphaThresholdProp = FindProperty("_Cutoff", properties);
        specularSourceProp = FindProperty("_SpecSource", properties);
        glossinessSourceProp = FindProperty("_GlossinessSource", properties);
        specularGlossMapProp = FindProperty("_SpecGlossMap", properties);
        specularColorProp = FindProperty("_SpecColor", properties);
        shininessProp = FindProperty("_Shininess", properties);
        bumpMapProp = FindProperty("_BumpMap", properties);
        emissionMapProp = FindProperty("_EmissionMap", properties);
        emissionColorProp = FindProperty("_EmissionColor", properties);
    }

    public override void ShaderPropertiesGUI(Material material)
    {
        EditorGUI.BeginChangeCheck();
        {
            DoSurfaceArea();
            DoSpecular();

            EditorGUILayout.Space();
            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMapProp);

            EditorGUILayout.Space();
            DoEmissionArea(material);

            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.TextureScaleOffsetProperty(albedoMapProp);
            if (EditorGUI.EndChangeCheck())
                emissionMapProp.textureScaleAndOffset = albedoMapProp.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

            GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_MaterialEditor.EnableInstancingField();
            EditorGUI.indentLevel--;
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in blendModeProp.targets)
                MaterialChanged((Material)obj);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    public override void MaterialChanged(Material material)
    {
        material.shaderKeywords = null;
        SetupMaterialBlendMode(material);
        SetMaterialKeywords(material);
    }

    private void SetMaterialKeywords(Material material)
    {
        material.shaderKeywords = null;
        SetupMaterialBlendMode(material);
        UpdateMaterialSpecularSource(material);
        CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));

        // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
        // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
        // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
        MaterialEditor.FixupEmissiveFlag(material);
        bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
        CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
    }

    private void UpdateMaterialSpecularSource(Material material)
    {
        SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecSource");
        if (specSource == SpecularSource.NoSpecular)
        {
            CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", false);
            CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", false);
            CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
        }
        else
        {
            GlossinessSource glossSource = (GlossinessSource)material.GetFloat("_GlossinessSource");
            bool hasGlossMap = material.GetTexture("_SpecGlossMap");
            CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap);
            CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasGlossMap);
            CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", glossSource == GlossinessSource.BaseAlpha);
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        // Shininess value cannot be zero since it will produce undefined values for cases where pow(0, 0).
        float shininess = material.GetFloat("_Shininess");
        material.SetFloat("_Shininess", Mathf.Clamp(shininess, kMinShininessValue, 1.0f));

        string oldShaderName = oldShader.name;
        string[] shaderStrings = oldShaderName.Split('/');

        if (shaderStrings[0].Equals("Legacy Shaders") || shaderStrings[0].Equals("Mobile"))
        {
            ConvertFromLegacy(material, oldShaderName);
        }

        StandardSimpleLightingUpgrader.UpdateMaterialKeywords(material);
    }

    private void DoSurfaceArea()
    {
        int surfaceTypeValue = (int)surfaceTypeProp.floatValue;
        EditorGUI.BeginChangeCheck();
        surfaceTypeValue = EditorGUILayout.Popup(Styles.surfaceTypeLabel, surfaceTypeValue, Styles.surfaceNames);
        if (EditorGUI.EndChangeCheck())
            surfaceTypeProp.floatValue = surfaceTypeValue;

        if((SurfaceType)surfaceTypeValue == SurfaceType.Transparent)
        {
            int blendModeValue = (int)blendModeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            blendModeValue = EditorGUILayout.Popup(Styles.blendingModeLabel, blendModeValue, Styles.blendNames);
            if (EditorGUI.EndChangeCheck())
                blendModeProp.floatValue = blendModeValue;
        }

        EditorGUI.BeginChangeCheck();
        bool twoSidedEnabled = EditorGUILayout.Toggle(Styles.twoSidedLabel, culling.floatValue == 0);
        if (EditorGUI.EndChangeCheck())
            culling.floatValue = twoSidedEnabled ? 0 : 2;

        EditorGUI.BeginChangeCheck();
        bool alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipLabel, alphaClip.floatValue == 1);
        if (EditorGUI.EndChangeCheck())
            alphaClip.floatValue = alphaClipEnabled ? 1 : 0;
        
        EditorGUILayout.Space();

        if ((SurfaceType)surfaceTypeValue == SurfaceType.Opaque)
        {
            int glossSource = (int)glossinessSourceProp.floatValue;
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoGlosinessLabels[glossSource], albedoMapProp,
                albedoColorProp);
        }
        else
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoAlphaLabel, albedoMapProp, albedoColorProp);
        }
        if (alphaClipEnabled)
            m_MaterialEditor.ShaderProperty(alphaThresholdProp, Styles.clipThresholdLabel, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
    }

    private void DoSpecular()
    {
        EditorGUILayout.Space();

        SpecularSource specularSource = (SpecularSource)specularSourceProp.floatValue;
        EditorGUI.BeginChangeCheck();
        bool enabled = EditorGUILayout.Toggle(Styles.specularSourceLabel, specularSource == SpecularSource.SpecularTextureAndColor);
        if (EditorGUI.EndChangeCheck())
            specularSourceProp.floatValue = enabled ? (float)SpecularSource.SpecularTextureAndColor : (float)SpecularSource.NoSpecular;

        SpecularSource specSource = (SpecularSource)specularSourceProp.floatValue;
        if (specSource != SpecularSource.NoSpecular)
        {
            bool hasSpecularMap = specularGlossMapProp.textureValue != null;
            m_MaterialEditor.TexturePropertySingleLine(Styles.specularGlossMapLabels[(int)glossinessSourceProp.floatValue], specularGlossMapProp, hasSpecularMap ? null : specularColorProp);

            EditorGUI.indentLevel += 2;
            GUI.enabled = hasSpecularMap;
            int glossinessSource = hasSpecularMap ? (int)glossinessSourceProp.floatValue : (int)GlossinessSource.BaseAlpha;
            EditorGUI.BeginChangeCheck();
            glossinessSource = EditorGUILayout.Popup(Styles.glossinessSourceLabel, glossinessSource, Styles.glossinessSourceNames);
            if (EditorGUI.EndChangeCheck())
                glossinessSourceProp.floatValue = glossinessSource;
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            float shininess = EditorGUILayout.Slider(Styles.shininessLabel, shininessProp.floatValue,
                    kMinShininessValue, 1.0f);
            if (EditorGUI.EndChangeCheck())
                shininessProp.floatValue = shininess;
            EditorGUI.indentLevel -= 2;
        }
    }

    void DoEmissionArea(Material material)
    {
        // Emission for GI?
        if (m_MaterialEditor.EmissionEnabledProperty())
        {
            bool hadEmissionTexture = emissionMapProp.textureValue != null;

            // Texture and HDR color controls
            m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionMapLabel, emissionMapProp, emissionColorProp, false);

            // If texture was assigned and color was black set color to white
            float brightness = emissionColorProp.colorValue.maxColorComponent;
            if (emissionMapProp.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                emissionColorProp.colorValue = Color.white;

            // LW does not support RealtimeEmissive. We set it to bake emissive and handle the emissive is black right.
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            if (brightness <= 0f)
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
    }

    private void ConvertFromLegacy(Material material, string oldShaderName)
    {
        UpgradeParams shaderUpgradeParams;

        if (oldShaderName.Contains("Transp"))
        {
            shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Transparent;
            shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
            shaderUpgradeParams.alphaClip = false;
            shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
        }
        else if (oldShaderName.Contains("Cutout"))
        {
            shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Opaque;
            shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
            shaderUpgradeParams.alphaClip = true;
            shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
        }
        else
        {
            shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Opaque;
            shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
            shaderUpgradeParams.alphaClip = false;
            shaderUpgradeParams.glosinessSource = GlossinessSource.BaseAlpha;
        }

        if (oldShaderName.Contains("Spec"))
            shaderUpgradeParams.specularSource = SpecularSource.SpecularTextureAndColor;
        else
            shaderUpgradeParams.specularSource = SpecularSource.NoSpecular;

        material.SetFloat("_Surface", (float)shaderUpgradeParams.surfaceType);
        material.SetFloat("_Blend", (float)shaderUpgradeParams.blendMode);
        material.SetFloat("_SpecSource", (float)shaderUpgradeParams.specularSource);
        material.SetFloat("_GlossinessSource", (float)shaderUpgradeParams.glosinessSource);

        if (oldShaderName.Contains("Self-Illumin"))
        {
            material.SetTexture("_EmissionMap", material.GetTexture("_MainTex"));
            material.SetTexture("_MainTex", null);
            material.SetColor("_EmissionColor", Color.white);
        }
    }
}
