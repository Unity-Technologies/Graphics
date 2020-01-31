using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipelineRayTracingResources : ScriptableObject
    {
#if ENABLE_RAYTRACING
        // Reflection
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingReflections.raytrace")]
        public RaytracingShader reflectionRaytracing;
#endif
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingReflectionFilter.compute")]
        public ComputeShader reflectionBilateralFilterCS;

        // Shadows
#if ENABLE_RAYTRACING
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/AreaShadows/RaytracingAreaShadows.raytrace")]
        public RaytracingShader areaShadowsRaytracingRT;
#endif
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/AreaShadows/RaytracingAreaShadow.compute")]
        public ComputeShader areaShadowRaytracingCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/AreaShadows/AreaBilateralShadow.compute")]
        public ComputeShader areaShadowFilterCS;

        // Primary visibility
#if ENABLE_RAYTRACING
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingRenderer.raytrace")]
        public RaytracingShader forwardRaytracing;
#endif
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFlagMask.shader")]
        public Shader raytracingFlagMask;

        // Light cluster
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightCluster.compute")]
        public ComputeShader lightClusterBuildCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/DebugLightCluster.compute")]
        public ComputeShader lightClusterDebugCS;

        // Indirect Diffuse
#if ENABLE_RAYTRACING
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIndirectDiffuse.raytrace")]
        public RaytracingShader indirectDiffuseRaytracing;
#endif
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuseAccumulation.compute")]
        public ComputeShader indirectDiffuseAccumulation;            

        // Ambient Occlusion
#if ENABLE_RAYTRACING
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.raytrace")]
        public RaytracingShader aoRaytracing;
#endif
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusionFilter.compute")]
        public ComputeShader raytracingAOFilterCS;

        // Ray count
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/CountTracedRays.compute")]
        public ComputeShader countTracedRays;


    #if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(HDRenderPipelineRayTracingResources))]
        class RenderPipelineRayTracingResourcesEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                // Add a "Reload All" button in inspector when we are in developer's mode
                if (UnityEditor.EditorPrefs.GetBool("DeveloperMode")
                    && GUILayout.Button("Reload All"))
                {
                    var resources = target as HDRenderPipelineRayTracingResources;
                    resources = null;
                    ResourceReloader.ReloadAllNullIn(target, HDUtils.GetHDRenderPipelinePath());
                }
            }
        }
    #endif
    }
}
