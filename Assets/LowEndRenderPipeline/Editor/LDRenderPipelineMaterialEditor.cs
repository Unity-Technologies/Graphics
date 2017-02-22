using System;
using UnityEditor;
using UnityEngine;

// TODO: Implement AssignNewShaderToMaterial to handle cases from update to different mobile shaders having different props
public class LDRenderPipelineMaterialEditor : MaterialEditor
{
    private MaterialProperty blendMode = null;
    private MaterialProperty albedoMap = null;
    private MaterialProperty albedoColor = null;
    private MaterialProperty alphaCutoff = null;
    private MaterialProperty specularGlossMap = null;
    private MaterialProperty specularColor = null;
    private MaterialProperty shininess = null;
    private MaterialProperty bumpMap = null;
    private MaterialProperty emissionColor = null;

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Alpha
    }

    private void Awake()
    {
        Material material = target as Material;
        FindMaterialProperties(material);
        UpdateMaterialKeywords(material);
    }

    private static class Styles
    {
        public static GUIContent albedoGlosinessLabel = new GUIContent("Base (RGB)", "Base Color");
        public static GUIContent albedoAlphaLabel = new GUIContent("Base (RGB) Alpha (A)", "Base Color (RGB) and Transparency (A)");
        public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
        public static GUIContent specularGlossMapLabel = new GUIContent("Specular Color (RGB) Glossiness (A)", "Specular Color (RGB) Glossiness (a)");
        public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
        public static GUIContent emissionText = new GUIContent("Color", "Emission (RGB)");
        public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));

        public static string renderingModeLabel = "RenderingMode";
        public static string albedoColorLabel = "Base Color";
        public static string albedoMapAlphaLabel = "Base(RGB) Alpha(A)";
        public static string albedoMapGlossinessLabel = "Base(RGB) Glossiness (A)";
        public static string alphaCutoffLabel = "Cutoff";
        public static string specularColorLabel = "Specular Color (RGB) Glossiness (A)";
        public static string shininessLabel = "Shininess";
        public static string normalMapLabel = "Normal map";
        public static string emissionColorLabel = "Emission Color";
    }

    private void FindMaterialProperties(Material material)
    {
        Material[] mats = { material };
        blendMode = GetMaterialProperty(mats, "_Mode");
        albedoMap = GetMaterialProperty(mats, "_MainTex");
        albedoColor = GetMaterialProperty(mats, "_Color");
        
        alphaCutoff = GetMaterialProperty(mats, "_Cutoff");
        specularGlossMap = GetMaterialProperty(mats, "_SpecGlossMap");
        specularColor = GetMaterialProperty(mats, "_SpecColor");
        shininess = GetMaterialProperty(mats, "_Glossiness");
        bumpMap = GetMaterialProperty(mats, "_BumpMap");
        emissionColor = GetMaterialProperty(mats, "_EmissionColor");
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
            
            BlendMode mode = (BlendMode)blendMode.floatValue;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            switch (mode)
            {
                case BlendMode.Opaque:
                    TexturePropertySingleLine(Styles.albedoGlosinessLabel, albedoMap, albedoColor);
                    TextureScaleOffsetProperty(albedoMap);
                    break;

                case BlendMode.Cutout:
                    TexturePropertySingleLine(Styles.albedoGlosinessLabel, albedoMap, albedoColor);
                    TextureScaleOffsetProperty(albedoMap);
                    RangeProperty(alphaCutoff, "Cutoff");
                    break;

                case BlendMode.Alpha:
                    TexturePropertySingleLine(Styles.albedoAlphaLabel, albedoMap, albedoColor);
                    TextureScaleOffsetProperty(albedoMap);
                    break;
            }

            EditorGUILayout.Space();
            TexturePropertySingleLine(Styles.specularGlossMapLabel, specularGlossMap, specularColor);
            RangeProperty(shininess, Styles.shininessLabel);

            EditorGUILayout.Space();
            TexturePropertySingleLine(Styles.normalMapText, bumpMap);

            EditorGUILayout.Space();
            ColorProperty(emissionColor, Styles.emissionColorLabel);

            if (EditorGUI.EndChangeCheck())
                UpdateMaterialKeywords(material);
        }
    }

    private void DoBlendMode()
    {
        int mode = (int)blendMode.floatValue;
        EditorGUI.BeginChangeCheck();
        mode = EditorGUILayout.Popup(Styles.renderingModeLabel, mode, Styles.blendNames);
        if (EditorGUI.EndChangeCheck())
            blendMode.floatValue = mode;
    }

    private void UpdateMaterialKeywords(Material material)
    {
        UpdateMaterialBlendMode(material);
        SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
        SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
    }

    private void UpdateMaterialBlendMode(Material material)
    {
        BlendMode mode = (BlendMode)blendMode.floatValue;
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

    private void SetKeyword(Material material, string keyword, bool enable)
    {
        if (enable)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }
}
