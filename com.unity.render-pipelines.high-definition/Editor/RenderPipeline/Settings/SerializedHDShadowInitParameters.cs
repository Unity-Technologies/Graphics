using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDShadowAtlasInitParams
    {
        public SerializedProperty shadowMapResolution;
        public SerializedProperty shadowMapDepthBits;
        public SerializedProperty useDynamicViewportRescale;
    }
    class SerializedHDShadowTiers
    {
        public SerializedProperty lowQualityShadowMap;
        public SerializedProperty mediumQualityShadowMap;
        public SerializedProperty highQualityShadowMap;
        public SerializedProperty veryHighQualityShadowMap;
    }

    class SerializedHDShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty directionalShadowMapDepthBits;

        public SerializedHDShadowAtlasInitParams serializedPunctualAtlasInit = new SerializedHDShadowAtlasInitParams();
        public SerializedHDShadowAtlasInitParams serializedAreaAtlasInit = new SerializedHDShadowAtlasInitParams();

        public SerializedHDShadowTiers serializedDirectionalLightTiers = new SerializedHDShadowTiers();
        public SerializedHDShadowTiers serializedPunctualLightTiers = new SerializedHDShadowTiers();
        public SerializedHDShadowTiers serializedAreaLightTiers = new SerializedHDShadowTiers();

        public SerializedProperty maxDirectionalShadowMapResolution;
        public SerializedProperty maxPunctualShadowMapResolution;
        public SerializedProperty maxAreaShadowMapResolution;

        public SerializedProperty maxShadowRequests;

        public SerializedProperty shadowFilteringQuality;

        public SerializedProperty supportScreenSpaceShadows;
        public SerializedProperty maxScreenSpaceShadows;

        public SerializedHDShadowInitParameters(SerializedProperty root)
        {
            this.root = root;

            directionalShadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.directionalShadowsDepthBits);

            serializedPunctualAtlasInit.shadowMapResolution = root.Find((HDShadowInitParameters s) => s.punctualLightShadowAtlas.shadowAtlasResolution);
            serializedAreaAtlasInit.shadowMapResolution = root.Find((HDShadowInitParameters s) => s.areaLightShadowAtlas.shadowAtlasResolution);
            serializedPunctualAtlasInit.shadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.punctualLightShadowAtlas.shadowAtlasDepthBits);
            serializedAreaAtlasInit.shadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.areaLightShadowAtlas.shadowAtlasDepthBits);
            serializedPunctualAtlasInit.useDynamicViewportRescale = root.Find((HDShadowInitParameters s) => s.punctualLightShadowAtlas.useDynamicViewportRescale);
            serializedAreaAtlasInit.useDynamicViewportRescale = root.Find((HDShadowInitParameters s) => s.areaLightShadowAtlas.useDynamicViewportRescale);
            maxShadowRequests = root.Find((HDShadowInitParameters s) => s.maxShadowRequests);

            serializedDirectionalLightTiers.lowQualityShadowMap = root.Find((HDShadowInitParameters s) => s.directionalLightsResolutionTiers.lowQualityResolution);
            serializedDirectionalLightTiers.mediumQualityShadowMap = root.Find((HDShadowInitParameters s) => s.directionalLightsResolutionTiers.mediumQualityResolution);
            serializedDirectionalLightTiers.highQualityShadowMap = root.Find((HDShadowInitParameters s) => s.directionalLightsResolutionTiers.highQualityResolution);
            serializedDirectionalLightTiers.veryHighQualityShadowMap = root.Find((HDShadowInitParameters s) => s.directionalLightsResolutionTiers.veryHighQualityResolution);
            maxDirectionalShadowMapResolution = root.Find((HDShadowInitParameters s) => s.maxDirectionalShadowMapResolution);

            serializedPunctualLightTiers.lowQualityShadowMap = root.Find((HDShadowInitParameters s) => s.punctualLightsResolutionTiers.lowQualityResolution);
            serializedPunctualLightTiers.mediumQualityShadowMap = root.Find((HDShadowInitParameters s) => s.punctualLightsResolutionTiers.mediumQualityResolution);
            serializedPunctualLightTiers.highQualityShadowMap = root.Find((HDShadowInitParameters s) => s.punctualLightsResolutionTiers.highQualityResolution);
            serializedPunctualLightTiers.veryHighQualityShadowMap = root.Find((HDShadowInitParameters s) => s.punctualLightsResolutionTiers.veryHighQualityResolution);
            maxPunctualShadowMapResolution = root.Find((HDShadowInitParameters s) => s.maxPunctualShadowMapResolution);

            serializedAreaLightTiers.lowQualityShadowMap = root.Find((HDShadowInitParameters s) => s.areaLightsResolutionTiers.lowQualityResolution);
            serializedAreaLightTiers.mediumQualityShadowMap = root.Find((HDShadowInitParameters s) => s.areaLightsResolutionTiers.mediumQualityResolution);
            serializedAreaLightTiers.highQualityShadowMap = root.Find((HDShadowInitParameters s) => s.areaLightsResolutionTiers.highQualityResolution);
            serializedAreaLightTiers.veryHighQualityShadowMap = root.Find((HDShadowInitParameters s) => s.areaLightsResolutionTiers.veryHighQualityResolution);
            maxAreaShadowMapResolution = root.Find((HDShadowInitParameters s) => s.maxAreaShadowMapResolution);


            shadowFilteringQuality = root.Find((HDShadowInitParameters s) => s.shadowFilteringQuality);
            supportScreenSpaceShadows = root.Find((HDShadowInitParameters s) => s.supportScreenSpaceShadows);
            maxScreenSpaceShadows = root.Find((HDShadowInitParameters s) => s.maxScreenSpaceShadows);
        }
    }
}
