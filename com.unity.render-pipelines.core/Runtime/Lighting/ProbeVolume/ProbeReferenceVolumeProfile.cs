using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Probe Reference Volume"/>.
    /// </summary>
    public sealed class ProbeReferenceVolumeProfile : ScriptableObject
    {
        internal enum Version
        {
            Initial,
        }

        [SerializeField]
        Version version = CoreUtils.GetLastEnumValue<Version>();

        /// <summary>
        /// How many levels contains the probes hierarchical structure.
        /// </summary>
        [Range(2, 5)]
        public int simplificationLevels = 3;

        /// <summary>
        /// The size of a Cell in number of bricks.
        /// </summary>
        public int cellSizeInBricks => (int)Mathf.Pow(3, simplificationLevels);

        /// <summary>
        /// The minimum distance between two probes in meters.
        /// </summary>
        [Min(0.1f)]
        public float minDistanceBetweenProbes = 1.0f;

        /// <summary>
        /// Maximum subdivision in the structure.
        /// </summary>
        public int maxSubdivision => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell

        /// <summary>
        /// Minimum size of a brick in meters.
        /// </summary>
        public float minBrickSize => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);

        /// <summary>
        /// Size of the cell in meters.
        /// </summary>
        public float cellSizeInMeters => (float)cellSizeInBricks * minBrickSize;

        void OnEnable()
        {
            if (version != CoreUtils.GetLastEnumValue<Version>())
            {
                // Migration code
            }
        }

        /// <summary>
        /// Determines if the Probe Reference Volume Profile is equivalent to another one.
        /// </summary>
        /// <param name ="otherProfile">The profile to compare with.</param>
        /// <returns>Whether the Probe Reference Volume Profile is equivalent to another one.</returns>
        public bool IsEquivalent(ProbeReferenceVolumeProfile otherProfile)
        {
            return minDistanceBetweenProbes == otherProfile.minDistanceBetweenProbes &&
                cellSizeInMeters == otherProfile.cellSizeInMeters &&
                simplificationLevels == otherProfile.simplificationLevels;
        }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeProfile))]
    internal class ProbeReferenceVolumeProfileEditor : Editor
    {
        SerializedProperty m_CellSize;
        SerializedProperty m_MinDistanceBetweenProbes;
        SerializedProperty m_SimplificationLevels;
        ProbeReferenceVolumeProfile profile => target as ProbeReferenceVolumeProfile;

        static class Styles
        {
            // TODO: Better tooltip are needed here.
            public static readonly GUIContent simplificationLevels = new GUIContent("Simplification levels", "Determine how many bricks are in a streamable unit. Each simplification step adds a brick level above the minimum one.");
            public static readonly string simplificationLevelsHighWarning = "High simplification levels have a big memory overhead, they are not recommended except for testing purposes.";
            public static readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Distance Between Probes", "The minimal distance between two probes in meters.");
            public static readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
        }

        void OnEnable()
        {
            m_CellSize = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.cellSizeInBricks));
            m_MinDistanceBetweenProbes = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.minDistanceBetweenProbes));
            m_SimplificationLevels = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.simplificationLevels));
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
            EditorGUILayout.HelpBox($"The distance between probes will fluctuate between : {profile.minDistanceBetweenProbes}m and {profile.cellSizeInMeters / 3.0f}m", MessageType.Info);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
