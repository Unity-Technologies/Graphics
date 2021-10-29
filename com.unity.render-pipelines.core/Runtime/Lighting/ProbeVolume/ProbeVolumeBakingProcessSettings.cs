using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    [System.Serializable]
    internal struct ProbeDilationSettings
    {
        public bool enableDilation;
        public float dilationDistance;
        public float dilationValidityThreshold;
        public int dilationIterations;
        public bool squaredDistWeighting;
    }

    [System.Serializable]
    internal struct VirtualOffsetSettings
    {
        public bool useVirtualOffset;
        [Range(0f, 1f)] public float outOfGeoOffset;
        [Range(0f, 2f)]public float searchMultiplier;
        [Range(-0.05f, 0f)] public float rayOriginBias;
        [Range(4, 24)] public int maxHitsPerRay;
        public LayerMask collisionMask;
        public bool useMultiThreadedOffset;
    }

    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    [System.Serializable]
    internal struct ProbeVolumeBakingProcessSettings
    {
        public ProbeDilationSettings dilationSettings;
        public VirtualOffsetSettings virtualOffsetSettings;
    }
}
