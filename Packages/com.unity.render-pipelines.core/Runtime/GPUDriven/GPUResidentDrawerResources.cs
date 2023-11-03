using System;
using System.ComponentModel;

namespace UnityEngine.Rendering
{
    /// <summary> Class for tracking resources used by the GPUResidentDrawer </summary>
    [Serializable]
    [HideInInspector]
    [Category("Resources/GPU Resident Drawers")]
    [SupportedOnRenderPipeline]
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
    }
}