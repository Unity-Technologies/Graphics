using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Experimental.Rendering;
//using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    // CDF: Cumulative Distribution Function
    class ComputeCDF1D
    {
        static RTHandle m_Temp0;
        static RTHandle m_Temp1;
        static RTHandle m_CDF;
        static RTHandle m_InvCDF;

        public enum SumDirection
        {
            Vertical,
            Horizontal
        }

        public ComputeCDF1D()
        {
        }

        static uint _Idx = 0;

        static private void SaveTempImg(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\CDF_" + _Idx.ToString() + " .exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static public RTHandle ComputeCDF(RTHandle input, CommandBuffer cmd, SumDirection sumDirection, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            if (input == null)
            {
                return null;
            }

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader cdfStep = hdrp.renderPipelineResources.shaders.ComputeCDF1DCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
                format = GraphicsFormat.R32G32B32A32_SFloat;
            else
                format = sumFormat;

            int width  = input.rt.width;
            int height = input.rt.height;

            m_Temp0  = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            m_Temp1  = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            m_CDF    = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            m_InvCDF = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);

            uint iteration;
            if (sumDirection == SumDirection.Vertical)
            {
                cdfStep.DisableKeyword("HORIZONTAL");
                cdfStep.EnableKeyword("VERTICAL");
                iteration = (uint)Mathf.Log((float)height, 2.0f);
            }
            else
            {
                cdfStep.DisableKeyword("VERTICAL");
                cdfStep.EnableKeyword("HORIZONTAL");
                iteration = (uint)Mathf.Log((float)width, 2.0f);
            }

            // RGB to Greyscale
            int kernel = cdfStep.FindKernel("CSMainFirst");
            int numTilesX;
            int numTilesY;
            {
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Input,     input);
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Output,    m_Temp0);
                cmd.SetComputeIntParams   (cdfStep,         HDShaderIDs._Sizes,
                                           input.rt.width, input.rt.height, m_Temp0.rt.width, m_Temp0.rt.height);
                if (sumDirection == SumDirection.Horizontal)
                {
                    numTilesX = (m_Temp0.rt.width  + (8 - 1))/8;
                    numTilesY = (m_Temp0.rt.height + (8 - 1))/8;
                }
                else
                {
                    numTilesX = (m_Temp0.rt.width  + (8 - 1))/8;
                    numTilesY = (m_Temp0.rt.height + (8 - 1))/8;
                }
                cmd.DispatchCompute     (cdfStep, kernel, numTilesX, numTilesY, 1);
                cmd.RequestAsyncReadback(m_Temp0, SaveTempImg);
            }

            // Loop
            kernel = cdfStep.FindKernel("CSMain");
            RTHandle ping = m_Temp0;
            RTHandle pong = m_Temp1;
            for (uint i = 0; i < iteration; ++i)
            {
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Input,     ping);
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Output,    pong);
                cmd.SetComputeIntParams   (cdfStep,         HDShaderIDs._Sizes,
                                           ping.rt.width, input.rt.height, pong.rt.width, pong.rt.height);
                cmd.SetComputeIntParam    (cdfStep,         HDShaderIDs._Iteration, (int)Mathf.Pow(2.0f, (float)i));
                if (sumDirection == SumDirection.Horizontal)
                {
                    numTilesX = (pong.rt.width  + (8 - 1))/8;
                    numTilesY =  pong.rt.height;
                }
                else
                {
                    numTilesX =  pong.rt.width;
                    numTilesY = (pong.rt.height + (8 - 1))/8;
                }
                cmd.DispatchCompute     (cdfStep, kernel, numTilesX, numTilesY, 1);
                cmd.RequestAsyncReadback(pong, SaveTempImg);
                if (i == iteration - 1)
                {
                    m_CDF = pong;
                }
                CoreUtils.Swap(ref ping, ref pong);
            }

            return m_CDF;
        }
    }
}
