using UnityEditor;

namespace UnityEngine
{
    /// <summary>
    /// Editor for Flare (builtin): Editor to show an error message
    /// </summary>
    [CustomEditorForRenderPipeline(typeof(UnityEngine.Flare), typeof(Rendering.RenderPipelineAsset))]
    class FlareEditor : Editor
    {
        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This asset doesn't work on SRP, use Lens Flare (SRP) instead.", MessageType.Error);
        }
    }

    /// <summary>
    /// Editor for Lens Flare (builtin): Editor to show an error message
    /// </summary>
    [CustomEditorForRenderPipeline(typeof(UnityEngine.LensFlare), typeof(Rendering.RenderPipelineAsset))]
    class LensFlareEditor : Editor
    {
        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This component doesn't work on SRP, use Lens Flare (SRP) instead.", MessageType.Error);
        }
    }
}
