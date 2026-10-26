using UnityEngine;

namespace UnityEditor.Rendering
{
    static class BuildTargetDiscoveryBridge
    {
        public static GUID GetGUIDFromBuildTarget(BuildTarget buildTarget)
            => BuildTargetDiscovery.GetGUIDFromBuildTarget(buildTarget);

        public static BuildTarget GetBuildTargetFromGUID(GUID platformGuid)
            => BuildTargetDiscovery.TryGetBuildTarget(platformGuid, out IBuildTarget buildTarget)
                ? (BuildTarget)buildTarget.GetLegacyId
                : BuildTarget.NoTarget;
    }
}
