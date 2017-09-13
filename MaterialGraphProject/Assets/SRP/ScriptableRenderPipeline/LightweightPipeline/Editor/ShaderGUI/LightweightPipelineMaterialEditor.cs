using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering.LightweightPipeline;

public class LightweightPipelineMaterialEditor : ShaderGUI
{
    private const float kMinShininessValue = 0.01f;
    private MaterialProperty blendModeProp = null;
    private MaterialProperty albedoMapProp = null;
    private MaterialProperty albedoColorProp = null;
    private MaterialProperty alphaCutoffProp = null;
    private MaterialProperty specularSourceProp = null;
    private MaterialProperty glossinessSourceProp = null;
    private MaterialProperty reflectionSourceProp = null;
    private MaterialProperty specularGlossMapProp = null;
    private MaterialProperty specularColorProp = null;
    private MaterialProperty shininessProp = null;
    private MaterialProperty bumpMapProp = null;
    private MaterialProperty emissionMapProp = null;
    private MaterialProperty emissionColorProp = null;
    private MaterialProperty reflectionMapProp = null;

    private MaterialEditor m_MaterialEditor = null;

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
            new GUIContent("Specular Color (RGB)", "Specular Color (RGB)"),
            new GUIContent("Specular Color (RGB) Glossiness (A)", "Specular Color (RGB) Glossiness (A)")
        };

        public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
        public static GUIContent emissionMapLabel = new GUIContent("Emission Map", "Emission Map");
        public static GUIContent reflectionMapLabel = new GUIContent("Reflection Source", "Reflection Source Map");

        public static GUIContent alphaCutoutWarning =
            new GUIContent(
                "This material has alpha cutout enabled. Alpha cutout has severe performance impact on mobile!");

        public static GUIStyle warningStyle = new GUIStyle();
        public static readonly string[] blendNames = Enum.GetNames(typeof(UpgradeBlendMode));
        public static readonly string[] specSourceNames = Enum.GetNames(typeof(SpecularSource));
        public static readonly string[] glossinessSourceNames = Enum.GetNames(typeof(GlossinessSource));
        public static readonly string[] speculaSourceNames = Enum.GetNames(typeof(ReflectionSource));

        public static string renderingModeLabel = "Rendering Mode";
        public static string specularSourceLabel = "Specular Color Source";
        public static string glossinessSourceLable = "Glossiness Source";
        public static string glossinessSource = "Glossiness Source";
        public static string albedoColorLabel = "Base Color";
        public static string albedoMapAlphaLabel = "Base(RGB) Alpha(A)";
        public static string albedoMapGlossinessLabel = "Base(RGB) Glossiness (A)";
        public static string alphaCutoffLabel = "Alpha Cutoff";
        public static string shininessLabel = "Shininess";
        public static string normalMapLabel = "Normal map";
        public static string emissionColorLabel = "Emission Color";
        public static string reflectionSourceLabel = "Reflection Source";
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
        reflectionMapProp = FindProperty("_Cube", properties);
        reflectionSourceProp = FindProperty("_ReflectionSource", properties);
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
        DoEmission();

        EditorGUILayout.Space();
        DoReflection();

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

        if (shaderStrings[0].Equals("Legacy Shaders") || shaderStrings[0].Equals("Mobile") ||
            shaderStrings[0].Equals("Reflective"))
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

        int source = (int)specularSourceProp.floatValue;
        EditorGUI.BeginChangeCheck();
        source = EditorGUILayout.Popup(Styles.specularSourceLabel, source, Styles.specSourceNames);
        if (EditorGUI.EndChangeCheck())
            specularSourceProp.floatValue = source;

        SpecularSource specSource = (SpecularSource)specularSourceProp.floatValue;
        if (specSource != SpecularSource.NoSpecular)
        {
            int glossinessSource = (int)glossinessSourceProp.floatValue;
            EditorGUI.BeginChangeCheck();
            glossinessSource = EditorGUILayout.Popup(Styles.glossinessSourceLable, glossinessSource,
                    Styles.glossinessSourceNames);
            if (EditorGUI.EndChangeCheck())
                glossinessSourceProp.floatValue = (float)glossinessSource;
        }

        int glossSource = (int)glossinessSourceProp.floatValue;
        if (specSource == SpecularSource.SpecularTextureAndColor)
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.specularGlossMapLabels[glossSource],
                specularGlossMapProp, specularColorProp);
        }

        if (specSource != SpecularSource.NoSpecular)
        {
            EditorGUI.BeginChangeCheck();
            float shininess = EditorGUILayout.Slider(Styles.shininessLabel, shininessProp.floatValue,
                    kMinShininessValue, 1.0f);
            if (EditorGUI.EndChangeCheck())
                shininessProp.floatValue = shininess;
        }
    }

    private void DoEmission()
    {
        if (m_MaterialEditor.EmissionEnabledProperty())
        {
            bool hadEmissionMap = emissionMapProp.textureValue != null;
            m_MaterialEditor.TexturePropertySingleLine(Styles.emissionMapLabel, emissionMapProp, emissionColorProp);

            float maxValue = emissionColorProp.colorValue.maxColorComponent;
            if (emissionMapProp.textureValue != null && !hadEmissionMap && maxValue <= 0.0f)
                emissionColorProp.colorValue = Color.white;

            m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
        }
    }

    private void DoReflection()
    {
        EditorGUILayout.Space();

        int source = (int)reflectionSourceProp.floatValue;
        EditorGUI.BeginChangeCheck();
        source = EditorGUILayout.Popup(Styles.reflectionSourceLabel, source, Styles.speculaSourceNames);
        if (EditorGUI.EndChangeCheck())
            reflectionSourceProp.floatValue = (float)source;

        EditorGUILayout.Space();
        ReflectionSource reflectionSource = (ReflectionSource)reflectionSourceProp.floatValue;
        if (reflectionSource == ReflectionSource.Cubemap)
            m_MaterialEditor.TexturePropertySingleLine(Styles.reflectionMapLabel, reflectionMapProp);
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

        if (oldShaderName.Contains("Reflective"))
            shaderUpgradeParams.reflectionSource = ReflectionSource.Cubemap;
        else
            shaderUpgradeParams.reflectionSource = ReflectionSource.NoReflection;

        material.SetFloat("_Mode", (float)shaderUpgradeParams.blendMode);
        material.SetFloat("_SpecSource", (float)shaderUpgradeParams.specularSource);
        material.SetFloat("_GlossinessSource", (float)shaderUpgradeParams.glosinessSource);
        material.SetFloat("_ReflectionSource", (float)shaderUpgradeParams.reflectionSource);

        if (oldShaderName.Contains("Self-Illumin"))
        {
            material.SetTexture("_EmissionMap", material.GetTexture("_MainTex"));
            material.SetTexture("_MainTex", null);
            material.SetColor("_EmissionColor", Color.white);
        }
    }
}
