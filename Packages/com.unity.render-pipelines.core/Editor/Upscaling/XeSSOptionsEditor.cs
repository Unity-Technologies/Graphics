#if ENABLE_XESS && ENABLE_INTELXESS_MODULE
using UnityEditor;
using UnityEngine;
using UnityEngine.Intel;

[CustomEditor(typeof(XeSSOptions))]
internal class XeSSOptionsEditor : UpscalerOptionsEditor
{
    protected override bool showsReactiveMaskGeneration => true;

    // Declare variables to hold each property
    private SerializedProperty m_QualityMode;

    protected override void OnEnable()
    {
        base.OnEnable();
        // Find each property by its exact field name in XeSSOptions.cs
        m_QualityMode = serializedObject.FindProperty("m_XeSSQualityMode");
    }

    protected override void DrawOptions()
    {
        EditorGUILayout.PropertyField(m_QualityMode);
    }
}
#endif
