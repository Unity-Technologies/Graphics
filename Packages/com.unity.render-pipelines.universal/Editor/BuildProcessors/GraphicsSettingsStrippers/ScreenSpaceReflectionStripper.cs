using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    class ScreenSpaceReflectionPersistentResourcesStripper : IRenderPipelineGraphicsSettingsStripper<ScreenSpaceReflectionPersistentResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(ScreenSpaceReflectionPersistentResources resources)
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
                        if (rendererFeature is ScreenSpaceReflectionRendererFeature { isActive: true })
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
