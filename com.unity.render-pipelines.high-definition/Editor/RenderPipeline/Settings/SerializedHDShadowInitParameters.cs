using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDShadowAtlasInitParams
    {
        public SerializedProperty shadowMapResolution;
        public SerializedProperty shadowMapDepthBits;
        public SerializedProperty useDynamicViewportRescale;
    }

    class SerializedHDShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty directionalShadowMapDepthBits;

        public SerializedHDShadowAtlasInitParams serializedPunctualAtlasInit = new SerializedHDShadowAtlasInitParams();
        public SerializedHDShadowAtlasInitParams serializedAreaAtlasInit = new SerializedHDShadowAtlasInitParams();

        public SerializedProperty maxShadowRequests;

        public SerializedProperty shadowQuality;

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
            shadowQuality = root.Find((HDShadowInitParameters s) => s.shadowQuality);
            supportScreenSpaceShadows = root.Find((HDShadowInitParameters s) => s.supportScreenSpaceShadows);
            maxScreenSpaceShadows = root.Find((HDShadowInitParameters s) => s.maxScreenSpaceShadows);
        }
    }
}
