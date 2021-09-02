using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [HDRPHelpURL("Default-Settings-Window")]
    partial class HDRenderPipelineRayTracingResources : HDRenderPipelineResources
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
        public RayTracingShader aoRaytracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.compute")]
        public ComputeShader aoRaytracingCS;

        // Sub-Surface Scattering
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RayTracingSubSurface.raytrace")]
        public RayTracingShader subSurfaceRayTracingRT;
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/SubSurface/RayTracingSubSurface.compute")]
        public ComputeShader subSurfaceRayTracingCS;

        // Denoising
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/SimpleDenoiser.compute")]
        public ComputeShader simpleDenoiserCS;
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

        // Ray Marching
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/RayMarching.compute")]
        public ComputeShader rayMarchingCS;

        // Ray Binning
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Common/RayBinning.compute")]
        public ComputeShader rayBinningCS;

        // Ray count
        [Reload("Runtime/RenderPipeline/Raytracing/Shaders/CountTracedRays.compute")]
        public ComputeShader countTracedRays;

        // Filtering for reflections
        [Reload("Runtime/RenderPipelineResources/Texture/ReflectionKernelMapping.png")]
        public Texture2D reflectionFilterMapping;
    }
}
