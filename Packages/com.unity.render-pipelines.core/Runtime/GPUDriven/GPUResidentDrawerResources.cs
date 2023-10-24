namespace UnityEngine.Rendering
{
    /// <summary>
    /// Class for tracking resources used by the GPUResidentDrawer
    /// </summary>
    [ReloadGroup]
    public class GPUResidentDrawerResources : RenderPipelineResources
    {
        /// <summary>
        /// Package path where the resources are located
        /// </summary>
        protected override string packagePath => "Packages/com.unity.render-pipelines.core/";

        /// <summary>
        /// InstanceDataBufferCopyKernels compute shader reference
        /// </summary>
        [Reload("Runtime/RenderPipelineResources/GPUDriven/InstanceDataBufferCopyKernels.compute")]
        public ComputeShader instanceDataBufferCopyKernels;

        /// <summary>
        /// InstanceDataBufferUploadKernels compute shader reference
        /// </summary>
        [Reload("Runtime/RenderPipelineResources/GPUDriven/InstanceDataBufferUploadKernels.compute")]
        public ComputeShader instanceDataBufferUploadKernels;

        /// <summary>
        /// InstanceTransformUpdateKernels compute shader reference
        /// </summary>
        [Reload("Runtime/RenderPipelineResources/GPUDriven/InstanceTransformUpdateKernels.compute")]
        public ComputeShader transformUpdaterKernels;
    }
}
