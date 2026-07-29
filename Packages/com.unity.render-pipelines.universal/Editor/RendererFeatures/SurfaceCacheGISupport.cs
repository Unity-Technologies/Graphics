using System;
using UnityEditor.Compilation;

namespace UnityEditor.Rendering.Universal
{
    static class SurfaceCacheGISupport
    {
        const string k_AssemblyName = "Unity.RenderPipelines.Universal.Runtime";
        const string k_SupportedDefine = "SURFACE_CACHE_SUPPORTED";

        internal const string k_UnsupportedWarningMessage =
            "If activated, this Surface Cache GI renderer feature will not be supported on the active Build target.";

        internal const string k_UnsupportedErrorMessage =
            "Surface Cache GI is not supported on the active Build target, remove Surface Cache GI renderer feature or build with a renderer that does not use it.";

        static BuildTarget s_CachedTarget = (BuildTarget)(-1);
        static bool s_CachedSupported;

        internal static bool IsSupportedByActiveBuildTarget(BuildTarget activeBuildTarget)
        {
            if (activeBuildTarget != s_CachedTarget)
            {
                s_CachedTarget = activeBuildTarget;
                s_CachedSupported = false;
                // The runtime assemblies compile on almost all platforms, so only the gating define signals support.
                foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
                {
                    if (assembly.name == k_AssemblyName)
                    {
                        s_CachedSupported = Array.IndexOf(assembly.defines, k_SupportedDefine) >= 0;
                        break;
                    }
                }
            }
            return s_CachedSupported;
        }
    }
}
