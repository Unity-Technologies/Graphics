using System;
using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDShadowAtlasInitParams
    {
        public SerializedProperty shadowMapResolution;
        public SerializedProperty cachedResolution;
        public SerializedProperty shadowMapDepthBits;
        public SerializedProperty useDynamicViewportRescale;
    }

    class SerializedHDShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty directionalShadowMapDepthBits;

        public SerializedHDShadowAtlasInitParams serializedPunctualAtlasInit;
        public SerializedHDShadowAtlasInitParams serializedAreaAtlasInit;

        public SerializedScalableSetting shadowResolutionDirectional;
        public SerializedScalableSetting shadowResolutionPunctual;
        public SerializedScalableSetting shadowResolutionArea;

        public SerializedProperty maxDirectionalShadowMapResolution;
        public SerializedProperty maxPunctualShadowMapResolution;
        public SerializedProperty maxAreaShadowMapResolution;

        public SerializedProperty maxShadowRequests;

        public SerializedProperty shadowFilteringQuality;

        public SerializedProperty supportScreenSpaceShadows;
        public SerializedProperty maxScreenSpaceShadowSlots;
        public SerializedProperty screenSpaceShadowBufferFormat;

        public SerializedHDShadowInitParameters(SerializedProperty root)
        {
            this.root = root;

            directionalShadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.directionalShadowsDepthBits);

            serializedPunctualAtlasInit = new SerializedHDShadowAtlasInitParams
            {
                shadowMapResolution = root.Find((HDShadowInitParameters s) => s.punctualLightShadowAtlas.shadowAtlasResolution),
                cachedResolution = root.Find((HDShadowInitParameters s) => s.cachedPunctualLightShadowAtlas),
                shadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.punctualLightShadowAtlas.shadowAtlasDepthBits),
                useDynamicViewportRescale = root.Find((HDShadowInitParameters s) => s.punctualLightShadowAtlas.useDynamicViewportRescale)
            };

            serializedAreaAtlasInit = new SerializedHDShadowAtlasInitParams
            {
                shadowMapResolution = root.Find((HDShadowInitParameters s) => s.areaLightShadowAtlas.shadowAtlasResolution),
                cachedResolution = root.Find((HDShadowInitParameters s) => s.cachedAreaLightShadowAtlas),
                shadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.areaLightShadowAtlas.shadowAtlasDepthBits),
                useDynamicViewportRescale = root.Find((HDShadowInitParameters s) => s.areaLightShadowAtlas.useDynamicViewportRescale)
            };

            maxShadowRequests = root.Find((HDShadowInitParameters s) => s.maxShadowRequests);

            shadowResolutionDirectional = new SerializedScalableSetting(root.Find((HDShadowInitParameters s) => s.shadowResolutionDirectional));
            shadowResolutionPunctual = new SerializedScalableSetting(root.Find((HDShadowInitParameters s) => s.shadowResolutionPunctual));
            shadowResolutionArea = new SerializedScalableSetting(root.Find((HDShadowInitParameters s) => s.shadowResolutionArea));
            maxDirectionalShadowMapResolution = root.Find((HDShadowInitParameters s) => s.maxDirectionalShadowMapResolution);
            maxPunctualShadowMapResolution = root.Find((HDShadowInitParameters s) => s.maxPunctualShadowMapResolution);
            maxAreaShadowMapResolution = root.Find((HDShadowInitParameters s) => s.maxAreaShadowMapResolution);

            shadowFilteringQuality = root.Find((HDShadowInitParameters s) => s.shadowFilteringQuality);
            supportScreenSpaceShadows = root.Find((HDShadowInitParameters s) => s.supportScreenSpaceShadows);
            maxScreenSpaceShadowSlots = root.Find((HDShadowInitParameters s) => s.maxScreenSpaceShadowSlots);
            screenSpaceShadowBufferFormat = root.Find((HDShadowInitParameters s) => s.screenSpaceShadowBufferFormat);
        }
    }
}
