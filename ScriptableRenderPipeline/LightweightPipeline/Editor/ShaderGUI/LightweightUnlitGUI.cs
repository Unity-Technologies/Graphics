using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering.LightweightPipeline;

public class LightweightUnlitGUI : ShaderGUI
{
    private MaterialProperty blendModeProp = null;
    private MaterialProperty mainTexProp = null;
    private MaterialProperty mainColorProp = null;
    private MaterialProperty alphaCutoffProp = null;

    private MaterialEditor m_MaterialEditor = null;

    private static class Styles
    {
        public static GUIContent[] mainTexLabels =
        {
            new GUIContent("MainTex (RGB)", "Base Color"),
            new GUIContent("MainTex (RGB) Alpha (A)", "Base Color and Alpha")
        };

        public static readonly string[] blendNames = Enum.GetNames(typeof(UpgradeBlendMode));

        public static string renderingModeLabel = "Rendering Mode";
        public static string alphaCutoffLabel = "Alpha Cutoff";
    }

    private void FindMaterialProperties(MaterialProperty[] properties)
    {
        blendModeProp = FindProperty("_Mode", properties);
        mainTexProp = FindProperty("_MainTex", properties);
        mainColorProp = FindProperty("_MainColor", properties);
        alphaCutoffProp = FindProperty("_Cutoff", properties);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;
        m_MaterialEditor = materialEditor;

        FindMaterialProperties(properties);

        int modeValue = (int)blendModeProp.floatValue;
        EditorGUI.BeginChangeCheck();
        modeValue = EditorGUILayout.Popup(Styles.renderingModeLabel, modeValue, Styles.blendNames);
        if (EditorGUI.EndChangeCheck())
            blendModeProp.floatValue = modeValue;

        GUIContent mainTexLabel = Styles.mainTexLabels[Math.Min(modeValue, 1)];
        m_MaterialEditor.TexturePropertySingleLine(mainTexLabel, mainTexProp, mainColorProp);
        m_MaterialEditor.TextureScaleOffsetProperty(mainTexProp);

        if ((UpgradeBlendMode) modeValue == UpgradeBlendMode.Cutout)
            m_MaterialEditor.RangeProperty(alphaCutoffProp, Styles.alphaCutoffLabel);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        materialEditor.RenderQueueField();

        LightweightShaderHelper.SetMaterialBlendMode(material);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }
}
