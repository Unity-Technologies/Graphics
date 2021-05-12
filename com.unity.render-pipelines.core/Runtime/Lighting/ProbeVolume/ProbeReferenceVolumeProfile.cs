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
        [Range(2, 6)]
        public int simplificationLevels = 3;

        /// <summary>
        /// The size of a Cell in number of bricks.
        /// </summary>
        public int cellSizeInBricks => (int)Mathf.Pow(3, simplificationLevels);

        /// <summary>
        /// The minimum distance between two probes in meters.
        /// </summary>
        [Min(0.01f)]
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
                maxSubdivision == otherProfile.maxSubdivision;
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

        sealed class Styles
        {
            // TODO: Better tooltip are needed here.
            public readonly GUIContent simplificationLevels = new GUIContent("Simplification levels", "Determine how much bricks there is in a streamable unit.");
            public readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Distance Between Probes", "The minimal distance between two probes in meters.");
            public readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
        }

        static Styles s_Styles = new Styles();

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

            EditorGUILayout.PropertyField(m_SimplificationLevels, s_Styles.simplificationLevels);
            EditorGUILayout.PropertyField(m_MinDistanceBetweenProbes, s_Styles.minDistanceBetweenProbes);
            EditorGUILayout.HelpBox($"The distance between probes will fluctuate between : {profile.minDistanceBetweenProbes}m and {profile.cellSizeInMeters}m", MessageType.Info);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
