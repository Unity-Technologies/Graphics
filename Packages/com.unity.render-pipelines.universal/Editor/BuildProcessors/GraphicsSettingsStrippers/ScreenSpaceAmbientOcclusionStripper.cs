using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    class ScreenSpaceAmbientOcclusionBlueNoiseResourcesStripper : IRenderPipelineGraphicsSettingsStripper<ScreenSpaceAmbientOcclusionBlueNoiseResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

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
                    // The volume can switch the noise method at runtime, so keep the textures for any active feature.
                    if (rendererFeature is ScreenSpaceAmbientOcclusion { isActive: true })
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