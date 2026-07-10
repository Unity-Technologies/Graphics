using System;
using UnityEngine.Rendering;


namespace UnityEngine.PathTracing.Core
{
    internal struct EnvironmentCDF
    {
        public GraphicsBuffer ConditionalBuffer;
        public GraphicsBuffer MarginalBuffer;
        public int ConditionalResolution;
        public int MarginalResolution;
    }

    internal class EnvironmentImportanceSampling : IDisposable
    {
        public EnvironmentImportanceSampling(ComputeShader shader)
        {
            // The marginal resolution should be at least twice the skybox side length (128 for the baker).
            // Because we sample from a discrete distribution but normalize against a continuous PDF,
            // quantization error introduces bias into the result. This bias vanishes as the marginal
            // resolution grows. When the resolution is too low, bright spots (such as the sun) get an
            // inflated PDF and are over-darkened, while dark spots end up too bright.
            _environmentCDF.MarginalResolution = 512;
            _environmentCDF.ConditionalResolution = _environmentCDF.MarginalResolution * 2;
            _environmentCDF.ConditionalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _environmentCDF.ConditionalResolution * _environmentCDF.MarginalResolution, sizeof(float));
            _environmentCDF.MarginalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _environmentCDF.MarginalResolution, sizeof(float));

            _shader = shader;
            if (_shader)
            {
                _computeConditionalKernel = _shader.FindKernel("ComputeConditional");
                _computeMarginalKernel = _shader.FindKernel("ComputeMarginal");
            }
        }

        public void ComputeCDFBuffers(CommandBuffer cmd, Texture cubemap)
        {
            cmd.SetComputeIntParam(_shader, "_ConditionalResolution", _environmentCDF.ConditionalResolution);
            cmd.SetComputeIntParam(_shader, "_MarginalResolution", _environmentCDF.MarginalResolution);

            cmd.SetComputeTextureParam(_shader, _computeConditionalKernel, "_EnvCubemap", cubemap);
            cmd.SetComputeBufferParam(_shader, _computeConditionalKernel, "_ConditionalBuffer", _environmentCDF.ConditionalBuffer);
            cmd.SetComputeBufferParam(_shader, _computeConditionalKernel, "_MarginalBuffer", _environmentCDF.MarginalBuffer);
            cmd.DispatchCompute(_shader, _computeConditionalKernel, 1, _environmentCDF.MarginalResolution, 1);

            cmd.SetComputeBufferParam(_shader, _computeMarginalKernel, "_MarginalBuffer", _environmentCDF.MarginalBuffer);
            cmd.DispatchCompute(_shader, _computeMarginalKernel, 1, 1, 1);
        }

        internal EnvironmentCDF GetSkyboxCDF()
        {
            return _environmentCDF;
        }

        public void Dispose()
        {
            _environmentCDF.ConditionalBuffer.Dispose();
            _environmentCDF.MarginalBuffer.Dispose();
        }

        private readonly ComputeShader _shader;
        private readonly int _computeConditionalKernel;
        private readonly int _computeMarginalKernel;
        private readonly EnvironmentCDF _environmentCDF;
    }
}
