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
        public SerializedProperty reflectionCacheCompressed;
        public SerializedProperty reflectionProbeFormat;
        public SerializedProperty reflectionProbeTexCacheSize;
        public SerializedProperty reflectionProbeTexLastValidCubeMip;
        public SerializedProperty reflectionProbeTexLastValidPlanarMip;
        public SerializedProperty reflectionProbeDecreaseResToFit;
        public SerializedProperty skyReflectionSize;
        public SerializedProperty skyLightingOverrideLayerMask;
        public SerializedProperty supportFabricConvolution;
        public SerializedProperty maxDirectionalLightsOnScreen;
        public SerializedProperty maxPunctualLightsOnScreen;
        public SerializedProperty maxAreaLightsOnScreen;
        public SerializedProperty maxCubeReflectionsOnScreen;
        public SerializedProperty maxPlanarReflectionsOnScreen;
        public SerializedProperty maxDecalsOnScreen;
        public SerializedProperty maxLightsPerClusterCell;
        public SerializedProperty maxLocalVolumetricFogOnScreen;

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
            reflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.reflectionCacheCompressed);
            reflectionProbeFormat = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeFormat);
            reflectionProbeTexCacheSize = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeTexCacheSize);
            reflectionProbeTexLastValidCubeMip = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeTexLastValidCubeMip);
            reflectionProbeTexLastValidPlanarMip = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeTexLastValidPlanarMip);
            reflectionProbeDecreaseResToFit = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeDecreaseResToFit);

            skyReflectionSize = root.Find((GlobalLightLoopSettings s) => s.skyReflectionSize);
            skyLightingOverrideLayerMask = root.Find((GlobalLightLoopSettings s) => s.skyLightingOverrideLayerMask);
            supportFabricConvolution = root.Find((GlobalLightLoopSettings s) => s.supportFabricConvolution);

            maxDirectionalLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxDirectionalLightsOnScreen);
            maxPunctualLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxPunctualLightsOnScreen);
            maxAreaLightsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxAreaLightsOnScreen);
            maxCubeReflectionsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxCubeReflectionOnScreen);
            maxPlanarReflectionsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxPlanarReflectionOnScreen);
            maxDecalsOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxDecalsOnScreen);
            maxLightsPerClusterCell = root.Find((GlobalLightLoopSettings s) => s.maxLightsPerClusterCell);

            maxLocalVolumetricFogOnScreen = root.Find((GlobalLightLoopSettings s) => s.maxLocalVolumetricFogOnScreen);
        }
    }
}
