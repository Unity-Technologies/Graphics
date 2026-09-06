using UnityEditor.Build.Profile;
using UnityEngine;

namespace UnityEditor.Rendering
{
    static class BuildProfileModuleUtilBridge
    {
        public static Texture2D GetPlatformIconSmall(GUID platformGuid)
            => BuildProfileModuleUtil.GetPlatformIconSmall(platformGuid);
    }
}
