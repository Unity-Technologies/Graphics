using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class LowendMobilePipelineMaterialEditor : ShaderGUI
{
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
    private MaterialProperty reflectionColorProp = null;

    private MaterialEditor m_MaterialEditor = null;

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Alpha
    }

    public enum SpecularSource
    {
        SpecularTextureAndColor,
        BaseTexture,
        NoSpecular
    }

    public enum GlossinessSource
    {
        BaseAlpha,
        SpecularAlpha
    }

    public enum ReflectionSource
    {
        NoReflection,
        Cubemap,
        ReflectionProbe
    }

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
        public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
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
        reflectionColorProp = FindProperty("_ReflectColor", properties);
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
        m_MaterialEditor.TexturePropertySingleLine(Styles.emissionMapLabel, emissionMapProp, emissionColorProp);

        EditorGUILayout.Space();
        DoReflection();

        if (EditorGUI.EndChangeCheck())
            UpdateMaterialKeywords(material);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        materialEditor.RenderQueueField();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if ((BlendMode)blendModeProp.floatValue == BlendMode.Cutout)
        {
            Styles.warningStyle.normal.textColor = Color.yellow;
            EditorGUILayout.LabelField(Styles.alphaCutoutWarning, Styles.warningStyle);
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        UpdateMaterialKeywords(material);
    }

    private void DoBlendMode()
    {
        int modeValue = (int)blendModeProp.floatValue;
        EditorGUI.BeginChangeCheck();
        modeValue = EditorGUILayout.Popup(Styles.renderingModeLabel, modeValue, Styles.blendNames);
        if (EditorGUI.EndChangeCheck())
            blendModeProp.floatValue = modeValue;

        BlendMode mode = (BlendMode)blendModeProp.floatValue;

        EditorGUILayout.Space();

        if (mode == BlendMode.Opaque)
        {
            int glossSource = (int)glossinessSourceProp.floatValue;
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoGlosinessLabels[glossSource], albedoMapProp, albedoColorProp);
            m_MaterialEditor.TextureScaleOffsetProperty(albedoMapProp);
        }
        else
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoAlphaLabel, albedoMapProp, albedoColorProp);
            m_MaterialEditor.TextureScaleOffsetProperty(albedoMapProp);
            if (mode == BlendMode.Cutout)
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
        {
            specularSourceProp.floatValue = source;
            if (source == (int)SpecularSource.BaseTexture)
                glossinessSourceProp.floatValue = (float)GlossinessSource.BaseAlpha;
        }

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
            m_MaterialEditor.TexturePropertySingleLine(Styles.specularGlossMapLabels[glossSource], specularGlossMapProp, specularColorProp);
        }

        if (specSource != SpecularSource.NoSpecular)
        {
            m_MaterialEditor.RangeProperty(shininessProp, Styles.shininessLabel);
        }
    }

    private void DoReflection()
    {
        EditorGUILayout.Space();

        int source = (int) reflectionSourceProp.floatValue;
        EditorGUI.BeginChangeCheck();
        source = EditorGUILayout.Popup(Styles.reflectionSourceLabel, source, Styles.speculaSourceNames);
        if (EditorGUI.EndChangeCheck())
            reflectionSourceProp.floatValue = (float) source;

        EditorGUILayout.Space();
        ReflectionSource reflectionSource = (ReflectionSource) reflectionSourceProp.floatValue;
        if (reflectionSource == ReflectionSource.Cubemap)
            m_MaterialEditor.TexturePropertySingleLine(Styles.reflectionMapLabel, reflectionMapProp, reflectionColorProp);
    }

    private void UpdateMaterialKeywords(Material material)
    {
        UpdateMaterialBlendMode(material);
        UpdateMaterialSpecularSource(material);
        UpdateMaterialReflectionSource(material);
        SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
        SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
        SetKeyword(material, "_CUBEMAP_REFLECTION", material.GetTexture("_Cube"));
        SetKeyword(material, "_EMISSION_MAP", material.GetTexture("_EmissionMap"));
    }

    private void UpdateMaterialBlendMode(Material material)
    {
        BlendMode mode = (BlendMode) material.GetFloat("_Mode");
        switch (mode)
        {
            case BlendMode.Opaque:
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                SetKeyword(material, "_ALPHATEST_ON", false);
                SetKeyword(material, "_ALPHABLEND_ON", false);
                material.renderQueue = -1;
                break;

            case BlendMode.Cutout:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                SetKeyword(material, "_ALPHATEST_ON", true);
                SetKeyword(material, "_ALPHABLEND_ON", false);
                material.renderQueue = (int)RenderQueue.AlphaTest;
                break;

            case BlendMode.Alpha:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                SetKeyword(material, "_ALPHATEST_ON", false);
                SetKeyword(material, "_ALPHABLEND_ON", true);
                material.renderQueue = (int) RenderQueue.Transparent;
                break;
        }
    }

    private void UpdateMaterialSpecularSource(Material material)
    {
        SpecularSource specSource = (SpecularSource) material.GetFloat("_SpecSource");
        if (specSource == SpecularSource.NoSpecular)
        {
            SetKeyword(material, "_SHARED_SPECULAR_DIFFUSE", false);
            SetKeyword(material, "_SPECULAR_MAP", false);
            SetKeyword(material, "_SPECULAR_COLOR", false);
        }
        else if (specSource == SpecularSource.BaseTexture)
        {
            SetKeyword(material, "_SHARED_SPECULAR_DIFFUSE", true);
            SetKeyword(material, "_SPECULAR_MAP", false);
            SetKeyword(material, "_SPECULAR_COLOR", false);
        }
        else if (specSource == SpecularSource.SpecularTextureAndColor && material.GetTexture("_SpecGlossMap"))
        {
            SetKeyword(material, "_SHARED_SPECULAR_DIFFUSE", false);
            SetKeyword(material, "_SPECULAR_MAP", true);
            SetKeyword(material, "_SPECULAR_COLOR", false);
        }
        else
        {
            SetKeyword(material, "_SHARED_SPECULAR_DIFFUSE", false);
            SetKeyword(material, "_SPECULAR_MAP", false);
            SetKeyword(material, "_SPECULAR_COLOR", true);
        }

        GlossinessSource glossSource = (GlossinessSource) material.GetFloat("_GlossinessSource");
        if (glossSource == GlossinessSource.BaseAlpha)
            SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", true);
        else
            SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
    }

    private void UpdateMaterialReflectionSource(Material material)
    {
        ReflectionSource reflectionSource = (ReflectionSource) material.GetFloat("_ReflectionSource");
        if (reflectionSource == ReflectionSource.NoReflection)
        {
            SetKeyword(material, "_CUBEMAP_REFLECTION", false);
        }
        else if (reflectionSource == ReflectionSource.Cubemap && material.GetTexture("_Cube"))
        {
            SetKeyword(material, "_CUBEMAP_REFLECTION", true);
        }
        else if (reflectionSource == ReflectionSource.ReflectionProbe)
        {
            Debug.LogWarning("Reflection probe not implemented yet");
            SetKeyword(material, "_CUBEMAP_REFLECTION", false);
        }
        else
        {
            SetKeyword(material, "_CUBEMAP_REFLECTION", false);
        }
    }

    private void SetKeyword(Material material, string keyword, bool enable)
    {
        if (enable)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }
}
