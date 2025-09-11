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
            _environmentCDF.MarginalResolution = 64;
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
            cmd.SetComputeIntParam(_shader, "_PathTracingSkyConditionalResolution", _environmentCDF.ConditionalResolution);
            cmd.SetComputeIntParam(_shader, "_PathTracingSkyMarginalResolution", _environmentCDF.MarginalResolution);

            cmd.SetComputeTextureParam(_shader, _computeConditionalKernel, "_PathTracingSkybox", cubemap);
            cmd.SetComputeBufferParam(_shader, _computeConditionalKernel, "_PathTracingSkyConditionalBuffer", _environmentCDF.ConditionalBuffer);
            cmd.SetComputeBufferParam(_shader, _computeConditionalKernel, "_PathTracingSkyMarginalBuffer", _environmentCDF.MarginalBuffer);
            cmd.DispatchCompute(_shader, _computeConditionalKernel, 1, _environmentCDF.MarginalResolution, 1);

            cmd.SetComputeBufferParam(_shader, _computeMarginalKernel, "_PathTracingSkyMarginalBuffer", _environmentCDF.MarginalBuffer);
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
