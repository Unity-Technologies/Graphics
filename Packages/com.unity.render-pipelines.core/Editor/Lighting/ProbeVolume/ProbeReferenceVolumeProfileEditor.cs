using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeProfile))]
    internal class ProbeReferenceVolumeProfileEditor : Editor
    {
        SerializedProperty m_MinDistanceBetweenProbes;
        SerializedProperty m_SimplificationLevels;
        SerializedProperty m_MinRendererVolumeSize;
        SerializedProperty m_RenderersLayerMask;
        ProbeReferenceVolumeProfile profile => target as ProbeReferenceVolumeProfile;

        static class Styles
        {
            // TODO: Better tooltip are needed here.
            public static readonly GUIContent simplificationLevels = new GUIContent("Simplification levels", "Determine how many bricks are in a streamable unit. Each simplification step adds a brick level above the minimum one.");
            public static readonly string simplificationLevelsHighWarning = "High simplification levels have a big memory overhead, they are not recommended except for testing purposes.";
            public static readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Distance Between Probes", "The minimal distance between two probes in meters.");
            public static readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
            public static readonly GUIContent minRendererVolumeSize = new GUIContent("Min Renderer Volume Size", "Specifies the minimum bounding box volume of renderers to consider placing probes around.");
            public static readonly GUIContent renderersLayerMask = new GUIContent("Layer Mask", "Specifies the layer mask for renderers when placing probes.");
            public static readonly GUIContent rendererFilterSettings = new GUIContent("Renderers Filter Settings");
        }

        void OnEnable()
        {
            m_MinDistanceBetweenProbes = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.minDistanceBetweenProbes));
            m_SimplificationLevels = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.simplificationLevels));
            m_MinRendererVolumeSize = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.minRendererVolumeSize));
            m_RenderersLayerMask = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.renderersLayerMask));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SimplificationLevels, Styles.simplificationLevels);
            if (m_SimplificationLevels.intValue == 5)
            {
                EditorGUILayout.HelpBox(Styles.simplificationLevelsHighWarning, MessageType.Warning);
            }
            EditorGUILayout.PropertyField(m_MinDistanceBetweenProbes, Styles.minDistanceBetweenProbes);
            EditorGUILayout.HelpBox($"The distance between probes will fluctuate between: {profile.minDistanceBetweenProbes}m and {profile.cellSizeInMeters / 3.0f}m", MessageType.Info);

            EditorGUILayout.Space();
            if (ProbeVolumeBakingWindow.Foldout(Styles.rendererFilterSettings, ProbeVolumeBakingWindow.Expandable.RendererFilterSettings))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RenderersLayerMask, Styles.renderersLayerMask);
                EditorGUILayout.PropertyField(m_MinRendererVolumeSize, Styles.minRendererVolumeSize);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
