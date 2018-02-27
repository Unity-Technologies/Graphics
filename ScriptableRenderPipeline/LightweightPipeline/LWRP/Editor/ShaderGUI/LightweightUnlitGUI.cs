using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Experimental.Rendering;

public class LightweightUnlitGUI : LightweightShaderGUI
{
    public enum UnlitBlendMode
    {
        Alpha,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Additive,
        Multiply
    }

    private MaterialProperty surfaceTypeProp;
    private MaterialProperty blendModeProp;
    private MaterialProperty culling;
    private MaterialProperty alphaClip;
    private MaterialProperty mainTexProp;
    private MaterialProperty mainColorProp;
    private MaterialProperty alphaCutoffProp;
    private MaterialProperty sampleGIProp;
    private MaterialProperty bumpMap;

    private static class Styles
    {
        public static GUIContent twoSidedLabel = new GUIContent("Two Sided", "Render front and back faces");
        public static GUIContent alphaClipLabel = new GUIContent("Alpha Clip", "Enable Alpha Clip");

        public static GUIContent[] mainTexLabels =
        {
            new GUIContent("MainTex (RGB)", "Base Color"),
            new GUIContent("MainTex (RGB) Alpha (A)", "Base Color and Alpha")
        };

        public static GUIContent normalMapLabel = new GUIContent("Normal Map", "Normal Map");
        public static readonly string[] surfaceNames = Enum.GetNames(typeof(SurfaceType));
        public static readonly string[] blendNames = Enum.GetNames(typeof(UnlitBlendMode));

        public static string surfaceTypeLabel = "Surface Type";
        public static string blendingModeLabel = "Blending Mode";
        public static string clipThresholdLabel = "Clip Threshold";
        public static GUIContent sampleGILabel = new GUIContent("Sample GI", "If enabled GI will be sampled from SH or Lightmap.");
    }

    public override void FindProperties(MaterialProperty[] properties)
    {
        surfaceTypeProp = FindProperty("_Surface", properties);
        blendModeProp = FindProperty("_Blend", properties);
        culling = FindProperty("_Cull", properties);
        alphaClip  = FindProperty("_AlphaClip", properties);
        mainTexProp = FindProperty("_MainTex", properties);
        mainColorProp = FindProperty("_Color", properties);
        alphaCutoffProp = FindProperty("_Cutoff", properties);
        sampleGIProp = FindProperty("_SampleGI", properties, false);
        bumpMap = FindProperty("_BumpMap", properties, false);
    }

    public override void ShaderPropertiesGUI(Material material)
    {
        EditorGUI.BeginChangeCheck();
        {
            DoPopup(Styles.surfaceTypeLabel, surfaceTypeProp, Styles.surfaceNames);
            int surfaceTypeValue = (int)surfaceTypeProp.floatValue;

            if((SurfaceType)surfaceTypeValue == SurfaceType.Transparent)
                DoPopup(Styles.blendingModeLabel, blendModeProp, Styles.blendNames);

            EditorGUI.BeginChangeCheck();
            bool twoSidedEnabled = EditorGUILayout.Toggle(Styles.twoSidedLabel, culling.floatValue == 0);
            if (EditorGUI.EndChangeCheck())
                culling.floatValue = twoSidedEnabled ? 0 : 2;

            EditorGUI.BeginChangeCheck();
            bool alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipLabel, alphaClip.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                alphaClip.floatValue = alphaClipEnabled ? 1 : 0;

            GUIContent mainTexLabel = Styles.mainTexLabels[Math.Min(surfaceTypeValue, 1)];
            m_MaterialEditor.TexturePropertySingleLine(mainTexLabel, mainTexProp, mainColorProp);

            if (alphaClipEnabled)
                m_MaterialEditor.ShaderProperty(alphaCutoffProp, Styles.clipThresholdLabel, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

            m_MaterialEditor.TextureScaleOffsetProperty(mainTexProp);

            EditorGUILayout.Space();
            m_MaterialEditor.ShaderProperty(sampleGIProp, Styles.sampleGILabel);
            if (sampleGIProp.floatValue >= 1.0)
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapLabel, bumpMap);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var target in blendModeProp.targets)
                MaterialChanged((Material)target);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    public override void MaterialChanged(Material material)
    {
        material.shaderKeywords = null;
        bool sampleGI = material.GetFloat("_SampleGI") >= 1.0f;
        CoreUtils.SetKeyword(material, "_SAMPLE_GI", sampleGI);
        CoreUtils.SetKeyword(material, "_NORMAL_MAP", sampleGI && material.GetTexture("_BumpMap"));

        bool alphaClip = material.GetFloat("_AlphaClip") == 1;
        if(alphaClip)
            material.EnableKeyword("_ALPHATEST_ON");
        else
            material.DisableKeyword("_ALPHATEST_ON");

        SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
        if(surfaceType == SurfaceType.Opaque)
        {
            material.SetOverrideTag("RenderType", "");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            material.SetShaderPassEnabled("ShadowCaster", true);
        }
        else
        { 
            UnlitBlendMode blendMode = (UnlitBlendMode)material.GetFloat("_Blend");
            switch (blendMode)
            {
                case UnlitBlendMode.Alpha:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    break;
                case UnlitBlendMode.Additive:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    break;
                case UnlitBlendMode.Multiply:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    break;
            }
        }
    }
}
