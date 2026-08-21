#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
using UnityEditor;
using UnityEngine;
using UnityEngine.AMD;

[CustomEditor(typeof(FSR4Options))]
public class FSR4OptionsEditor : UpscalerOptionsEditor
{
    protected override bool showsResolutionMode => true;

    // Declare variables to hold each property
    private SerializedProperty m_QualityMode;
    private SerializedProperty m_EnableSharpening;
    private SerializedProperty m_Sharpness;

    protected override void OnEnable()
    {
        base.OnEnable();
        // Find each property by its exact field name in FSR4Options.cs
        m_QualityMode = serializedObject.FindProperty("m_FSR4QualityMode");
        m_EnableSharpening = serializedObject.FindProperty("m_EnableSharpening");
        m_Sharpness = serializedObject.FindProperty("m_Sharpness");
    }

    protected override void DrawOptions()
    {
        EditorGUILayout.PropertyField(m_QualityMode);
        EditorGUILayout.PropertyField(m_EnableSharpening);
        if(m_EnableSharpening.boolValue)
            EditorGUILayout.PropertyField(m_Sharpness);
    }
}
#endif
