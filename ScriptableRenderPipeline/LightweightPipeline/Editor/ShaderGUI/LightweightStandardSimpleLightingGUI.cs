using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.Rendering.LightweightPipeline;

public class LightweightStandardSimpleLightingGUI : ShaderGUI
{
    private const float kMinShininessValue = 0.01f;
    private MaterialProperty blendModeProp = null;
    private MaterialProperty albedoMapProp = null;
    private MaterialProperty albedoColorProp = null;
    private MaterialProperty alphaCutoffProp = null;
    private MaterialProperty specularSourceProp = null;
    private MaterialProperty glossinessSourceProp = null;
    private MaterialProperty specularGlossMapProp = null;
    private MaterialProperty specularColorProp = null;
    private MaterialProperty shininessProp = null;
    private MaterialProperty bumpMapProp = null;
    private MaterialProperty emissionMapProp = null;
    private MaterialProperty emissionColorProp = null;

    private MaterialEditor m_MaterialEditor = null;
    private const float kMaxfp16 = 65536f; // Clamp to a value that fits into fp16.
    private ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, kMaxfp16, 1 / kMaxfp16, 3f);

    private static class Styles
    {
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

        public static GUIContent alphaCutoutWarning =
            new GUIContent(
                "This material has alpha cutout enabled. Alpha cutout has severe performance impact on mobile!");

        public static GUIStyle warningStyle = new GUIStyle();
        public static readonly string[] blendNames = Enum.GetNames(typeof(UpgradeBlendMode));
        public static readonly string[] glossinessSourceNames = Enum.GetNames(typeof(GlossinessSource));

        public static string renderingModeLabel = "Rendering Mode";
        public static string specularSourceLabel = "Specular";
        public static string glossinessSourceLabel = "Glossiness Source";
        public static string glossinessSource = "Glossiness Source";
        public static string albedoColorLabel = "Base Color";
        public static string albedoMapAlphaLabel = "Base(RGB) Alpha(A)";
        public static string albedoMapGlossinessLabel = "Base(RGB) Glossiness (A)";
        public static string alphaCutoffLabel = "Alpha Cutoff";
        public static string shininessLabel = "Shininess";
        public static string normalMapLabel = "Normal map";
        public static string emissionColorLabel = "Emission Color";
    }

    private void FindMaterialProperties(MaterialProperty[] properties)
    {
        blendModeProp = FindProperty("_Mode", properties);
        albedoMapProp = FindProperty("_MainTex", properties);
        albedoColorProp = FindProperty("_Color", properties);

        alphaCutoffProp = FindProperty("_Cutoff", properties);
        specularSourceProp = FindProperty("_SpecSource", properties);
        glossinessSourceProp = FindProperty("_GlossinessSource", properties);
        specularGlossMapProp = FindProperty("_SpecGlossMap", properties);
        specularColorProp = FindProperty("_SpecColor", properties);
        shininessProp = FindProperty("_Shininess", properties);
        bumpMapProp = FindProperty("_BumpMap", properties);
        emissionMapProp = FindProperty("_EmissionMap", properties);
        emissionColorProp = FindProperty("_EmissionColor", properties);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;
        m_MaterialEditor = materialEditor;

        FindMaterialProperties(properties);

        EditorGUI.BeginChangeCheck();
        DoBlendMode();

        EditorGUILayout.Space();
        DoSpecular();

        EditorGUILayout.Space();
        m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMapProp);

        EditorGUILayout.Space();
        DoEmissionArea(material);

        if (EditorGUI.EndChangeCheck())
            LegacyBlinnPhongUpgrader.UpdateMaterialKeywords(material);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        materialEditor.RenderQueueField();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if ((UpgradeBlendMode)blendModeProp.floatValue == UpgradeBlendMode.Cutout)
        {
            Styles.warningStyle.normal.textColor = Color.yellow;
            EditorGUILayout.LabelField(Styles.alphaCutoutWarning, Styles.warningStyle);
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

        LegacyBlinnPhongUpgrader.UpdateMaterialKeywords(material);
    }

    private void DoBlendMode()
    {
        int modeValue = (int)blendModeProp.floatValue;
        EditorGUI.BeginChangeCheck();
        modeValue = EditorGUILayout.Popup(Styles.renderingModeLabel, modeValue, Styles.blendNames);
        if (EditorGUI.EndChangeCheck())
            blendModeProp.floatValue = modeValue;

        UpgradeBlendMode mode = (UpgradeBlendMode)blendModeProp.floatValue;

        EditorGUILayout.Space();

        if (mode == UpgradeBlendMode.Opaque)
        {
            int glossSource = (int)glossinessSourceProp.floatValue;
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoGlosinessLabels[glossSource], albedoMapProp,
                albedoColorProp);
            m_MaterialEditor.TextureScaleOffsetProperty(albedoMapProp);
        }
        else
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoAlphaLabel, albedoMapProp, albedoColorProp);
            m_MaterialEditor.TextureScaleOffsetProperty(albedoMapProp);
            if (mode == UpgradeBlendMode.Cutout)
                m_MaterialEditor.RangeProperty(alphaCutoffProp, "Cutoff");
        }
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
            m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionMapLabel, emissionMapProp, emissionColorProp, m_ColorPickerHDRConfig, false);

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
            shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
            shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
        }
        else if (oldShaderName.Contains("Cutout"))
        {
            shaderUpgradeParams.blendMode = UpgradeBlendMode.Cutout;
            shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
        }
        else
        {
            shaderUpgradeParams.blendMode = UpgradeBlendMode.Opaque;
            shaderUpgradeParams.glosinessSource = GlossinessSource.BaseAlpha;
        }

        if (oldShaderName.Contains("Spec"))
            shaderUpgradeParams.specularSource = SpecularSource.SpecularTextureAndColor;
        else
            shaderUpgradeParams.specularSource = SpecularSource.NoSpecular;

        material.SetFloat("_Mode", (float)shaderUpgradeParams.blendMode);
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
