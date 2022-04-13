using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>ScriptableRendererFeature</c> class.
    /// </summary>
    [CustomEditor(typeof(ScriptableRendererFeature), true)]
    public class ScriptableRendererFeatureEditor : Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script");
        }
    }
}
