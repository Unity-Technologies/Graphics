using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    class SerializedGlobalLightLoopSettings
    {
        public SerializedProperty root;

        public SerializedProperty cookieAtlasWidth;
        public SerializedProperty cookieAtlasHeight;
        public SerializedProperty reflectionProbeCacheSize;
        public SerializedProperty reflectionCubemapSize;
        public SerializedProperty reflectionCacheCompressed;
        public SerializedProperty planarReflectionProbeCacheSize;
        public SerializedProperty planarReflectionCubemapSize;
        public SerializedProperty planarReflectionCacheCompressed;
        public SerializedProperty skyReflectionSize;
        public SerializedProperty skyLightingOverrideLayerMask;

        public SerializedGlobalLightLoopSettings(SerializedProperty root)
        {
            this.root = root;

            cookieAtlasWidth = root.Find((GlobalLightLoopSettings s) => s.cookieAtlasWidth);
            cookieAtlasHeight = root.Find((GlobalLightLoopSettings s) => s.cookieAtlasHeight);

            reflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeCacheSize);
            reflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.reflectionCubemapSize);
            reflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.reflectionCacheCompressed);

            planarReflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionProbeCacheSize);
            planarReflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionTextureSize);
            planarReflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.planarReflectionCacheCompressed);

            skyReflectionSize = root.Find((GlobalLightLoopSettings s) => s.skyReflectionSize);
            skyLightingOverrideLayerMask = root.Find((GlobalLightLoopSettings s) => s.skyLightingOverrideLayerMask);
        }
    }
}
