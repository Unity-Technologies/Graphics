using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(Projector))]
    [SupportedOnRenderPipeline]
    [CanEditMultipleObjects]
    class ProjectorEditor : Editor
    {
        static readonly GUIContent k_Message = EditorGUIUtility.TrTextContent("The active render pipeline does not support the Projector component. If using HDRP, use the Decal Projector component instead.");

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(k_Message.text, MessageType.Warning);

            using (new EditorGUI.DisabledScope(true))
            {
                // Projector doesn't have a CustomEditor, so we can just draw the default inspector.
                DrawDefaultInspector();
            }
        }
    }
}
