using System;

namespace UnityEngine.Rendering
{
    /// <summary> Class for tracking resources used by the GPUResidentDrawer </summary>
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R: GPU Resident Drawers", Order = 1000), HideInInspector]
    class GPUResidentDrawerResources : IRenderPipelineResources
    {
        public enum Version
        {
            Initial,

            Count,
            Latest = Count - 1
        }
        [SerializeField, HideInInspector] Version m_Version = Version.Latest;
        int IRenderPipelineGraphicsSettings.version => (int)m_Version;

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/InstanceDataBufferCopyKernels.compute")]
        ComputeShader m_InstanceDataBufferCopyKernels;
        
        /// <summary> InstanceDataBufferCopyKernels compute shader reference </summary>
        public ComputeShader instanceDataBufferCopyKernels
        {
            get => m_InstanceDataBufferCopyKernels;
            set => this.SetValueAndNotify(ref m_InstanceDataBufferCopyKernels, value, nameof(m_InstanceDataBufferCopyKernels));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/InstanceDataBufferUploadKernels.compute")]
        ComputeShader m_InstanceDataBufferUploadKernels;
        
        /// <summary> InstanceDataBufferUploadKernels compute shader reference </summary>
        public ComputeShader instanceDataBufferUploadKernels
        {
            get => m_InstanceDataBufferUploadKernels;
            set => this.SetValueAndNotify(ref m_InstanceDataBufferUploadKernels, value, nameof(m_InstanceDataBufferUploadKernels));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/InstanceTransformUpdateKernels.compute")]
        ComputeShader m_TransformUpdaterKernels;
        
        /// <summary> TransformUpdaterKernels compute shader reference </summary>
        public ComputeShader transformUpdaterKernels
        {
            get => m_TransformUpdaterKernels;
            set => this.SetValueAndNotify(ref m_TransformUpdaterKernels, value, nameof(m_TransformUpdaterKernels));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/InstanceWindDataUpdateKernels.compute")]
        public ComputeShader m_WindDataUpdaterKernels;

        /// <summary>
        /// InstanceWindDataUpdateKernels compute shader reference
        /// </summary>
        public ComputeShader windDataUpdaterKernels
        {
            get => m_WindDataUpdaterKernels;
            set => this.SetValueAndNotify(ref m_WindDataUpdaterKernels, value, nameof(m_WindDataUpdaterKernels));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/OccluderDepthPyramidKernels.compute")]
        ComputeShader m_OccluderDepthPyramidKernels;

        /// <summary>
        /// OccluderDepthPyramidKernels compute shader reference
        /// </summary>
        public ComputeShader occluderDepthPyramidKernels
        {
            get => m_OccluderDepthPyramidKernels;
            set => this.SetValueAndNotify(ref m_OccluderDepthPyramidKernels, value, nameof(m_OccluderDepthPyramidKernels));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/InstanceOcclusionCullingKernels.compute")]
        ComputeShader m_InstanceOcclusionCullingKernels;

        /// <summary>
        /// InstanceOcclusionCullingKernels compute shader reference
        /// </summary>
        public ComputeShader instanceOcclusionCullingKernels
        {
            get => m_InstanceOcclusionCullingKernels;
            set => this.SetValueAndNotify(ref m_InstanceOcclusionCullingKernels, value, nameof(m_InstanceOcclusionCullingKernels));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/OcclusionCullingDebug.compute")]
        ComputeShader m_OcclusionCullingDebugKernels;

        /// <summary>
        /// OcclusionCullingDebugKernels compute shader reference
        /// </summary>
        public ComputeShader occlusionCullingDebugKernels
        {
            get => m_OcclusionCullingDebugKernels;
            set => this.SetValueAndNotify(ref m_OcclusionCullingDebugKernels, value, nameof(m_OcclusionCullingDebugKernels));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/DebugOcclusionTest.shader")]
        Shader m_DebugOcclusionTestPS;

        /// <summary>
        /// DebugOcclusionTestPS pixel shader reference
        /// </summary>
        public Shader debugOcclusionTestPS
        {
            get => m_DebugOcclusionTestPS;
            set => this.SetValueAndNotify(ref m_DebugOcclusionTestPS, value, nameof(m_DebugOcclusionTestPS));
        }

        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/GPUDriven/DebugOccluder.shader")]
        Shader m_DebugOccluderPS;

        /// <summary>
        /// DebugOccluderPS pixel shader reference
        /// </summary>
        public Shader debugOccluderPS
        {
            get => m_DebugOccluderPS;
            set => this.SetValueAndNotify(ref m_DebugOccluderPS, value, nameof(m_DebugOccluderPS));
        }
    }
}