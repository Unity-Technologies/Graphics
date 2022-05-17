namespace UnityEngine.Rendering
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

        // TODO: This is here just to find a place where to serialize it. It might not be the best spot.
        [SerializeField]
        internal bool freezePlacement = false;

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

        /// <summary>
        /// Layer mask filter for all renderers.
        /// </summary>
        public LayerMask renderersLayerMask = -1;

        /// <summary>
        /// Specifies the minimum bounding box volume of renderers to consider placing probes around.
        /// </summary>
        [Min(0)]
        public float minRendererVolumeSize = 0.1f;

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
                simplificationLevels == otherProfile.simplificationLevels &&
                renderersLayerMask == otherProfile.renderersLayerMask;
        }
    }
}
