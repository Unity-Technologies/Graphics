using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    internal struct ProbeVolumeBakingProcessSettings
    {
        public ProbeDilationSettings dilationSettings;
        public VirtualOffsetSettings virtualOffsetSettings;
    }
}
