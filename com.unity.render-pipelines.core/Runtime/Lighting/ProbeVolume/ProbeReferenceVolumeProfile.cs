using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Probe Reference Volume"/>.
    /// </summary>
    public sealed class ProbeReferenceVolumeProfile : ScriptableObject
    {
        /// <summary>
        /// The default dimensions for APV's index data structure.
        /// </summary>
        public static Vector3Int s_DefaultIndexDimensions = new Vector3Int(1024, 64, 1024);

        /// <summary>
        /// The size of a Cell.
        /// </summary>
        public int cellSize = 64;
        /// <summary>
        /// The size of a Brick.
        /// </summary>
        public int brickSize = 4;
        /// <summary>
        /// Max subdivision.
        /// </summary>
        public int maxSubdivision = 2;
        /// <summary>
        /// The normal bias to apply during shading.
        /// </summary>
        public float normalBias = 0.2f;
        /// <summary>
        /// Index field dimensions.
        /// </summary>
        public Vector3Int indexDimensions = s_DefaultIndexDimensions;

        /// <summary>
        /// Determines if the Probe Reference Volume Profile is equivalent to another one.
        /// </summary>
        /// <param name ="otherProfile">The profile to compare with.</param>
        /// <returns>Whether the Probe Reference Volume Profile is equivalent to another one.</returns>
        public bool IsEquivalent(ProbeReferenceVolumeProfile otherProfile)
        {
            return brickSize == otherProfile.brickSize &&
                cellSize == otherProfile.cellSize &&
                maxSubdivision == otherProfile.maxSubdivision &&
                normalBias == otherProfile.normalBias;
        }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeProfile))]
    internal class ProbeReferenceVolumeProfileEditor : Editor
    {
        private SerializedProperty m_CellSize;
        private SerializedProperty m_BrickSize;
        private SerializedProperty m_MaxSubdivision;
        private SerializedProperty m_NormalBias;
        private SerializedProperty m_IndexDimensions;

        sealed class Styles
        {
            // TODO: Better tooltip are needed here.
            public readonly GUIContent cellSizeStyle = new GUIContent("Cell Size", "Determine the size of the cells.");
            public readonly GUIContent brickSizeStyle = new GUIContent("Brick Size", "The smallest possible brick size.");
            public readonly GUIContent maxSubdivStyle = new GUIContent("Max Subdivision", "The maximum number of possible subdivisions.");
            public readonly GUIContent normalBias = new GUIContent("Normal Bias", "The normal bias used when sampling the volume. It can reduce leaking.");
            public readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
        }

        static Styles s_Styles = new Styles();

        private void OnEnable()
        {
            m_CellSize = serializedObject.FindProperty("cellSize");
            m_BrickSize = serializedObject.FindProperty("brickSize");
            m_MaxSubdivision = serializedObject.FindProperty("maxSubdivision");
            m_NormalBias = serializedObject.FindProperty("normalBias");
            m_IndexDimensions = serializedObject.FindProperty("indexDimensions");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_CellSize, s_Styles.cellSizeStyle);
            EditorGUILayout.PropertyField(m_BrickSize, s_Styles.brickSizeStyle);
            EditorGUILayout.PropertyField(m_MaxSubdivision, s_Styles.maxSubdivStyle);
            EditorGUILayout.DelayedFloatField(m_NormalBias, s_Styles.normalBias);
            EditorGUILayout.PropertyField(m_IndexDimensions, s_Styles.indexDimensions);

            ProbeReferenceVolume.instance.normalBiasFromProfile = m_NormalBias.floatValue;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
