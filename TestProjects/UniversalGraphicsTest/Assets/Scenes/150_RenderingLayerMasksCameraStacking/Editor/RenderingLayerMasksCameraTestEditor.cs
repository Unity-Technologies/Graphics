using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[CustomEditor(typeof(RenderingLayerMasksCameraTest))]
public class RenderingLayerMasksCameraTestEditor : Editor
{
    SerializedProperty m_BaseCamera;
    SerializedProperty m_OverlayCamera;

    void Init()
    {
        m_BaseCamera = serializedObject.FindProperty("m_BaseCamera");
        m_OverlayCamera = serializedObject.FindProperty("m_OverlayCamera");
    }

    public override void OnInspectorGUI()
    {
        if (m_BaseCamera == null || m_OverlayCamera == null)
            Init();

        serializedObject.Update();
        string[] layerNames = UniversalRenderPipeline.asset.renderingLayerMaskNames;

        DrawCameraData("Base Camera", ref layerNames, ref m_BaseCamera);
        DrawCameraData("Overlay Camera 1", ref layerNames, ref m_OverlayCamera);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawCameraData(string label, ref string[] layerNames, ref SerializedProperty camData)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        SerializedProperty forwardRendererData = camData.FindPropertyRelative("forwardRendererData");
        EditorGUILayout.PropertyField(forwardRendererData);
        SerializedProperty renderingLayerMask = camData.FindPropertyRelative("renderingLayerMask");
        renderingLayerMask.longValue = (uint) EditorGUILayout.MaskField("Rendering Layer Mask", renderingLayerMask.intValue, layerNames);
        EditorGUILayout.Space();
    }
}
