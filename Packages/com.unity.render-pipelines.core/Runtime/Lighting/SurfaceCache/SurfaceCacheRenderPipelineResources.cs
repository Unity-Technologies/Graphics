using System;

namespace UnityEngine.Rendering
{
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R: Surface Cache Core Resources", Order = 1000), HideInInspector]
    sealed class SurfaceCacheRenderPipelineResourceSet : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 5;

        int IRenderPipelineGraphicsSettings.version => m_Version;

        [ResourcePath("Runtime/Lighting/SurfaceCache/TemporalFiltering.compute")]
        public ComputeShader m_TemporalFilteringShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/SpatialFiltering.compute")]
        public ComputeShader m_SpatialFilteringShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Scrolling.compute")]
        public ComputeShader m_ScrollingShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/GlobalProbe.compute")]
        public ComputeShader m_GlobalProbeShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Defrag.compute")]
        public ComputeShader m_DefragShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Estimation.compute")]
        public ComputeShader m_EstimationShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/PunctualLightSampling.compute")]
        public ComputeShader m_PunctualLightSamplingShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/Eviction.compute")]
        public ComputeShader m_EvictionShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/PatchAllocation.compute")]
        public ComputeShader m_PatchAllocationShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/EmissiveTriangleAddition.compute")]
        public ComputeShader m_EmissiveTriangleAdditionComputeShader;

        [ResourcePath("Runtime/Lighting/SurfaceCache/EmissiveTriangleRemoval.compute")]
        public ComputeShader m_EmissiveTriangleRemovalComputeShader;

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

        public ComputeShader emissiveTriangleAdditionComputeShader
        {
            get => m_EmissiveTriangleAdditionComputeShader;
            set => this.SetValueAndNotify(ref m_EmissiveTriangleAdditionComputeShader, value, nameof(m_EmissiveTriangleAdditionComputeShader));
        }

        public ComputeShader emissiveTriangleRemovalComputeShader
        {
            get => m_EmissiveTriangleRemovalComputeShader;
            set => this.SetValueAndNotify(ref m_EmissiveTriangleRemovalComputeShader, value, nameof(m_EmissiveTriangleRemovalComputeShader));
        }

        public ComputeShader estimationShader
        {
            get => m_EstimationShader;
            set => this.SetValueAndNotify(ref m_EstimationShader, value, nameof(m_EstimationShader));
        }

        public ComputeShader punctualLightSamplingShader
        {
            get => m_PunctualLightSamplingShader;
            set => this.SetValueAndNotify(ref m_PunctualLightSamplingShader, value, nameof(m_PunctualLightSamplingShader));
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

        public ComputeShader globalProbeShader
        {
            get => m_GlobalProbeShader;
            set => this.SetValueAndNotify(ref m_GlobalProbeShader, value, nameof(m_GlobalProbeShader));
        }

        public ComputeShader evictionShader
        {
            get => m_EvictionShader;
            set => this.SetValueAndNotify(ref m_EvictionShader, value, nameof(m_EvictionShader));
        }

        public ComputeShader patchAllocationShader
        {
            get => m_PatchAllocationShader;
            set => this.SetValueAndNotify(ref m_PatchAllocationShader, value, nameof(m_PatchAllocationShader));
        }
    }
}
