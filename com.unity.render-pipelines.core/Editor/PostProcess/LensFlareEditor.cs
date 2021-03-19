using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEngine
{
    /// <summary>
    /// Editor for LensFlare (builtin): Editor to show an error message
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Flare))]
    public class LensFlareEditor : Editor
    {
        /// <summary>
        /// Implement this function to make a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This asset doesn't work on SRP {URP & HDRP}, use SRP Lens Flare instead.", MessageType.Error);
        }
    }
}
