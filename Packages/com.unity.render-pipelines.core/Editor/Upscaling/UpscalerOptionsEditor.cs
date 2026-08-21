#if ENABLE_UPSCALER_FRAMEWORK
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

/// <summary>
/// Base inspector for <see cref="UnityEngine.Rendering.UpscalerOptions"/> and its subclasses. It draws the
/// framework-common fields (the Resolution Mode dropdown, for quality-mode upscalers) so upscaler authors only
/// implement their upscaler-specific options in <see cref="DrawOptions"/> and never have to wire up the shared fields.
/// Also hides the default "Script" field.
/// </summary>
/// <remarks>
/// Used directly for options without a dedicated editor (the Resolution Mode field stays hidden because
/// <see cref="showsResolutionMode"/> defaults to false — it is dormant for upscalers with no quality mode). A
/// quality-mode upscaler provides a derived editor with <c>[CustomEditor(typeof(MyOptions))]</c> that overrides
/// <see cref="showsResolutionMode"/> (true) and <see cref="DrawOptions"/> for its own fields. Do not call
/// <c>serializedObject.Update()/ApplyModifiedProperties()</c> in <see cref="DrawOptions"/>.
/// </remarks>
[CustomEditor(typeof(UnityEngine.Rendering.UpscalerOptions), true)]
public class UpscalerOptionsEditor : Editor
{
    SerializedProperty m_ResolutionMode;

    SerializedProperty m_EnableReactiveMaskPass;
    SerializedProperty m_ReactiveMaskUseComponentMax;
    SerializedProperty m_ReactiveMaskScale;
    SerializedProperty m_ApplyReactiveMaskThreshold;
    SerializedProperty m_ReactiveMaskThreshold;
    SerializedProperty m_ReactiveMaskBinaryValue;

    static class Styles
    {
        internal static readonly GUIContent EnableReactiveMaskLabel = new GUIContent("Reactive Mask");
        internal static readonly GUIContent ReactiveMaskUseComponentMaxLabel = new GUIContent("Use Component Max");
        internal static readonly GUIContent ReactiveMaskScaleLabel = new GUIContent("Scale");
        internal static readonly GUIContent ReactiveMaskApplyThresholdLabel = new GUIContent("Apply Threshold");
        internal static readonly GUIContent ReactiveMaskThresholdLabel = new GUIContent("Threshold");
        internal static readonly GUIContent ReactiveMaskBinaryValueLabel = new GUIContent("Binary Value");
    }

    /// <summary>Override to true for quality-mode upscalers so the Resolution Mode dropdown is shown.</summary>
    protected virtual bool showsResolutionMode => false;

    protected virtual bool showsReactiveMaskGeneration => false;

    protected virtual void OnEnable()
    {
        m_ResolutionMode = serializedObject.FindProperty("m_ResolutionMode");

        m_EnableReactiveMaskPass = serializedObject.FindProperty("m_EnableReactiveMaskPass");
        m_ReactiveMaskUseComponentMax = serializedObject.FindProperty("m_ReactiveMaskUseComponentMax");
        m_ReactiveMaskScale = serializedObject.FindProperty("m_ReactiveMaskScale");
        m_ApplyReactiveMaskThreshold = serializedObject.FindProperty("m_ApplyReactiveMaskThreshold");
        m_ReactiveMaskThreshold = serializedObject.FindProperty("m_ReactiveMaskThreshold");
        m_ReactiveMaskBinaryValue = serializedObject.FindProperty("m_ReactiveMaskBinaryValue");
    }

    public sealed override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (showsResolutionMode)
            EditorGUILayout.PropertyField(m_ResolutionMode); // two-value enum (QualityMode / CustomScaling); no custom filtering needed

        if (showsReactiveMaskGeneration)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_EnableReactiveMaskPass, Styles.EnableReactiveMaskLabel);
            if (m_EnableReactiveMaskPass.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ReactiveMaskUseComponentMax, Styles.ReactiveMaskUseComponentMaxLabel);
                EditorGUI.BeginChangeCheck();
                float newScale = Mathf.Max(0.0f, EditorGUILayout.FloatField(Styles.ReactiveMaskScaleLabel, m_ReactiveMaskScale.floatValue));
                if (EditorGUI.EndChangeCheck())
                    m_ReactiveMaskScale.floatValue = newScale;
                EditorGUILayout.PropertyField(m_ApplyReactiveMaskThreshold, Styles.ReactiveMaskApplyThresholdLabel);
                if (m_ApplyReactiveMaskThreshold.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Slider(m_ReactiveMaskThreshold, 0.0f, 1.0f, Styles.ReactiveMaskThresholdLabel);
                    EditorGUILayout.Slider(m_ReactiveMaskBinaryValue, 0.0f, 1.0f, Styles.ReactiveMaskBinaryValueLabel);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        DrawOptions();
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Draws the upscaler-specific options, between Update and ApplyModifiedProperties. The default draws every
    /// serialized field except the Script and the (framework-owned) Resolution Mode field.
    /// </summary>
    protected virtual void DrawOptions()
    {
        DrawPropertiesExcluding(serializedObject, "m_Script", "m_ResolutionMode",
            "m_EnableReactiveMaskPass", "m_ReactiveMaskUseComponentMax", "m_ReactiveMaskScale", "m_ApplyReactiveMaskThreshold", "m_ReactiveMaskThreshold", "m_ReactiveMaskBinaryValue");
    }
}
#endif
#endif
