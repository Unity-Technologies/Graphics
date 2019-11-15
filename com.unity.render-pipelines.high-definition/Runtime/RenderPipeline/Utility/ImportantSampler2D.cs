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
        ComputeBuffer   m_GeneratedSamples;

        RTHandle m_InvCDFFull;
        RTHandle m_InvCDFRows;

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
                string path = @"C:\UProjects\PDFDensity_" + _Idx.ToString() + " .exr";
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
                string path = @"C:\UProjects\CDFFull_" + _Idx.ToString() + " .exr";
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
                string path = @"C:\UProjects\MinMaxFull_" + _Idx.ToString() + " .exr";
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
            cmd.RequestAsyncReadback(pdfDensity, SavePDFDensity);
            RTHandle cdfFull = ComputeCDF1D.ComputeCDF(pdfDensity,
                                                       cmd,
                                                       ComputeCDF1D.SumDirection.Horizontal,
                                                       Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(cdfFull, SaveCDFFull);
            RTHandle minMaxFull = ParallelOperation.ComputeOperation(cdfFull,
                                                    cmd,
                                                    ParallelOperation.Operation.MinMax,
                                                    ParallelOperation.Direction.Horizontal,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(minMaxFull, SaveMinMaxFull);

            RTHandle sumRows = ParallelOperation.ComputeOperation(pdfDensity,
                                                    cmd,
                                                    ParallelOperation.Operation.Sum,
                                                    ParallelOperation.Direction.Horizontal,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(sumRows, SaveCDFFull);
            RTHandle minMaxRows = ParallelOperation.ComputeOperation(sumRows,
                                                    cmd,
                                                    ParallelOperation.Operation.MinMax,
                                                    ParallelOperation.Direction.Vertical,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);

            _Idx = 0;
            Rescale(cdfFull, minMaxFull, ParallelOperation.Direction.Horizontal, cmd);
            Rescale(sumRows, minMaxRows, ParallelOperation.Direction.Vertical,   cmd);

            m_InvCDFFull = ComputeCDF1D.ComputeInverseCDF(cdfFull,
                                                          cmd,
                                                          ComputeCDF1D.SumDirection.Horizontal,
                                                          Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            m_InvCDFRows = ComputeCDF1D.ComputeInverseCDF(sumRows,
                                                          cmd,
                                                          ComputeCDF1D.SumDirection.Vertical,
                                                          Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);

            GenerateSamples(512, m_InvCDFRows, m_InvCDFFull, ParallelOperation.Direction.Horizontal, cmd);
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
                string path = @"C:\UProjects\Rescaled_" + _Idx.ToString() + " .exr";
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
                string path = @"C:\UProjects\Samples_" + _Idx.ToString() + " .exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        private void Rescale(RTHandle tex, RTHandle minMax, ParallelOperation.Direction direction, CommandBuffer cmd)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader rescale01 = hdrp.renderPipelineResources.shaders.Rescale01CS;

            rescale01.EnableKeyword("MINMAX");
            rescale01.EnableKeyword("READ_WRITE");
            string addon = "";
            if (direction == ParallelOperation.Direction.Horizontal)
            {
                addon += "H";
            }
            else
            {
                addon += "V";
            }

            int kernel = rescale01.FindKernel("CSMain" + addon);

            cmd.SetComputeTextureParam(rescale01, kernel, HDShaderIDs._Output, tex);
            cmd.SetComputeTextureParam(rescale01, kernel, HDShaderIDs._MinMax, minMax);
            cmd.SetComputeIntParams   (rescale01,         HDShaderIDs._Sizes,
                                       tex.rt.width, tex.rt.height, tex.rt.width, tex.rt.height);

            int numTilesX = (tex.rt.width  + (8 - 1))/8;
            int numTilesY = (tex.rt.height + (8 - 1))/8;

            cmd.DispatchCompute(rescale01, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(tex, SaveTempImg);
        }

        public void GenerateSamples(uint samplesCount, RTHandle sliceInvCDF, RTHandle fullInvCDF, ParallelOperation.Direction direction, CommandBuffer cmd)
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
        }
    }
}
