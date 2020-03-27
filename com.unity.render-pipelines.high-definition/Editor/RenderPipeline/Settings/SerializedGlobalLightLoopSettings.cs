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
        public SerializedProperty pointCookieSize;
        public SerializedProperty cubeCookieTexArraySize;
        public SerializedProperty reflectionProbeCacheSize;
        public SerializedProperty reflectionCubemapSize;
        public SerializedProperty reflectionCacheCompressed;
        public SerializedProperty planarReflectionAtlasSize;
        public SerializedProperty planarReflectionCacheCompressed;
        public SerializedProperty skyReflectionSize;
        public SerializedProperty skyLightingOverrideLayerMask;
        public SerializedProperty supportFabricConvolution;
        public SerializedProperty maxDirectionalLightsOnScreen;
        public SerializedProperty maxPunctualLightsOnScreen;
        public SerializedProperty maxAreaLightsOnScreen; 
        public SerializedProperty maxEnvLightsOnScreen;
        public SerializedProperty maxDecalsOnScreen;
        public SerializedProperty maxPlanarReflectionOnScreen;

        public SerializedGlobalLightLoopSettings(SerializedProperty root)
        {
            this.root = root;

            cookieAtlasSize = root.Find((GlobalLightLoopSettings s) => s.cookieAtlasSize);
            cookieFormat = root.Find((GlobalLightLoopSettings s) => s.cookieFormat);
            cookieAtlasLastValidMip = root.Find((GlobalLightLoopSettings s) => s.cookieAtlasLastValidMip);
            pointCookieSize = root.Find((GlobalLightLoopSettings s) => s.pointCookieSize);
            cubeCookieTexArraySize = root.Find((GlobalLightLoopSettings s) => s.cubeCookieTexArraySize);

            reflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeCacheSize);
            reflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.reflectionCubemapSize);
            reflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.reflectionCacheCompressed);

            planarReflectionAtlasSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionAtlasSize);
            planarReflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.planarReflectionCacheCompressed);

            skyReflectionSize = root.Find((GlobalLightLoopSettings s) => s.skyReflectionSize);
            skyLightingOverrideLayerMask = root.Find((GlobalLightLoopSettings s) => s.skyLightingOverrideLayerMask);
            supportFabricConvolution = root.Find((GlobalLightLoopSettings s) => s.supportFabricConvolution);

            maxDirectionalLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxDirectionalLightsOnScreen);
            maxPunctualLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxPunctualLightsOnScreen);
            maxAreaLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxAreaLightsOnScreen);
            maxEnvLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxEnvLightsOnScreen);
            maxDecalsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxDecalsOnScreen);
            maxPlanarReflectionOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxPlanarReflectionOnScreen);
        }
    }
}
