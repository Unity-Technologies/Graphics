using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    /// <summary>
    /// Editor for Lens Flare (builtin): Editor to show an error message
    /// </summary>
    [CustomEditor(typeof(LensFlare))]
    [SupportedOnRenderPipeline]
    [CanEditMultipleObjects]
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
