#if URP_SHADOW_CACHING

namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL]
    internal enum ShadowCachingDebugState
    {
        /// <summary>No shadow caching debug color.</summary>
        None,
        /// <summary>Shadows come from the cache.</summary>
        Cached,
        /// <summary>Shadows were rendered into the cache recently.</summary>
        RecentlyRefreshed,
        /// <summary>Shadows are rendered this frame.</summary>
        Live,
        /// <summary>Shadows do not cover the camera, and nothing has refreshed them.</summary>
        Stale,
    }
}

#endif
