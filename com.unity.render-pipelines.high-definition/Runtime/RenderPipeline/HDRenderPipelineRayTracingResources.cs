using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipelineRayTracingResources : ScriptableObject
    {
#if ENABLE_RAYTRACING
        // Reflection
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingReflections.raytrace")]
        public RayTracingShader reflectionRaytracing;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingReflectionFilter.compute")]
        public ComputeShader reflectionBilateralFilterCS;

        // Shadows
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadow.raytrace")]
        public RayTracingShader shadowRaytracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadow.compute")]
        public ComputeShader shadowRaytracingCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadowFilter.compute")]
        public ComputeShader shadowFilterCS;

        // Primary visibility
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingRenderer.raytrace")]
        public RayTracingShader forwardRaytracing;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFlagMask.shader")]
        public Shader raytracingFlagMask;

        // Light cluster
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightCluster.compute")]
        public ComputeShader lightClusterBuildCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/DebugLightCluster.compute")]
        public ComputeShader lightClusterDebugCS;

        // Indirect Diffuse
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIndirectDiffuse.raytrace")]
        public RayTracingShader indirectDiffuseRaytracing;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuseAccumulation.compute")]
        public ComputeShader indirectDiffuseAccumulation;

        // Ambient Occlusion
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.raytrace")]
        public RayTracingShader aoRaytracing;

        // Denoising
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/SimpleDenoiser.compute")]
        public ComputeShader simpleDenoiserCS;

        // Ray count
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/CountTracedRays.compute")]
        public ComputeShader countTracedRays;
#endif

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
