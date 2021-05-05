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
        /// How much the probes structure contains hierarchical levels
        /// </summary>
        [Range(2, 4)]
        public int simplificationLevels = 3;

        // This field will be replaced by something else (probably a distance based setting in meter) when the artists decide
        // what they want. So, we shouldn't rely on this information too much.
        /// <summary>
        /// The size of a Cell.
        /// </summary>
        public int cellSizeInBricks => (int)Mathf.Pow(simplificationLevels, 3);

        /// <summary>
        /// The size of a Brick.
        /// </summary>
        [Min(0.1f)]
        public float minDistanceBetweenProbes = 1.0f;

        /// <summary>
        /// The normal bias to apply during shading.
        /// </summary>
        [Range(0.0f, 1.0f), Delayed]
        public float normalBias = 0.2f;

        /// <summary>
        /// Maximum subdivision in the structure.
        /// </summary>
        public int maxSubdivision => simplificationLevels + 1;

        public float minBrickSize => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);

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
        private SerializedProperty m_MinDistanceBetweenProbes;
        private SerializedProperty m_IndexDimensions;
        private SerializedProperty m_SimplificationLevels;
        ProbeReferenceVolumeProfile profile => target as ProbeReferenceVolumeProfile;

        sealed class Styles
        {
            // TODO: Better tooltip are needed here.
            public readonly GUIContent simplificationLevels = new GUIContent("Simplification levels", "Determine how much bricks there is in a streamable unit.");
            public readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Distance Between Probes", "The minimal distance between two probes in meters.");
            public readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
        }

        static Styles s_Styles = new Styles();

        private void OnEnable()
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
            {
                serializedObject.ApplyModifiedProperties();

                float minDistanceBetweenProbes = ((float)profile.cellSizeInBricks / Mathf.Pow(3, ProbeBrickIndex.kMaxSubdivisionLevels)) / 3.0f;
                if (profile.minDistanceBetweenProbes < minDistanceBetweenProbes)
                    profile.minDistanceBetweenProbes = minDistanceBetweenProbes;
            }
        }
    }
#endif
}
