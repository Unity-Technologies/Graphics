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
        public float outOfGeoOffset;
        public float searchMultiplier;
    }

    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    [System.Serializable]
    internal struct ProbeVolumeBakingProcessSettings
    {
        public ProbeDilationSettings dilationSettings;
        public VirtualOffsetSettings virtualOffsetSettings;
    }
}
