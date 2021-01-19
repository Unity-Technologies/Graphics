using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditorForRenderPipeline(typeof(Projector), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    public class HighDefinitionRenderPipelineProjectorEditor : Editor
    {
        const string k_Message = "The active render pipeline (HDRP) does not support the Projector component. Use the Decal Projector component instead.";

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
