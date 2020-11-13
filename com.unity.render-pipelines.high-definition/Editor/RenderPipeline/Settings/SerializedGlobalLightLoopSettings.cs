using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalLightLoopSettings
    {
        public SerializedProperty root;

        public SerializedProperty cookieAtlasSize;
        public SerializedProperty cookieFormat;
        public SerializedProperty cookieAtlasLastValidMip;
#if UNITY_2020_1_OR_NEWER
#else
        public SerializedProperty pointCookieSize;
#endif
        public SerializedProperty reflectionProbeCacheSize;
        public SerializedProperty reflectionCubemapSize;
        public SerializedProperty reflectionCacheCompressed;
        public SerializedProperty reflectionProbeFormat;
        public SerializedProperty planarReflectionAtlasSize;
        public SerializedProperty skyReflectionSize;
        public SerializedProperty skyLightingOverrideLayerMask;
        public SerializedProperty supportFabricConvolution;
        public SerializedProperty maxDirectionalLightsOnScreen;
        public SerializedProperty maxPunctualLightsOnScreen;
        public SerializedProperty maxAreaLightsOnScreen;
        public SerializedProperty maxEnvLightsOnScreen;
        public SerializedProperty maxDecalsOnScreen;
        public SerializedProperty maxPlanarReflectionOnScreen;
        public SerializedProperty maxLightsPerClusterCell;

        public SerializedGlobalLightLoopSettings(SerializedProperty root)
        {
            this.root = root;

            cookieAtlasSize = root.Find((GlobalLightLoopSettings s) => s.cookieAtlasSize);
            cookieFormat = root.Find((GlobalLightLoopSettings s) => s.cookieFormat);
            cookieAtlasLastValidMip = root.Find((GlobalLightLoopSettings s) => s.cookieAtlasLastValidMip);
#if UNITY_2020_1_OR_NEWER
#else
            pointCookieSize = root.Find((GlobalLightLoopSettings s) => s.pointCookieSize);
#endif

            reflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeCacheSize);
            reflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.reflectionCubemapSize);
            reflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.reflectionCacheCompressed);
            reflectionProbeFormat = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeFormat);

            planarReflectionAtlasSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionAtlasSize);

            skyReflectionSize = root.Find((GlobalLightLoopSettings s) => s.skyReflectionSize);
            skyLightingOverrideLayerMask = root.Find((GlobalLightLoopSettings s) => s.skyLightingOverrideLayerMask);
            supportFabricConvolution = root.Find((GlobalLightLoopSettings s) => s.supportFabricConvolution);

            maxDirectionalLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxDirectionalLightsOnScreen);
            maxPunctualLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxPunctualLightsOnScreen);
            maxAreaLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxAreaLightsOnScreen);
            maxEnvLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxEnvLightsOnScreen);
            maxDecalsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxDecalsOnScreen);
            maxPlanarReflectionOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxPlanarReflectionOnScreen);
            maxLightsPerClusterCell = root.Find((GlobalLightLoopSettings s) => s.maxLightsPerClusterCell);
        }
    }
}
