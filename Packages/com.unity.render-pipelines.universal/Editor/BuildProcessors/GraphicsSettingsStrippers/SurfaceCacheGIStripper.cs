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
        static bool IsSurfaceCacheEnabled(UniversalRenderPipelineAsset urpAsset)
        {
            foreach (var rendererData in urpAsset.m_RendererDataList)
            {
                if(rendererData is not UniversalRendererData)
                    continue;

                foreach (var rendererFeature in rendererData.rendererFeatures)
                {
                    if (rendererFeature is SurfaceCacheGIRendererFeature { isActive: true })
                        return true;
                }
            }
            return false;
        }

        static bool IsSurfaceCacheEnabled(List<UniversalRenderPipelineAsset> urpAssets)
        {
            foreach (var urpAssetForBuild in urpAssets)
            {
                if (IsSurfaceCacheEnabled(urpAssetForBuild))
                    return true;
            }
            return false;
        }

        internal static bool CanRemoveSurfaceCacheSettings(List<UniversalRenderPipelineAsset> urpAssets)
        {
            // TODO: UUM-148447. Unsupported targets should ideally never ship the resources, but the support check only answers for the active target.
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (URPBuildData.instance.buildTarget == activeBuildTarget &&
                !SurfaceCacheGISupport.IsSupportedByActiveBuildTarget(activeBuildTarget))
                return true;

            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpShaderStrippingSettings) && !urpShaderStrippingSettings.stripUnusedVariants)
                return false;

            if (IsSurfaceCacheEnabled(urpAssets))
                return false;

            return true;
        }

        // Required by native side for meta pass stripping.
        [UnityEngine.Scripting.Preserve]
        internal static bool IsSurfaceCacheEnabledForBuild()
        {
            // Check if we're building with URP and if Surface Cache is enabled in the build assets
            if (URPBuildData.instance.buildingPlayerForUniversalRenderPipeline)
            {
                return IsSurfaceCacheEnabled(URPBuildData.instance.renderPipelineAssets);
            }

            // Fallback to current render pipeline asset if not in a build context
            var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (currentRP is UniversalRenderPipelineAsset urpAsset)
            {
                return IsSurfaceCacheEnabled(urpAsset);
            }

            return false;
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
