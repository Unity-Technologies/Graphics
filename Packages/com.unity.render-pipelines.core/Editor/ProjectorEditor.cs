using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(Projector))]
    [CanEditMultipleObjects]
    class ProjectorEditor : Editor
    {
        static readonly GUIContent k_Message =
            EditorGUIUtility.TrTextContent("The active render pipeline does not support the Projector component. If using HDRP or URP, use the Decal Projector component instead.");

        public override void OnInspectorGUI()
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
                EditorGUILayout.HelpBox(k_Message.text, MessageType.Warning);
            else
                DrawDefaultInspector();
        }
    }
}
