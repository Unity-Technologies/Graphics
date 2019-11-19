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
                string path = @"C:\UProjects\CDF_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static private void SaveInvCDF(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\InvCDF_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static public RTHandle ComputeCDF(RTHandle input, CommandBuffer cmd, SumDirection direction, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            RTHandle temp0;
            RTHandle temp1;
            RTHandle cdf;

            if (input == null)
            {
                return null;
            }

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader cdfStep = hdrp.renderPipelineResources.shaders.CDF1DCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
                format = GraphicsFormat.R32G32B32A32_SFloat;
            else
                format = sumFormat;

            int width  = input.rt.width;
            int height = input.rt.height;

            temp0 = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            temp1 = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            cdf   = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);

            uint iteration;
            string addon = "";
            if (direction == SumDirection.Vertical)
            {
                addon = "V";
                iteration = (uint)Mathf.Log((float)height, 2.0f);
            }
            else
            {
                addon = "H";
                iteration = (uint)Mathf.Log((float)width,  2.0f);
            }

            // RGB to Greyscale
            int kernel = cdfStep.FindKernel("CSMainFirst" + addon);
            cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Input,     input);
            cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Output,    temp0);
            cmd.SetComputeIntParams   (cdfStep,         HDShaderIDs._Sizes,
                                       input.rt.width, input.rt.height, temp0.rt.width, temp0.rt.height);
            int numTilesX;
            int numTilesY;
            if (direction == SumDirection.Horizontal)
            {
                numTilesX = (temp0.rt.width  + (8 - 1))/8;
                numTilesY =  temp0.rt.height;
            }
            else
            {
                numTilesX =  temp0.rt.width;
                numTilesY = (temp0.rt.height + (8 - 1))/8;
            }
            cmd.DispatchCompute     (cdfStep, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(temp0, SaveTempImg);

            // Loop
            kernel = cdfStep.FindKernel("CSMain" + addon);
            RTHandle ping = temp0;
            RTHandle pong = temp1;
            for (uint i = 0; i < iteration; ++i)
            {
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Input,     ping);
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Output,    pong);
                cmd.SetComputeIntParams   (cdfStep,         HDShaderIDs._Sizes,
                                           ping.rt.width, input.rt.height, pong.rt.width, pong.rt.height);
                cmd.SetComputeIntParam    (cdfStep,         HDShaderIDs._Iteration, (int)Mathf.Pow(2.0f, (float)i));
                if (direction == SumDirection.Horizontal)
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
                    cdf = pong;
                }
                CoreUtils.Swap(ref ping, ref pong);
            }

            return cdf;
        }

        static public RTHandle ComputeInverseCDF(RTHandle cdf, CommandBuffer cmd, SumDirection direction, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            if (cdf == null)
            {
                return null;
            }

            RTHandle invCDF;

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader invCDFCS = hdrp.renderPipelineResources.shaders.InverseCDF1DCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
                format = GraphicsFormat.R32G32B32A32_SFloat;
            else
                format = sumFormat;

            int width  = cdf.rt.width;
            int height = cdf.rt.height;

            invCDF = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);

            string addon = "";
            if (direction == SumDirection.Vertical)
            {
                addon = "V";
            }
            else
            {
                addon = "H";
            }

            // RGB to Greyscale
            int kernel = invCDFCS.FindKernel("CSMain" + addon);
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._Input,  cdf);
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._Output, invCDF);
            cmd.SetComputeIntParams   (invCDFCS,         HDShaderIDs._Sizes,
                                        cdf.rt.width, cdf.rt.height, invCDF.rt.width, invCDF.rt.height);
            int numTilesX = (invCDF.rt.width  + (8 - 1))/8;
            int numTilesY = (invCDF.rt.height + (8 - 1))/8;
            cmd.DispatchCompute     (invCDFCS, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(invCDF, SaveInvCDF);

            return cdf;
        }
    }
}
