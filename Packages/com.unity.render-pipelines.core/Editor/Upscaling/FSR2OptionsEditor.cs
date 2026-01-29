#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
using UnityEditor;
using UnityEngine;
using UnityEngine.AMD;

[CustomEditor(typeof(FSR2Options))]
public class FSR2OptionsEditor : Editor
{
    // Declare variables to hold each property
    private SerializedProperty m_QualityMode;
    private SerializedProperty m_FixedResolution;
    private SerializedProperty m_EnableSharpening;
    private SerializedProperty m_Sharpness;
    private void OnEnable()
    {
        // Find each property by its exact field name in FSR2Options.cs
        m_QualityMode = serializedObject.FindProperty("m_FSR2QualityMode");
        m_FixedResolution = serializedObject.FindProperty("m_FixedResolutionMode");
        m_EnableSharpening = serializedObject.FindProperty("m_EnableSharpening");
        m_Sharpness = serializedObject.FindProperty("m_Sharpness");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_QualityMode);
        EditorGUILayout.PropertyField(m_FixedResolution);
        EditorGUILayout.PropertyField(m_EnableSharpening);
        if(m_EnableSharpening.boolValue)
            EditorGUILayout.PropertyField(m_Sharpness);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
