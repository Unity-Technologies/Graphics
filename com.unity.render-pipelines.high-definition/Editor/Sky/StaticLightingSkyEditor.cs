using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(StaticLightingSky))]
    [DisallowMultipleComponent]
    class StaticLightingSkyEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Go to LightingWindow for edition."));
        }
    }
}
