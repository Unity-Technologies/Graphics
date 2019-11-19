using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Experimental.Rendering;
//using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    class ImportantSampler2D
    {
        Texture2D       m_CFDinv; // Cumulative Function Distribution Inverse

        RTHandle m_InvCDFFull;
        RTHandle m_InvCDFRows;
        RTHandle m_OutDebug;

        public ImportantSampler2D()
        {
        }

        static private void SavePDFDensity(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\PDFDensity_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static private void SaveCDFFull(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\CDFFull_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static private void SaveMinMaxFull(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\MinMaxFull_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static public int _Idx = 0;

        static private void SaveTempImg(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\Rescaled_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static private void SaveSamples(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\Samples_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static private void SaveDebug(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\Debug_" + _Idx.ToString() + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        public void Init(RTHandle pdfDensity, CommandBuffer cmd)
        {
            ParallelOperation._Idx = 0;
            _Idx = 0;
            cmd.RequestAsyncReadback(pdfDensity, SavePDFDensity);

            // Rescale pdf between 0 & 1
            RTHandle pdfCopy = RTHandles.Alloc(pdfDensity.rt.width, pdfDensity.rt.height, colorFormat: pdfDensity.rt.graphicsFormat, enableRandomWrite: true);
            cmd.CopyTexture(pdfDensity, pdfCopy);

            RTHandle minMaxFull0 = ParallelOperation.ComputeOperation(
                                    pdfCopy,
                                    cmd,
                                    ParallelOperation.Operation.MinMax,
                                    ParallelOperation.Direction.Horizontal,
                                    2,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            RTHandle minMaxFull1 = ParallelOperation.ComputeOperation(
                                    minMaxFull0,
                                    cmd,
                                    ParallelOperation.Operation.MinMax,
                                    ParallelOperation.Direction.Vertical,
                                    2,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            Rescale(pdfCopy, minMaxFull0, ParallelOperation.Direction.Horizontal, cmd, true);

            // Compute the CDF of the rows of the rescaled PDF
            RTHandle cdfFull = ComputeCDF1D.ComputeCDF(
                                    pdfCopy,
                                    cmd,
                                    ComputeCDF1D.SumDirection.Horizontal,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(cdfFull, SaveCDFFull);

            // Rescale between 0 & 1 the rows_cdf: to be inverted in UV
            RTHandle minMaxFull = ParallelOperation.ComputeOperation(
                                    cdfFull,
                                    cmd,
                                    ParallelOperation.Operation.MinMax,
                                    ParallelOperation.Direction.Horizontal,
                                    2,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(minMaxFull, SaveMinMaxFull);
            Rescale(cdfFull, minMaxFull, ParallelOperation.Direction.Horizontal, cmd);

            // Sum of rows
            RTHandle sumRows = ParallelOperation.ComputeOperation(pdfCopy,
                                                    cmd,
                                                    ParallelOperation.Operation.Sum,
                                                    ParallelOperation.Direction.Horizontal,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(sumRows, SaveCDFFull);
            // Minmax of rows
            RTHandle minMaxRows = ParallelOperation.ComputeOperation(sumRows,
                                                    cmd,
                                                    ParallelOperation.Operation.MinMax,
                                                    ParallelOperation.Direction.Vertical,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(minMaxRows, SaveMinMaxFull);
            // Rescale sum of rows
            Rescale(sumRows, minMaxRows, ParallelOperation.Direction.Vertical, cmd);

            // Compute inverse of CDFs
            m_InvCDFFull = ComputeCDF1D.ComputeInverseCDF(cdfFull,
                                                          cmd,
                                                          ComputeCDF1D.SumDirection.Horizontal,
                                                          Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            m_InvCDFRows = ComputeCDF1D.ComputeInverseCDF(sumRows,
                                                          cmd,
                                                          ComputeCDF1D.SumDirection.Vertical,
                                                          Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);

            // Generate sample from invCDFs
            RTHandle samples = GenerateSamples(4096, m_InvCDFRows, m_InvCDFFull, ParallelOperation.Direction.Horizontal, cmd);

            //
            m_OutDebug = RTHandles.Alloc(pdfDensity.rt.width, pdfDensity.rt.height, colorFormat: pdfDensity.rt.graphicsFormat, enableRandomWrite: true);
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader outputDebug2D = hdrp.renderPipelineResources.shaders.OutputDebugCS;

            int kernel = outputDebug2D.FindKernel("CSMain");

            cmd.CopyTexture(pdfDensity, m_OutDebug);

            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Output,  m_OutDebug);
            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Samples, samples);
            cmd.SetComputeIntParams   (outputDebug2D, HDShaderIDs._Sizes,
                                       pdfDensity.rt.width, pdfDensity.rt.height, samples.rt.width, 1);

            int numTilesX = (samples.rt.width  + (8 - 1))/8;
            cmd.DispatchCompute(outputDebug2D, kernel, numTilesX, 1, 1);
            cmd.RequestAsyncReadback(m_OutDebug, SaveDebug);
            //
        }

        private void Rescale(RTHandle tex, RTHandle minMax, ParallelOperation.Direction direction, CommandBuffer cmd, bool single = false)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader rescale01 = hdrp.renderPipelineResources.shaders.Rescale01CS;

            rescale01.EnableKeyword("MINMAX");
            rescale01.EnableKeyword("READ_WRITE");
            string addon0 = "";
            if (single)
            {
                addon0 += "S";
            }
            else if (direction == ParallelOperation.Direction.Horizontal)
            {
                addon0 += "H";
            }
            else
            {
                addon0 += "V";
            }

            int kernel = rescale01.FindKernel("CSMain" + addon0);

            cmd.SetComputeTextureParam(rescale01, kernel, HDShaderIDs._Output, tex);
            cmd.SetComputeTextureParam(rescale01, kernel, HDShaderIDs._MinMax, minMax);
            cmd.SetComputeIntParams   (rescale01,         HDShaderIDs._Sizes,
                                       tex.rt.width, tex.rt.height, tex.rt.width, tex.rt.height);

            int numTilesX = (tex.rt.width  + (8 - 1))/8;
            int numTilesY = (tex.rt.height + (8 - 1))/8;

            cmd.DispatchCompute(rescale01, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(tex, SaveTempImg);
        }

        public RTHandle GenerateSamples(uint samplesCount, RTHandle sliceInvCDF, RTHandle fullInvCDF, ParallelOperation.Direction direction, CommandBuffer cmd)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader importanceSample2D = hdrp.renderPipelineResources.shaders.ImportanceSample2DCS;

            string addon = "";
            if (direction == ParallelOperation.Direction.Horizontal)
            {
                addon += "H";
            }
            else
            {
                addon += "V";
            }

            RTHandle samples = RTHandles.Alloc((int)samplesCount, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);

            int kernel = importanceSample2D.FindKernel("CSMain" + addon);

            cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._SliceInvCDF, sliceInvCDF);
            cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._InvCDF,      fullInvCDF);
            cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._Output,      samples);
            cmd.SetComputeIntParams   (importanceSample2D, HDShaderIDs._Sizes,
                                       fullInvCDF.rt.width, fullInvCDF.rt.height, (int)samplesCount, 1);

            int numTilesX = (samples.rt.width + (8 - 1))/8;

            cmd.DispatchCompute(importanceSample2D, kernel, numTilesX, 1, 1);
            cmd.RequestAsyncReadback(samples, SaveSamples);

            return samples;
        }
    }
}
