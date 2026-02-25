#if SURFACE_CACHE
using System.Collections.Generic;
using UnityEditor.Rendering.Universal;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering
{
    static class SurfaceCacheStripperUtility
    {
        static bool IsSurfaceCacheEnabled(List<UniversalRenderPipelineAsset> urpAssets)
        {
            foreach (var urpAssetForBuild in urpAssets)
            {
                foreach (var rendererData in urpAssetForBuild.m_RendererDataList)
                {
                    if(rendererData is not UniversalRendererData)
                        continue;

                    foreach (var rendererFeature in rendererData.rendererFeatures)
                    {
                        if (rendererFeature is SurfaceCacheGIRendererFeature { isActive: true })
                            return true;
                    }
                }
            }
            return false;
        }

        internal static bool CanRemoveSurfaceCacheSettings(List<UniversalRenderPipelineAsset> urpAssets)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpShaderStrippingSettings) && !urpShaderStrippingSettings.stripUnusedVariants)
                return false;

            if (IsSurfaceCacheEnabled(urpAssets))
                return false;

            return true;
        }
    }

    class UniversalSurfaceCacheIntegrationStripper : IRenderPipelineGraphicsSettingsStripper<UnityEngine.Rendering.Universal.SurfaceCacheRenderPipelineResourceSet>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(UnityEngine.Rendering.Universal.SurfaceCacheRenderPipelineResourceSet settings)
        {
            return SurfaceCacheStripperUtility.CanRemoveSurfaceCacheSettings(URPBuildData.instance.renderPipelineAssets);
        }
    }

    class UniversalSurfaceCacheCoreStripper : IRenderPipelineGraphicsSettingsStripper<UnityEngine.Rendering.SurfaceCacheRenderPipelineResourceSet>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(UnityEngine.Rendering.SurfaceCacheRenderPipelineResourceSet settings)
        {
            return SurfaceCacheStripperUtility.CanRemoveSurfaceCacheSettings(URPBuildData.instance.renderPipelineAssets);
        }
    }

    class RayTracingResourcesStripper : IRenderPipelineGraphicsSettingsStripper<RayTracingRenderPipelineResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(RayTracingRenderPipelineResources settings)
        {
            return SurfaceCacheStripperUtility.CanRemoveSurfaceCacheSettings(URPBuildData.instance.renderPipelineAssets);
        }
    }

    class PathTracingWorldResourcesStripper : IRenderPipelineGraphicsSettingsStripper<WorldRenderPipelineResources>
    {
        public bool active => URPBuildData.instance.buildingPlayerForUniversalRenderPipeline;

        public bool CanRemoveSettings(WorldRenderPipelineResources settings)
        {
            return SurfaceCacheStripperUtility.CanRemoveSurfaceCacheSettings(URPBuildData.instance.renderPipelineAssets);
        }
    }
}
#endif
