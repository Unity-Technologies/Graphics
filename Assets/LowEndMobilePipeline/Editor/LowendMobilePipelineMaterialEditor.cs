using System;
using UnityEditor;
using UnityEngine;

public class LowendMobilePipelineMaterialEditor : MaterialEditor
{
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

    private void Awake()
    {
        Material material = target as Material;
        FindMaterialProperties(material);
        UpdateMaterialKeywords(material);

        Styles.warningStyle.normal.textColor = Color.yellow;
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

        public static GUIContent alphaCutoutWarning =
            new GUIContent(
                "This material has alpha cutout enabled. Alpha cutout has severe performance impact on mobile!");

        public static GUIStyle warningStyle = new GUIStyle();
        public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
        public static readonly string[] specSourceNames = Enum.GetNames(typeof(SpecularSource));
        public static readonly string[] glossinessSourceNames = Enum.GetNames(typeof(GlossinessSource));

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
    }

    private void FindMaterialProperties(Material material)
    {
        Material[] mats = { material };
        blendModeProp = GetMaterialProperty(mats, "_Mode");
        albedoMapProp = GetMaterialProperty(mats, "_MainTex");
        albedoColorProp = GetMaterialProperty(mats, "_Color");

        alphaCutoffProp = GetMaterialProperty(mats, "_Cutoff");
        specularSourceProp = GetMaterialProperty(mats, "_SpecSource");
        glossinessSourceProp = GetMaterialProperty(mats, "_GlossinessSource");
        specularGlossMapProp = GetMaterialProperty(mats, "_SpecGlossMap");
        specularColorProp = GetMaterialProperty(mats, "_SpecColor");
        shininessProp = GetMaterialProperty(mats, "_Shininess");
        bumpMapProp = GetMaterialProperty(mats, "_BumpMap");
        emissionMapProp = GetMaterialProperty(mats, "_EmissionMap");
        emissionColorProp = GetMaterialProperty(mats, "_EmissionColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var shaderObj = serializedObject.FindProperty("m_Shader");
        if (isVisible && shaderObj.objectReferenceValue != null)
        {
            Material material = target as Material;
            FindMaterialProperties(material);

            EditorGUI.BeginChangeCheck();
            DoBlendMode();

            EditorGUILayout.Space();
            DoSpecular();

            EditorGUILayout.Space();
            TexturePropertySingleLine(Styles.normalMapText, bumpMapProp);

            EditorGUILayout.Space();
            TexturePropertySingleLine(Styles.emissionMapLabel, emissionMapProp, emissionColorProp);

            if (EditorGUI.EndChangeCheck())
                UpdateMaterialKeywords(material);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if ((BlendMode)blendModeProp.floatValue == BlendMode.Cutout)
            {
                Styles.warningStyle.normal.textColor = Color.yellow;
                EditorGUILayout.LabelField(Styles.alphaCutoutWarning, Styles.warningStyle);
            }
        }
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
            TexturePropertySingleLine(Styles.albedoGlosinessLabels[glossSource], albedoMapProp, albedoColorProp);
            TextureScaleOffsetProperty(albedoMapProp);
        }
        else
        {
            TexturePropertySingleLine(Styles.albedoAlphaLabel, albedoMapProp, albedoColorProp);
            TextureScaleOffsetProperty(albedoMapProp);
            if (mode == BlendMode.Cutout)
                RangeProperty(alphaCutoffProp, "Cutoff");
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
            TexturePropertySingleLine(Styles.specularGlossMapLabels[glossSource], specularGlossMapProp, specularColorProp);
        }

        if (specSource != SpecularSource.NoSpecular)
        {
            RangeProperty(shininessProp, Styles.shininessLabel);
        }
    }

    private void UpdateMaterialKeywords(Material material)
    {
        UpdateMaterialBlendMode(material);
        UpdateMaterialSpecularSource(material);
        SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
        SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
    }

    private void UpdateMaterialBlendMode(Material material)
    {
        BlendMode mode = (BlendMode)blendModeProp.floatValue;
        switch (mode)
        {
            case BlendMode.Opaque:
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                SetKeyword(material, "_ALPHATEST_ON", false);
                SetKeyword(material, "_ALPHABLEND_ON", false);
                break;

            case BlendMode.Cutout:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                SetKeyword(material, "_ALPHATEST_ON", true);
                SetKeyword(material, "_ALPHABLEND_ON", false);
                break;

            case BlendMode.Alpha:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                SetKeyword(material, "_ALPHATEST_ON", false);
                SetKeyword(material, "_ALPHABLEND_ON", true);
                break;
        }
    }

    private void UpdateMaterialSpecularSource(Material material)
    {
        SpecularSource specSource = (SpecularSource)specularSourceProp.floatValue;
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

        GlossinessSource glossSource = (GlossinessSource)glossinessSourceProp.floatValue;
        if (glossSource == GlossinessSource.BaseAlpha)
            SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", true);
        else
            SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
    }

    private void SetKeyword(Material material, string keyword, bool enable)
    {
        if (enable)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }
}
