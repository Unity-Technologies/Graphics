using System.Collections.Generic;
using System.Linq;
using Unity.RenderPipelines.Core.Runtime.Shared;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// HDRP Rendering Debugger Display Stats.
    /// </summary>
    class HDDebugDisplayStats : DebugDisplayStats
    {
        static bool IsRaytracingEnabled() => HDRenderPipeline.isReady &&
                                             HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings
                                                 .supportRayTracing;

        /// <summary>Display ray tracing ray count per frame.</summary>
        public bool countRays = false;

        // Bridge: returns ProfilingSamplers via ProfilingSampler.Get(HDProfileId.X).
        // Replace with HDRPProfilerMarkers direct field references once HDRPProfilerMarkers is created.
        List<ProfilingSampler> GetDefaultSamplers() => GetSamplers(
            HDProfileId.HDRenderPipelineAllRenderRequest,
            HDProfileId.VolumeUpdate,
            HDProfileId.RenderShadowMaps,
            HDProfileId.GBuffer,
            HDProfileId.PrepareLightsForGPU,
            HDProfileId.VolumeVoxelization,
            HDProfileId.VolumetricLighting,
            HDProfileId.VolumetricClouds,
            HDProfileId.VolumetricCloudsTrace,
            HDProfileId.VolumetricCloudsReproject,
            HDProfileId.VolumetricCloudsUpscale,
            HDProfileId.VolumetricCloudsCombine,
            HDProfileId.RenderDeferredLightingCompute,
            HDProfileId.ForwardOpaque,
            HDProfileId.ForwardTransparent,
            HDProfileId.ForwardPreRefraction,
            HDProfileId.ColorPyramid,
            HDProfileId.DepthPyramid,
            HDProfileId.PostProcessing
        );

        List<ProfilingSampler> GetRayTracingSamplers() => GetSamplers(
            HDProfileId.RaytracingBuildCluster,
            HDProfileId.RaytracingCullLights,
            HDProfileId.RaytracingBuildAccelerationStructure,

            // Ray Traced Reflections
            HDProfileId.RaytracingReflectionDirectionGeneration,
            HDProfileId.RaytracingReflectionEvaluation,
            HDProfileId.RaytracingReflectionAdjustWeight,
            HDProfileId.RaytracingReflectionUpscale,
            HDProfileId.RaytracingReflectionFilter,

            // Ray Traced Ambient Occlusion
            HDProfileId.RaytracingAmbientOcclusion,
            HDProfileId.RaytracingFilterAmbientOcclusion,

            // Ray Traced Shadows
            HDProfileId.RaytracingDirectionalLightShadow,
            HDProfileId.RaytracingLightShadow,

            // Ray Traced Indirect Diffuse
            HDProfileId.RaytracingIndirectDiffuseDirectionGeneration,
            HDProfileId.RaytracingIndirectDiffuseEvaluation,
            HDProfileId.RaytracingIndirectDiffuseUpscale,
            HDProfileId.RaytracingFilterIndirectDiffuse,
            HDProfileId.RaytracingDebugOverlay,
            HDProfileId.ForwardPreRefraction,
            HDProfileId.RayTracingRecursiveRendering,
            HDProfileId.RayTracingDepthPrepass,
            HDProfileId.RayTracingFlagMask,
            HDProfileId.RaytracingDeferredLighting
        );

        static List<ProfilingSampler> GetSamplers(params HDProfileId[] ids)
        {
            var result = new List<ProfilingSampler>(ids.Length);
            foreach (var id in ids)
            {
                var sampler = ProfilingSampler.Get(id);
                if (sampler != null)
                    result.Add(sampler);
            }

            return result;
        }

        /// <inheritdoc/>
        public override void EnableProfilingRecorders()
        {
            AddAndEnableProfilingSamplers(GetDefaultSamplers());
            base.EnableProfilingRecorders();
            if (IsRaytracingEnabled())
                AddAndEnableProfilingSamplers(GetRayTracingSamplers());
        }

        /// <inheritdoc/>
        public override void RegisterDebugUI(List<DebugUI.Widget> list)
        {
            base.RegisterDebugUI(list);

            var detailedStats = DebugUIUtilities.GetFoldoutByName("Detailed Stats", list);
            var detailedFoldout = detailedStats != null ? DebugUIUtilities.GetFoldoutByName("Profiling Scopes", detailedStats.children) : null;
            detailedFoldout?.children.InsertRange(0,  BuildProfilingSamplerWidgetList(GetDefaultSamplers()));

            var raytracingScopes = BuildDetailedStatsList("Ray Tracing Profiling Scopes", GetRayTracingSamplers());
            raytracingScopes.isHiddenCallback = () => !IsRaytracingEnabled();
            detailedStats.children.Add(raytracingScopes);
            detailedStats.children.Add(new DebugUI.BoolField
            {
                displayName = "Count Rays (MRays/Frame)",
                getter = () => countRays,
                setter = value => countRays = value,
                isHiddenCallback = () => !IsRaytracingEnabled()
            });
            detailedStats.children.Add(new DebugUI.Container
            {
                isHiddenCallback = () => !IsRaytracingEnabled() || !countRays,
                children =
                {
                    CreateRayCountDebugValue("Ambient Occlusion", RayCountValues.AmbientOcclusion),
                    CreateRayCountDebugValue("Shadows Directional", RayCountValues.ShadowDirectional),
                    CreateRayCountDebugValue("Shadows Area", RayCountValues.ShadowAreaLight),
                    CreateRayCountDebugValue("Shadows Point/Spot", RayCountValues.ShadowPointSpot),
                    CreateRayCountDebugValue("Reflections Forward ", RayCountValues.ReflectionForward),
                    CreateRayCountDebugValue("Reflections Deferred", RayCountValues.ReflectionDeferred),
                    CreateRayCountDebugValue("Diffuse GI Forward", RayCountValues.DiffuseGI_Forward),
                    CreateRayCountDebugValue("Diffuse GI Deferred", RayCountValues.DiffuseGI_Deferred),
                    CreateRayCountDebugValue("Recursive Rendering", RayCountValues.Recursive),
                    CreateRayCountDebugValue("Total", RayCountValues.Total),
                }
            });
        }

        DebugUI.Value CreateRayCountDebugValue(string name, RayCountValues counter)
        {
            return new DebugUI.Value
            {
                displayName = name,
                getter = () => (HDRenderPipeline.currentPipeline?.GetRaysPerFrame(counter) ?? 0) / 1e6f,
                refreshRate = 1f / 30f
            };
        }
    }
}
