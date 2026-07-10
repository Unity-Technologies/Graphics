#if SURFACE_CACHE

using UnityEditor.Compilation;

namespace UnityEditor.Rendering.Universal
{
    static class SurfaceCacheGISupport
    {
        const string k_AssemblyName = "Unity.SurfaceCache.Runtime";

        internal const string k_UnsupportedWarningMessage =
            "If activated, this Surface Cache GI renderer feature will not be supported on the active Build target.";

        internal const string k_UnsupportedErrorMessage =
            "Surface Cache GI is not supported on the active Build target, remove Surface Cache GI renderer feature or build with a renderer that does not use it.";

        static BuildTarget s_CachedTarget = (BuildTarget)(-1);
        static bool s_CachedSupported;

        internal static bool IsSupportedByActiveBuildTarget()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (target != s_CachedTarget)
            {
                s_CachedTarget = target;
                s_CachedSupported = false;
                foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
                {
                    if (assembly.name == k_AssemblyName)
                    {
                        s_CachedSupported = true;
                        break;
                    }
                }
            }
            return s_CachedSupported;
        }
    }
}

#endif
