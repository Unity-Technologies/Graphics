using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    class ScreenSpaceAmbientOcclusionDynamicResourcesStripper : IRenderPipelineGraphicsSettingsStripper<ScreenSpaceAmbientOcclusionDynamicResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(ScreenSpaceAmbientOcclusionDynamicResources resources)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpShaderStrippingSettings) && !urpShaderStrippingSettings.stripUnusedVariants)
                return false;
            
            foreach (var urpAssetForBuild in URPBuildData.instance.renderPipelineAssets)
            {
                foreach (var rendererData in urpAssetForBuild.m_RendererDataList)
                {
                    if (rendererData is not UniversalRendererData) 
                        continue;
                    
                    foreach (var rendererFeature in rendererData.rendererFeatures)
                    {
                        if (rendererFeature is ScreenSpaceAmbientOcclusion { isActive: true } occlusion
                            && occlusion.settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise)
                            return false;
                    }
                }
            }

            return true;
        }
    }

    class ScreenSpaceAmbientOcclusionPersistentResourcesStripper : IRenderPipelineGraphicsSettingsStripper<ScreenSpaceAmbientOcclusionPersistentResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(ScreenSpaceAmbientOcclusionPersistentResources resources)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpShaderStrippingSettings) && !urpShaderStrippingSettings.stripUnusedVariants)
                return false;
            
            foreach (var urpAssetForBuild in URPBuildData.instance.renderPipelineAssets)
            {
                foreach (var rendererData in urpAssetForBuild.m_RendererDataList)
                {
                    if (rendererData is not UniversalRendererData)
                        continue;
                    
                    foreach (var rendererFeature in rendererData.rendererFeatures)
                    {
                        if (rendererFeature is ScreenSpaceAmbientOcclusion { isActive: true })
                            return false;
                    }
                }
            }

            return true;
        }
    }
}