#if ENABLE_UPSCALER_FRAMEWORK
#if UNITY_EDITOR
using UnityEditor;

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

    /// <summary>Override to true for quality-mode upscalers so the Resolution Mode dropdown is shown.</summary>
    protected virtual bool showsResolutionMode => false;

    protected virtual void OnEnable()
    {
        m_ResolutionMode = serializedObject.FindProperty("m_ResolutionMode");
    }

    public sealed override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (showsResolutionMode)
            EditorGUILayout.PropertyField(m_ResolutionMode); // two-value enum (QualityMode / CustomScaling); no custom filtering needed
        DrawOptions();
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Draws the upscaler-specific options, between Update and ApplyModifiedProperties. The default draws every
    /// serialized field except the Script and the (framework-owned) Resolution Mode field.
    /// </summary>
    protected virtual void DrawOptions()
    {
        DrawPropertiesExcluding(serializedObject, "m_Script", "m_ResolutionMode");
    }
}
#endif
#endif
