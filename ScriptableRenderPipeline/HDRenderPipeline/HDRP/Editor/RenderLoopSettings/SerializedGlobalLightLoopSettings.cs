using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    class SerializedGlobalLightLoopSettings
    {
        public SerializedProperty root;

        public SerializedProperty spotCookieSize;
        public SerializedProperty cookieTexArraySize;
        public SerializedProperty pointCookieSize;
        public SerializedProperty cubeCookieTexArraySize;
        public SerializedProperty reflectionProbeCacheSize;
        public SerializedProperty reflectionCubemapSize;
        public SerializedProperty reflectionCacheCompressed;
        public SerializedProperty planarReflectionProbeCacheSize;
        public SerializedProperty planarReflectionCubemapSize;
        public SerializedProperty planarReflectionCacheCompressed;
        public SerializedProperty maxPlanarReflectionProbes;
        public SerializedProperty skyReflectionSize;
        public SerializedProperty skyLightingOverrideLayerMask;

        public SerializedGlobalLightLoopSettings(SerializedProperty root)
        {
            this.root = root;

            spotCookieSize = root.Find((GlobalLightLoopSettings s) => s.spotCookieSize);
            cookieTexArraySize = root.Find((GlobalLightLoopSettings s) => s.cookieTexArraySize);
            pointCookieSize = root.Find((GlobalLightLoopSettings s) => s.pointCookieSize);
            cubeCookieTexArraySize = root.Find((GlobalLightLoopSettings s) => s.cubeCookieTexArraySize);

            reflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeCacheSize);
            reflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.reflectionCubemapSize);
            reflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.reflectionCacheCompressed);

            planarReflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionProbeCacheSize);
            planarReflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionTextureSize);
            planarReflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.planarReflectionCacheCompressed);
            maxPlanarReflectionProbes = root.Find((GlobalLightLoopSettings s) => s.maxPlanarReflectionProbes);

            skyReflectionSize = root.Find((GlobalLightLoopSettings s) => s.skyReflectionSize);
            skyLightingOverrideLayerMask = root.Find((GlobalLightLoopSettings s) => s.skyLightingOverrideLayerMask);
        }
    }
}
