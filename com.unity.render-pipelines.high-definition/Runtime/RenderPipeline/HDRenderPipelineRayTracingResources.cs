using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineRayTracingResources : ScriptableObject
    {
        // Reflection
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Reflections/RaytracingReflections.raytrace")]
        public RayTracingShader reflectionRaytracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Reflections/RaytracingReflections.compute")]
        public ComputeShader reflectionRaytracingCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingReflectionFilter.compute")]
        public ComputeShader reflectionBilateralFilterCS;

        // Shadows
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadow.raytrace")]
        public RayTracingShader shadowRaytracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RayTracingContactShadow.raytrace")]
        public RayTracingShader contactShadowRayTracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadow.compute")]
        public ComputeShader shadowRaytracingCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadowFilter.compute")]
        public ComputeShader shadowFilterCS;

        // Recursive tracing
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingRenderer.raytrace")]
        public RayTracingShader forwardRaytracing;

        // Light cluster
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightCluster.compute")]
        public ComputeShader lightClusterBuildCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/DebugLightCluster.shader")]
        public Shader lightClusterDebugS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/DebugLightCluster.compute")]
        public ComputeShader lightClusterDebugCS;

        // Indirect Diffuse
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuse/RaytracingIndirectDiffuse.raytrace")]
        public RayTracingShader indirectDiffuseRaytracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuse/RaytracingIndirectDiffuse.compute")]
        public ComputeShader indirectDiffuseRaytracingCS;

        // Ambient Occlusion
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.raytrace")]
        public RayTracingShader aoRaytracing;

        // Sub-Surface Scattering
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RayTracingSubSurface.raytrace")]
        public RayTracingShader subSurfaceRayTracing;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/SubSurface/RayTracingSubSurface.compute")]
        public ComputeShader subSurfaceRayTracingCS;

        // Denoising
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/TemporalFilter.compute")]
        public ComputeShader temporalFilterCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/SimpleDenoiser.compute")]
        public ComputeShader simpleDenoiserCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/DiffuseDenoiser.compute")]
        public ComputeShader diffuseDenoiserCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReflectionDenoiser.compute")]
        public ComputeShader reflectionDenoiserCS;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/DiffuseShadowDenoiser.compute")]
        public ComputeShader diffuseShadowDenoiserCS;

        // Deferred Lighting
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingGBuffer.raytrace")]
        public RayTracingShader gBufferRaytracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingDeferred.compute")]
        public ComputeShader deferredRaytracingCS;

        // Path Tracing
        [Reload("Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMain.raytrace")]
        public RayTracingShader pathTracing;

        // Ray Binning
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Common/RayBinning.compute")]
        public ComputeShader rayBinningCS;

        // Ray count
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/CountTracedRays.compute")]
        public ComputeShader countTracedRays;

        // Filtering for reflections
        [Reload("Runtime/RenderPipelineResources/Texture/ReflectionKernelMapping.png")]
        public Texture2D reflectionFilterMapping;
        [Reload("Runtime/RenderPipelineResources/Texture/ShadowKernelMapping.asset")]
        public Texture3D shadowFilterMapping;
        

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
                    foreach (var field in typeof(HDRenderPipelineRayTracingResources).GetFields())
                        field.SetValue(target, null);

                    ResourceReloader.ReloadAllNullIn(target, HDUtils.GetHDRenderPipelinePath());
                }
            }
        }
    #endif
    }
}
