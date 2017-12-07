using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.Rendering.LightweightPipeline;

public class LightweightUnlitGUI : ShaderGUI
{
    private MaterialProperty blendModeProp;
    private MaterialProperty mainTexProp;
    private MaterialProperty mainColorProp;
    private MaterialProperty alphaCutoffProp;
    private MaterialProperty sampleGIProp;
    private MaterialProperty bumpMap;

    private MaterialEditor m_MaterialEditor;

    private static class Styles
    {
        public static GUIContent[] mainTexLabels =
        {
            new GUIContent("MainTex (RGB)", "Base Color"),
            new GUIContent("MainTex (RGB) Alpha (A)", "Base Color and Alpha")
        };

        public static GUIContent normalMapLabel = new GUIContent("Normal Map", "Normal Map");
        public static readonly string[] blendNames = Enum.GetNames(typeof(UpgradeBlendMode));

        public static string renderingModeLabel = "Rendering Mode";
        public static string alphaCutoffLabel = "Alpha Cutoff";
        public static GUIContent sampleGILabel = new GUIContent("Sample GI", "If enabled GI will be sampled from SH or Lightmap.");
    }

    private void FindMaterialProperties(MaterialProperty[] properties)
    {
        blendModeProp = FindProperty("_Mode", properties);
        mainTexProp = FindProperty("_MainTex", properties);
        mainColorProp = FindProperty("_Color", properties);
        alphaCutoffProp = FindProperty("_Cutoff", properties);
        sampleGIProp = FindProperty("_SampleGI", properties, false);
        bumpMap = FindProperty("_BumpMap", properties, false);
    }

    private void DoPopup(string label, MaterialProperty property, string[] options)
    {
        EditorGUI.showMixedValue = property.hasMixedValue;

        var mode = property.floatValue;
        EditorGUI.BeginChangeCheck();
        mode = EditorGUILayout.Popup(label, (int)mode, options);
        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo(label);
            property.floatValue = mode;
        }

        EditorGUI.showMixedValue = false;
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        m_MaterialEditor = materialEditor;

        FindMaterialProperties(properties);

        EditorGUI.BeginChangeCheck();
        {
            DoPopup(Styles.renderingModeLabel, blendModeProp, Styles.blendNames);
            int modeValue = (int) blendModeProp.floatValue;

            GUIContent mainTexLabel = Styles.mainTexLabels[Math.Min(modeValue, 1)];
            m_MaterialEditor.TexturePropertySingleLine(mainTexLabel, mainTexProp, mainColorProp);
            m_MaterialEditor.TextureScaleOffsetProperty(mainTexProp);

            if ((UpgradeBlendMode) modeValue == UpgradeBlendMode.Cutout)
                m_MaterialEditor.RangeProperty(alphaCutoffProp, Styles.alphaCutoffLabel);

            EditorGUILayout.Space();
            m_MaterialEditor.ShaderProperty(sampleGIProp, Styles.sampleGILabel);
            if (sampleGIProp.floatValue >= 1.0)
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapLabel, bumpMap);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            materialEditor.RenderQueueField();
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var target in blendModeProp.targets)
                MaterialChanged((Material)target);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    private void MaterialChanged(Material material)
    {
        material.shaderKeywords = null;
        bool sampleGI = material.GetFloat("_SampleGI") >= 1.0f;
        LightweightShaderHelper.SetMaterialBlendMode(material);
        LightweightShaderHelper.SetKeyword(material, "_SAMPLE_GI", sampleGI);
        LightweightShaderHelper.SetKeyword(material, "_NORMAL_MAP", sampleGI && material.GetTexture("_BumpMap"));
    }
}
