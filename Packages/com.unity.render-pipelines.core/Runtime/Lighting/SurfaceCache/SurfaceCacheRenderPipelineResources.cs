#if SURFACE_CACHE

using System;

namespace UnityEngine.Rendering
{
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R: Surface Cache Core Resources", Order = 1000), HideInInspector]
    sealed class SurfaceCacheRenderPipelineResourceSet : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 3;

        int IRenderPipelineGraphicsSettings.version => m_Version;

        [ResourcePath("Runtime/Lighting/SurfaceCache/TemporalFiltering.compute")]
        public ComputeShader m_TemporalFilteringShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/SpatialFiltering.compute")]
        public ComputeShader m_SpatialFilteringShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Scrolling.compute")]
        public ComputeShader m_ScrollingShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Defrag.compute")]
        public ComputeShader m_DefragShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Eviction.compute")]
        public ComputeShader m_EvictionShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/PunctualLightSampling.urtshader")]
        public ComputeShader m_PunctualLightSamplingComputeShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/PunctualLightSampling.urtshader")]
        public RayTracingShader m_PunctualLightSamplingRayTracingShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Estimation.urtshader")]
        public ComputeShader m_EstimationComputeShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Estimation.urtshader")]
        public RayTracingShader m_EstimationRayTracingShader;

        public ComputeShader spatialFilteringShader
        {
            get => m_SpatialFilteringShader;
            set => this.SetValueAndNotify(ref m_SpatialFilteringShader, value, nameof(m_SpatialFilteringShader));
        }

        public ComputeShader temporalFilteringShader
        {
            get => m_TemporalFilteringShader;
            set => this.SetValueAndNotify(ref m_TemporalFilteringShader, value, nameof(m_TemporalFilteringShader));
        }

        public ComputeShader punctualLightSamplingComputeShader
        {
            get => m_PunctualLightSamplingComputeShader;
            set => this.SetValueAndNotify(ref m_PunctualLightSamplingComputeShader, value, nameof(m_PunctualLightSamplingComputeShader));
        }

        public RayTracingShader punctualLightSamplingRayTracingShader
        {
            get => m_PunctualLightSamplingRayTracingShader;
            set => this.SetValueAndNotify(ref m_PunctualLightSamplingRayTracingShader, value, nameof(m_PunctualLightSamplingRayTracingShader));
        }

        public ComputeShader estimationComputeShader
        {
            get => m_EstimationComputeShader;
            set => this.SetValueAndNotify(ref m_EstimationComputeShader, value, nameof(m_EstimationComputeShader));
        }

        public RayTracingShader estimationRayTracingShader
        {
            get => m_EstimationRayTracingShader;
            set => this.SetValueAndNotify(ref m_EstimationRayTracingShader, value, nameof(m_EstimationRayTracingShader));
        }

        public ComputeShader defragShader
        {
            get => m_DefragShader;
            set => this.SetValueAndNotify(ref m_DefragShader, value, nameof(m_DefragShader));
        }

        public ComputeShader scrollingShader
        {
            get => m_ScrollingShader;
            set => this.SetValueAndNotify(ref m_ScrollingShader, value, nameof(m_ScrollingShader));
        }

        public ComputeShader evictionShader
        {
            get => m_EvictionShader;
            set => this.SetValueAndNotify(ref m_EvictionShader, value, nameof(m_EvictionShader));
        }
    }
}

#endif
