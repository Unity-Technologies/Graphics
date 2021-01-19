using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditorForRenderPipeline(typeof(Projector), typeof(UniversalRenderPipelineAsset))]
    [CanEditMultipleObjects]
    public class UniversalRenderPipelineProjectorEditor : Editor
    {
        const string k_Message = "The active render pipeline (URP) does not support the Projector component.";

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(k_Message, MessageType.Warning);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawDefaultInspector();
            }
        }
    }
}
