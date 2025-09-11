using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.PostProcessing
{
    internal class ProbePostProcessor
    {
        private ComputeShader _computeShader;
        private int _convolveRadianceToIrradianceKernel;
        private int _convertToUnityFormatKernel;
        private int _addSphericalHarmonicsL2Kernel;
        private int _scaleSphericalHarmonicsL2Kernel;
        private int _windowSphericalHarmonicsL2Kernel;

        /// <summary>
        /// Prepares the probe post processor for use.
        /// Expects to be given the "ProbePostProcessing.compute" shader.
        /// </summary>
        /// <param name="computeShader">The compute shader containing post processing kernels. Should be the "ProbePostProcessing.compute" shader.</param>
        public void Prepare(ComputeShader computeShader)
        {
            _computeShader = computeShader;
            Debug.Assert(_computeShader != null);
            _convolveRadianceToIrradianceKernel = _computeShader.FindKernel("ConvolveRadianceToIrradiance");
            Debug.Assert(_convolveRadianceToIrradianceKernel != -1);
            _convertToUnityFormatKernel = _computeShader.FindKernel("ConvertToUnityFormat");
            Debug.Assert(_convertToUnityFormatKernel != -1);
            _addSphericalHarmonicsL2Kernel = _computeShader.FindKernel("AddSphericalHarmonicsL2");
            Debug.Assert(_addSphericalHarmonicsL2Kernel != -1);
            _scaleSphericalHarmonicsL2Kernel = _computeShader.FindKernel("ScaleSphericalHarmonicsL2");
            Debug.Assert(_scaleSphericalHarmonicsL2Kernel != -1);
            _windowSphericalHarmonicsL2Kernel = _computeShader.FindKernel("WindowSphericalHarmonicsL2");
            Debug.Assert(_windowSphericalHarmonicsL2Kernel != -1);
        }

        public void ConvolveRadianceToIrradiance(CommandBuffer cmd, GraphicsBuffer inRadianceBuffer, GraphicsBuffer outIrradianceBuffer, uint inputOffset, uint outputOffset, uint probeCount)
        {
            Debug.Assert(_computeShader != null);
            Debug.Assert(inRadianceBuffer.count == outIrradianceBuffer.count);
            Debug.Assert(inRadianceBuffer.stride == sizeof(float));

            cmd.SetComputeBufferParam(_computeShader, _convolveRadianceToIrradianceKernel, Shader.PropertyToID("g_PrimaryInputShl2"), inRadianceBuffer);
            cmd.SetComputeBufferParam(_computeShader, _convolveRadianceToIrradianceKernel, Shader.PropertyToID("g_OutputShl2"), outIrradianceBuffer);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_PrimaryInputOffset"), (int)inputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_OutputOffset"), (int)outputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_ProbeCount"), (int)probeCount);

            _computeShader.GetKernelThreadGroupSizes(_convolveRadianceToIrradianceKernel, out uint threadGroupsX, out uint _, out uint _);
            cmd.DispatchCompute(_computeShader, _convolveRadianceToIrradianceKernel, Mathf.CeilToInt(probeCount / (float)threadGroupsX), 1, 1);
        }

        public void ConvertToUnityFormat(CommandBuffer cmd, GraphicsBuffer inIrradianceBuffer, GraphicsBuffer outIrradianceBuffer, uint inputOffset, uint outputOffset, uint probeCount)
        {
            Debug.Assert(_computeShader != null);
            Debug.Assert(inIrradianceBuffer.count == outIrradianceBuffer.count);
            Debug.Assert(inIrradianceBuffer.stride == sizeof(float));

            cmd.SetComputeBufferParam(_computeShader, _convertToUnityFormatKernel, Shader.PropertyToID("g_PrimaryInputShl2"), inIrradianceBuffer);
            cmd.SetComputeBufferParam(_computeShader, _convertToUnityFormatKernel, Shader.PropertyToID("g_OutputShl2"), outIrradianceBuffer);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_PrimaryInputOffset"), (int)inputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_OutputOffset"), (int)outputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_ProbeCount"), (int)probeCount);

            _computeShader.GetKernelThreadGroupSizes(_convertToUnityFormatKernel, out uint threadGroupsX, out _, out _);
            cmd.DispatchCompute(_computeShader, _convertToUnityFormatKernel, Mathf.CeilToInt(probeCount / (float)threadGroupsX), 1, 1);
        }

        internal void AddSphericalHarmonicsL2(CommandBuffer cmd, GraphicsBuffer inA, GraphicsBuffer inB, GraphicsBuffer outSum, uint inputOffsetA, uint inputOffsetB, uint outputOffset, uint probeCount)
        {
            Debug.Assert(_computeShader != null);
            Debug.Assert(inA.count == inB.count);
            Debug.Assert(inA.count == outSum.count);
            Debug.Assert(inA.stride == sizeof(float));

            cmd.SetComputeBufferParam(_computeShader, _addSphericalHarmonicsL2Kernel, Shader.PropertyToID("g_PrimaryInputShl2"), inA);
            cmd.SetComputeBufferParam(_computeShader, _addSphericalHarmonicsL2Kernel, Shader.PropertyToID("g_SecondaryInputShl2"), inB);
            cmd.SetComputeBufferParam(_computeShader, _addSphericalHarmonicsL2Kernel, Shader.PropertyToID("g_OutputShl2"), outSum);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_PrimaryInputOffset"), (int)inputOffsetA);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_SecondaryInputOffset"), (int)inputOffsetB);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_OutputOffset"), (int)outputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_ProbeCount"), (int)probeCount);

            _computeShader.GetKernelThreadGroupSizes(_addSphericalHarmonicsL2Kernel, out uint threadGroupsX, out _, out _);
            cmd.DispatchCompute(_computeShader, _addSphericalHarmonicsL2Kernel, Mathf.CeilToInt(probeCount / (float)threadGroupsX), 1, 1);
        }

        internal void ScaleSphericalHarmonicsL2(CommandBuffer cmd, GraphicsBuffer input, GraphicsBuffer outScaled, uint inputOffset, uint outputOffset, uint probeCount, float scale)
        {
            Debug.Assert(_computeShader != null);
            Debug.Assert(input.stride == sizeof(float));

            cmd.SetComputeBufferParam(_computeShader, _scaleSphericalHarmonicsL2Kernel, Shader.PropertyToID("g_PrimaryInputShl2"), input);
            cmd.SetComputeBufferParam(_computeShader, _scaleSphericalHarmonicsL2Kernel, Shader.PropertyToID("g_OutputShl2"), outScaled);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_PrimaryInputOffset"), (int)inputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_OutputOffset"), (int)outputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_ProbeCount"), (int)probeCount);
            cmd.SetComputeFloatParam(_computeShader, Shader.PropertyToID("g_Scale"), scale);

            _computeShader.GetKernelThreadGroupSizes(_scaleSphericalHarmonicsL2Kernel, out uint threadGroupsX, out _, out _);
            cmd.DispatchCompute(_computeShader, _scaleSphericalHarmonicsL2Kernel, Mathf.CeilToInt(probeCount / (float)threadGroupsX), 1, 1);
        }

        internal void WindowSphericalHarmonicsL2(CommandBuffer cmd, GraphicsBuffer input, GraphicsBuffer outWindowed, uint inputOffset, uint outputOffset, uint probeCount)
        {
            Debug.Assert(_computeShader != null);
            Debug.Assert(input.stride == sizeof(float));

            cmd.SetComputeBufferParam(_computeShader, _windowSphericalHarmonicsL2Kernel, Shader.PropertyToID("g_PrimaryInputShl2"), input);
            cmd.SetComputeBufferParam(_computeShader, _windowSphericalHarmonicsL2Kernel, Shader.PropertyToID("g_OutputShl2"), outWindowed);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_PrimaryInputOffset"), (int)inputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_OutputOffset"), (int)outputOffset);
            cmd.SetComputeIntParam(_computeShader, Shader.PropertyToID("g_ProbeCount"), (int)probeCount);

            _computeShader.GetKernelThreadGroupSizes(_windowSphericalHarmonicsL2Kernel, out uint threadGroupsX, out _, out _);
            cmd.DispatchCompute(_computeShader, _windowSphericalHarmonicsL2Kernel, Mathf.CeilToInt(probeCount / (float)threadGroupsX), 1, 1);
        }
    }
}
