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

        public enum CellSize
        {
            [InspectorName("1")]
            CellSize1 = 1,
            [InspectorName("9")]
            CellSize9 = 9,
            [InspectorName("27")]
            CellSize27 = 27,
            [InspectorName("81")]
            CellSize81 = 81,
            [InspectorName("243")]
            CellSize243 = 243,
        }

        // This field will be replaced by something else (probably a distance based setting in meter) when the artists decide
        // what they want. So, we shouldn't rely on this information too much.
        /// <summary>
        /// The size of a Cell.
        /// </summary>
        public CellSize cellSizeInBricks = CellSize.CellSize81;

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

        [SerializeField]
        Version version = CoreUtils.GetLastEnumValue<Version>();

        public int maxSubdivision => Mathf.CeilToInt(Mathf.Log((float)cellSize / brickSize, 3));
        public float brickSize => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);
        public int cellSize => Mathf.CeilToInt((float)cellSizeInBricks * brickSize);

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
        private SerializedProperty m_MinDistanceBetweenProbes;
        private SerializedProperty m_NormalBias;
        private SerializedProperty m_IndexDimensions;
        ProbeReferenceVolumeProfile profile => target as ProbeReferenceVolumeProfile;

        sealed class Styles
        {
            // TODO: Better tooltip are needed here.
            public readonly GUIContent cellSizeStyle = new GUIContent("Brick Count Per Cell", "Determine how much bricks there is in a streamable unit.");
            public readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Distance Between Probes", "The minimal distance between two probes in meters.");
            public readonly GUIContent normalBias = new GUIContent("Normal Bias", "The normal bias used when sampling the volume. It can reduce leaking.");
            public readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
        }

        static Styles s_Styles = new Styles();

        private void OnEnable()
        {
            m_CellSize = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.cellSizeInBricks));
            m_MinDistanceBetweenProbes = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.minDistanceBetweenProbes));
            m_NormalBias = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.normalBias));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_CellSize, s_Styles.cellSizeStyle);
            EditorGUILayout.PropertyField(m_MinDistanceBetweenProbes, s_Styles.minDistanceBetweenProbes);
            EditorGUILayout.HelpBox("Maximum subvision of the volume: " + profile.maxSubdivision, MessageType.Info);
            EditorGUILayout.PropertyField(m_NormalBias, s_Styles.normalBias);

            ProbeReferenceVolume.instance.normalBiasFromProfile = m_NormalBias.floatValue;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                float minDistanceBetweenProbes = ((float)profile.cellSize / Mathf.Pow(3, ProbeBrickIndex.kMaxSubdivisionLevels)) / 3.0f;
                if (profile.minDistanceBetweenProbes < minDistanceBetweenProbes)
                    profile.minDistanceBetweenProbes = minDistanceBetweenProbes;
            }
        }
    }
#endif
}
