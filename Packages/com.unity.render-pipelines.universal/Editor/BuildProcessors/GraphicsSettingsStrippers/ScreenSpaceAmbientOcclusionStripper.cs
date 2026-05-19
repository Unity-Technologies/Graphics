using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    class ScreenSpaceAmbientOcclusionBlueNoiseResourcesStripper : IRenderPipelineGraphicsSettingsStripper<ScreenSpaceAmbientOcclusionBlueNoiseResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        static bool RequiresBlueNoiseResources(ScreenSpaceAmbientOcclusion occlusion)
        {
#if MODERN_SSAO
            return true;
#else
            return occlusion.settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
#endif
        }

        public bool CanRemoveSettings(ScreenSpaceAmbientOcclusionBlueNoiseResources resources)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpShaderStrippingSettings) && !urpShaderStrippingSettings.stripUnusedVariants)
                return false;
            
            foreach (var rendererData in URPBuildData.instance.rendererDataList)
            {
                if (rendererData is not UniversalRendererData)
                    continue;

                foreach (var rendererFeature in rendererData.rendererFeatures)
                {
                    if (rendererFeature is ScreenSpaceAmbientOcclusion { isActive: true } occlusion
                        && RequiresBlueNoiseResources(occlusion))
                        return false;
                }
            }

            return true;
        }
    }

    class ScreenSpaceAmbientOcclusionCoreResourcesStripper : IRenderPipelineGraphicsSettingsStripper<ScreenSpaceAmbientOcclusionCoreResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(ScreenSpaceAmbientOcclusionCoreResources resources)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpShaderStrippingSettings) && !urpShaderStrippingSettings.stripUnusedVariants)
                return false;
            
            foreach (var rendererData in URPBuildData.instance.rendererDataList)
            {
                if (rendererData is not UniversalRendererData)
                    continue;

                foreach (var rendererFeature in rendererData.rendererFeatures)
                {
                    if (rendererFeature is ScreenSpaceAmbientOcclusion { isActive: true })
                        return false;
                }
            }

            return true;
        }
    }
}