using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Ray Tracing", Order = 1000), HideInInspector]
    class HDRPRayTracingResources : IRenderPipelineResources
    {
        public int version => 0;

        #region Reflection
        [Header("Reflection")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Reflections/RaytracingReflections.raytrace")]
        private RayTracingShader m_ReflectionRayTracingRT;

        public RayTracingShader reflectionRayTracingRT
        {
            get => m_ReflectionRayTracingRT;
            set => this.SetValueAndNotify(ref m_ReflectionRayTracingRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Reflections/RaytracingReflections.compute")]
        private ComputeShader m_ReflectionRayTracingCS;

        public ComputeShader reflectionRayTracingCS
        {
            get => m_ReflectionRayTracingCS;
            set => this.SetValueAndNotify(ref m_ReflectionRayTracingCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingReflectionFilter.compute")]
        private ComputeShader m_ReflectionBilateralFilterCS;
        public ComputeShader reflectionBilateralFilterCS
        {
            get => m_ReflectionBilateralFilterCS;
            set => this.SetValueAndNotify(ref m_ReflectionBilateralFilterCS, value);
        }
        #endregion

        #region Shadows

        [Header("Shadows")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadow.raytrace")]
        private RayTracingShader m_ShadowRayTracingRT;
        public RayTracingShader shadowRayTracingRT
        {
            get => m_ShadowRayTracingRT;
            set => this.SetValueAndNotify(ref m_ShadowRayTracingRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RayTracingContactShadow.raytrace")]
        private RayTracingShader m_ContactShadowRayTracingRT;
        public RayTracingShader contactShadowRayTracingRT
        {
            get => m_ContactShadowRayTracingRT;
            set => this.SetValueAndNotify(ref m_ContactShadowRayTracingRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadow.compute")]
        private ComputeShader m_ShadowRayTracingCS;
        public ComputeShader shadowRayTracingCS
        {
            get => m_ShadowRayTracingCS;
            set => this.SetValueAndNotify(ref m_ShadowRayTracingCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Shadows/RaytracingShadowFilter.compute")]
        private ComputeShader m_ShadowFilterCS;
        public ComputeShader shadowFilterCS
        {
            get => m_ShadowFilterCS;
            set => this.SetValueAndNotify(ref m_ShadowFilterCS, value);
        }

        #endregion

        #region Recursive tracing
        [Header("Recursive tracing")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingRenderer.raytrace")]
        private RayTracingShader m_ForwardRayTracing;
        public RayTracingShader forwardRayTracing
        {
            get => m_ForwardRayTracing;
            set => this.SetValueAndNotify(ref m_ForwardRayTracing, value);
        }
        #endregion

        #region Light cluster

        [Header("Light cluster")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightCluster.compute")]
        private ComputeShader m_LightClusterBuildCS;
        public ComputeShader lightClusterBuildCS
        {
            get => m_LightClusterBuildCS;
            set => this.SetValueAndNotify(ref m_LightClusterBuildCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/DebugLightCluster.shader")]
        private Shader m_LightClusterDebugS;
        public Shader lightClusterDebugS
        {
            get => m_LightClusterDebugS;
            set => this.SetValueAndNotify(ref m_LightClusterDebugS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/DebugLightCluster.compute")]
        private ComputeShader m_LightClusterDebugCS;
        public ComputeShader lightClusterDebugCS
        {
            get => m_LightClusterDebugCS;
            set => this.SetValueAndNotify(ref m_LightClusterDebugCS, value);
        }
        #endregion

        #region Indirect Diffuse
        [Header("Indirect Diffuse")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuse/RaytracingIndirectDiffuse_APVOff.raytrace")]
        private RayTracingShader m_IndirectDiffuseRayTracingOffRT;
        public RayTracingShader indirectDiffuseRayTracingOffRT
        {
            get => m_IndirectDiffuseRayTracingOffRT;
            set => this.SetValueAndNotify(ref m_IndirectDiffuseRayTracingOffRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuse/RaytracingIndirectDiffuse_APVL1.raytrace")]
        private RayTracingShader m_IndirectDiffuseRayTracingL1RT;
        public RayTracingShader indirectDiffuseRayTracingL1RT
        {
            get => m_IndirectDiffuseRayTracingL1RT;
            set => this.SetValueAndNotify(ref m_IndirectDiffuseRayTracingL1RT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuse/RaytracingIndirectDiffuse_APVL2.raytrace")]
        private RayTracingShader m_IndirectDiffuseRaytracingL2RT;
        public RayTracingShader indirectDiffuseRaytracingL2RT
        {
            get => m_IndirectDiffuseRaytracingL2RT;
            set => this.SetValueAndNotify(ref m_IndirectDiffuseRaytracingL2RT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/IndirectDiffuse/RaytracingIndirectDiffuse.compute")]
        private ComputeShader m_IndirectDiffuseRayTracingCS;
        public ComputeShader indirectDiffuseRayTracingCS
        {
            get => m_IndirectDiffuseRayTracingCS;
            set => this.SetValueAndNotify(ref m_IndirectDiffuseRayTracingCS, value);
        }
        #endregion

        #region Ambient Occlusion
        [Header("Ambient Occlusion")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.raytrace")]
        private RayTracingShader m_AoRayTracingRT;
        public RayTracingShader aoRayTracingRT
        {
            get => m_AoRayTracingRT;
            set => this.SetValueAndNotify(ref m_AoRayTracingRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.compute")]
        private ComputeShader m_AoRayTracingCS;
        public ComputeShader aoRayTracingCS
        {
            get => m_AoRayTracingCS;
            set => this.SetValueAndNotify(ref m_AoRayTracingCS, value);
        }

        #endregion

        #region Sub-Surface Scattering
        [Header("Sub-Surface Scattering")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RayTracingSubSurface.raytrace")]
        private RayTracingShader m_SubSurfaceRayTracingRT;
        public RayTracingShader subSurfaceRayTracingRT
        {
            get => m_SubSurfaceRayTracingRT;
            set => this.SetValueAndNotify(ref m_SubSurfaceRayTracingRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/SubSurface/RayTracingSubSurface.compute")]
        private ComputeShader m_SubSurfaceRayTracingCS;
        public ComputeShader subSurfaceRayTracingCS
        {
            get => m_SubSurfaceRayTracingCS;
            set => this.SetValueAndNotify(ref m_SubSurfaceRayTracingCS, value);
        }

        #endregion

        #region Denoising
        [Header("Denoising")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/SimpleDenoiser.compute")]
        private ComputeShader m_SimpleDenoiserCS;
        public ComputeShader simpleDenoiserCS
        {
            get => m_SimpleDenoiserCS;
            set => this.SetValueAndNotify(ref m_SimpleDenoiserCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReflectionDenoiser.compute")]
        private ComputeShader m_ReflectionDenoiserCS;
        public ComputeShader reflectionDenoiserCS
        {
            get => m_ReflectionDenoiserCS;
            set => this.SetValueAndNotify(ref m_ReflectionDenoiserCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/DiffuseShadowDenoiser.compute")]
        private ComputeShader m_DiffuseShadowDenoiserCS;
        public ComputeShader diffuseShadowDenoiserCS
        {
            get => m_DiffuseShadowDenoiserCS;
            set => this.SetValueAndNotify(ref m_DiffuseShadowDenoiserCS, value);
        }
        #endregion

        #region ReBlur
        [Header("ReBlur")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_PreBlur.compute")]
        private ComputeShader m_ReblurPreBlurCS;
        public ComputeShader reblurPreBlurCS
        {
            get => m_ReblurPreBlurCS;
            set => this.SetValueAndNotify(ref m_ReblurPreBlurCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_TemporalAccumulation.compute")]
        private ComputeShader m_ReblurTemporalAccumulationCS;
        public ComputeShader reblurTemporalAccumulationCS
        {
            get => m_ReblurTemporalAccumulationCS;
            set => this.SetValueAndNotify(ref m_ReblurTemporalAccumulationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_MipGeneration.compute")]
        private ComputeShader m_ReblurMipGenerationCS;
        public ComputeShader reblurMipGenerationCS
        {
            get => m_ReblurMipGenerationCS;
            set => this.SetValueAndNotify(ref m_ReblurMipGenerationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_HistoryFix.compute")]
        private ComputeShader m_ReblurHistoryFixCS;
        public ComputeShader reblurHistoryFixCS
        {
            get => m_ReblurHistoryFixCS;
            set => this.SetValueAndNotify(ref m_ReblurHistoryFixCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_Blur.compute")]
        private ComputeShader m_ReblurBlurCS;
        public ComputeShader reblurBlurCS
        {
            get => m_ReblurBlurCS;
            set => this.SetValueAndNotify(ref m_ReblurBlurCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_PostBlur.compute")]
        private ComputeShader m_ReblurPostBlurCS;
        public ComputeShader reblurPostBlurCS
        {
            get => m_ReblurPostBlurCS;
            set => this.SetValueAndNotify(ref m_ReblurPostBlurCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_CopyHistory.compute")]
        private ComputeShader m_ReblurCopyHistoryCS;
        public ComputeShader reblurCopyHistoryCS
        {
            get => m_ReblurCopyHistoryCS;
            set => this.SetValueAndNotify(ref m_ReblurCopyHistoryCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/ReBlur/ReBlur_TemporalStabilization.compute")]
        private ComputeShader m_ReblurTemporalStabilizationCS;
        public ComputeShader reblurTemporalStabilizationCS
        {
            get => m_ReblurTemporalStabilizationCS;
            set => this.SetValueAndNotify(ref m_ReblurTemporalStabilizationCS, value);
        }
        #endregion

        #region Deferred Lighting

        [Header("Deferred Lighting")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingGBuffer.raytrace")]
        private RayTracingShader m_GBufferRayTracingRT;
        public RayTracingShader gBufferRayTracingRT
        {
            get => m_GBufferRayTracingRT;
            set => this.SetValueAndNotify(ref m_GBufferRayTracingRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingDeferred.compute")]
        private ComputeShader m_DeferredRayTracingCS;
        public ComputeShader deferredRayTracingCS
        {
            get => m_DeferredRayTracingCS;
            set => this.SetValueAndNotify(ref m_DeferredRayTracingCS, value);
        }


        #endregion

        #region Path Tracing
        [Header("Path Tracing")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMain.raytrace")]
        private RayTracingShader m_PathTracingRT;
        public RayTracingShader pathTracingRT
        {
            get => m_PathTracingRT;
            set => this.SetValueAndNotify(ref m_PathTracingRT, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSkySamplingData.compute")]
        private ComputeShader m_PathTracingSkySamplingDataCS;
        public ComputeShader pathTracingSkySamplingDataCS
        {
            get => m_PathTracingSkySamplingDataCS;
            set => this.SetValueAndNotify(ref m_PathTracingSkySamplingDataCS, value);
        }

        #endregion

        #region Ray Marching
        [Header("Ray Marching")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RayMarching.compute")]
        private ComputeShader m_RayMarchingCS;
        public ComputeShader rayMarchingCS
        {
            get => m_RayMarchingCS;
            set => this.SetValueAndNotify(ref m_RayMarchingCS, value);
        }

        #endregion

        #region Ray Binning
        [Header("Ray Binning")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Common/RayBinning.compute")]
        private ComputeShader m_RayBinningCS;
        public ComputeShader rayBinningCS
        {
            get => m_RayBinningCS;
            set => this.SetValueAndNotify(ref m_RayBinningCS, value);
        }


        #endregion

        #region Ray count
        [Header("Ray count")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/CountTracedRays.compute")]
        private ComputeShader m_CountTracedRaysCS;
        public ComputeShader countTracedRaysCS
        {
            get => m_CountTracedRaysCS;
            set => this.SetValueAndNotify(ref m_CountTracedRaysCS, value);
        }

        #endregion

        #region Filtering for reflections
        [Header("Filtering for reflections")]
        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/Texture/ReflectionKernelMapping.png")]
        public Texture2D m_ReflectionFilterMappingTexture;

        public Texture2D reflectionFilterMappingTexture
        {
            get => m_ReflectionFilterMappingTexture;
            set => this.SetValueAndNotify(ref m_ReflectionFilterMappingTexture, value);
        }

        #endregion

        #region Debug
        [Header("Debug")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/RTASDebug.raytrace")]
        public RayTracingShader m_RtasDebugRT;

        public RayTracingShader debugRTASRT
        {
            get => m_RtasDebugRT;
            set => this.SetValueAndNotify(ref m_RtasDebugRT, value);
        }
        #endregion
    }
}
