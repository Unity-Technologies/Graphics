#define DUMP_IMAGE

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    static class ImportanceSampler2D
    {

#if DUMP_IMAGE
        static private void Default(AsyncGPUReadbackRequest request, string name, GraphicsFormat format)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, format, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
                string path = @"C:\UProjects\" + name + ".exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
            }
        }
#endif

        internal static GraphicsFormat GetFormat(uint channelCount, bool isFullPrecision = false)
        {
            if (isFullPrecision)
            {
                if (channelCount == 1)
                    return GraphicsFormat.R32_SFloat;
                else if (channelCount == 2)
                    return GraphicsFormat.R32G32_SFloat;
                else if (channelCount == 4)
                    return GraphicsFormat.R32G32B32A32_SFloat;
                else
                    return GraphicsFormat.None;
            }
            else
            {
                if (channelCount == 1)
                    return GraphicsFormat.R16_SFloat;
                else if (channelCount == 2)
                    return GraphicsFormat.R16G16_SFloat;
                else if (channelCount == 4)
                    return GraphicsFormat.R16G16B16A16_SFloat;
                else
                    return GraphicsFormat.None;
            }
        }

        /// <summary>
        /// Build Mariginal textures for 2D texture 'density'
        /// </summary>
        /// <param name="marginal">A Columns marginal texture</param>
        /// <param name="conditionalMarginal">Full resolution marginal texture</param>
        /// <param name="density">Density, not normalized PDF, must be single channel</param>
        /// <param name="cmd">Command Buffer</param>
        /// <param name="dumpFile"></param>
        /// <param name="idx"></param>
        public static void GenerateMarginals(
                                out RTHandle marginal, out RTHandle conditionalMarginal,
                                RTHandle density,
                                CommandBuffer cmd,
                                bool dumpFile, int idx)
        {
            int width   = density.rt.width;
            int height  = density.rt.height;

            marginal            = null;
            conditionalMarginal = null;

            Debug.Assert(HDUtils.GetFormatChannelsCount(density.rt.graphicsFormat) == 1);

            bool isFullPrecision = HDUtils.GetFormatMaxPrecisionBits(density.rt.graphicsFormat) == 32;

            GraphicsFormat format1 = GetFormat(1, isFullPrecision);
            GraphicsFormat format2 = GetFormat(2, isFullPrecision);
            GraphicsFormat format4 = GetFormat(4, isFullPrecision);

#if DUMP_IMAGE
            string strName = string.Format("{0}S{1}M{1}", idx, 0, 0);
#endif

            RTHandle pdfCopy = RTHandles.Alloc(density.rt.width, density.rt.height, colorFormat: format1, enableRandomWrite: true, useMipMap: false, autoGenerateMips: false);
            RTHandleDeleter.ScheduleRelease(pdfCopy);
            cmd.CopyTexture(density, 0, 0, pdfCopy, 0, 0);
            if (dumpFile)
                cmd.RequestAsyncReadback(pdfCopy, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "02_Input" + strName, pdfCopy.rt.graphicsFormat);
                });
            // 3. CDF Rows
            RTHandle cdfFull = GPUScan.ComputeOperation(pdfCopy, cmd, GPUScan.Operation.Add, GPUScan.Direction.Horizontal, format1);
            RTHandleDeleter.ScheduleRelease(cdfFull);
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "03_CDFRows" + strName, cdfFull.rt.graphicsFormat);
                });
            // 4. CDF L
            RTHandle sumRows = RTHandles.Alloc(1, height, colorFormat: format1, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(sumRows);
            cmd.CopyTexture(cdfFull, 0, 0, width - 1, 0, 1, height, sumRows, 0, 0, 0, 0);
            if (dumpFile)
                cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "04.1_SumOfRows" + strName, sumRows.rt.graphicsFormat);
                });
            RTHandle minMaxSumRows = GPUScan.ComputeOperation(sumRows, cmd, GPUScan.Operation.MinMax, GPUScan.Direction.Vertical, format2);
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxSumRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "04.2_MinMaxSumRows" + strName, minMaxSumRows.rt.graphicsFormat);
                });
            RTHandleDeleter.ScheduleRelease(minMaxSumRows);
            Rescale(sumRows, minMaxSumRows, GPUScan.Direction.Vertical, cmd, true);
            if (dumpFile)
                cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "04.3_SumOfRowsRescaled" + strName, sumRows.rt.graphicsFormat);
                });
            RTHandle cdfL = GPUScan.ComputeOperation(sumRows, cmd, GPUScan.Operation.Add, GPUScan.Direction.Vertical);
            RTHandleDeleter.ScheduleRelease(cdfL);
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfL, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "04.4_CDFL" + strName, cdfL.rt.graphicsFormat);
                });
            // 5. Min Max Rows
            RTHandle minMaxRows = GPUScan.ComputeOperation(cdfFull, cmd, GPUScan.Operation.MinMax, GPUScan.Direction.Horizontal);
            RTHandleDeleter.ScheduleRelease(minMaxRows);
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "05_MinMaxRows" + strName, minMaxRows.rt.graphicsFormat);
                });
            // 6. Min Max L
            RTHandle minMaxCDFL = GPUScan.ComputeOperation(cdfL, cmd, GPUScan.Operation.MinMax, GPUScan.Direction.Vertical);
            RTHandleDeleter.ScheduleRelease(minMaxCDFL);
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxCDFL, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "06_MinMaxL" + strName, minMaxCDFL.rt.graphicsFormat);
                });
            // 7. Rescale CDF Rows
            Rescale(cdfFull, minMaxRows, GPUScan.Direction.Horizontal, cmd);
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "07_CDFRowsRescaled" + strName, cdfFull.rt.graphicsFormat);
                });
            // 8. Rescale CDF L
            Rescale(cdfL, minMaxCDFL, GPUScan.Direction.Vertical, cmd, true);
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfL, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "08_CDFLRescaled" + strName, minMaxCDFL.rt.graphicsFormat);
                });
            // 9. Inv CDF Rows
            conditionalMarginal = InverseCDF1D.ComputeInverseCDF(cdfFull, pdfCopy, cmd, InverseCDF1D.SumDirection.Horizontal, format4);
            GraphicsFormat formatfff = conditionalMarginal.rt.graphicsFormat;
            if (dumpFile)
                cmd.RequestAsyncReadback(conditionalMarginal, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "09_ConditionalMarginalRows" + strName, formatfff);
                });
            // 10. Inv CDF L
            marginal = InverseCDF1D.ComputeInverseCDF(cdfL,    pdfCopy, cmd, InverseCDF1D.SumDirection.Vertical, format1);
            GraphicsFormat formatffff = marginal.rt.graphicsFormat;
            if (dumpFile)
                cmd.RequestAsyncReadback(marginal, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "10_MarginalL" + strName, formatffff);
                });
            // 11. Generate Samples
            uint samplesCount = 4096;
            RTHandle samples = GenerateSamples(samplesCount, marginal, conditionalMarginal, GPUScan.Direction.Horizontal, cmd, true);
            RTHandleDeleter.ScheduleRelease(samples);
            if (dumpFile)
                cmd.RequestAsyncReadback(samples, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "11_Samples" + strName, samples.rt.graphicsFormat);
                });
            // 12. Generate Output
            RTHandle pdfCopyRGBA = RTHandles.Alloc(density.rt.width, density.rt.height, colorFormat: format4, enableRandomWrite: true, useMipMap: false, autoGenerateMips: false);
            RTHandleDeleter.ScheduleRelease(pdfCopyRGBA);
            RTHandle blackRT = RTHandles.Alloc(Texture2D.blackTexture);
            RTHandleDeleter.ScheduleRelease(blackRT);
            GPUArithmetic.ComputeOperation(pdfCopyRGBA, pdfCopy, blackRT, cmd, GPUArithmetic.Operation.Add);
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader outputDebug2D = hdrp.renderPipelineResources.shaders.outputDebugCS;
            int kernel = outputDebug2D.FindKernel("CSMain");
            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._PDF,      density);
            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Output,   pdfCopyRGBA);
            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Samples,  samples);
            cmd.SetComputeIntParams   (outputDebug2D, HDShaderIDs._Sizes,
                                       pdfCopyRGBA.rt.width, pdfCopyRGBA.rt.height, samples.rt.width, 1);
            int numTilesX = (samples.rt.width + (8 - 1))/8;
            cmd.DispatchCompute(outputDebug2D, kernel, numTilesX, 1, 1);
            if (dumpFile)
                cmd.RequestAsyncReadback(pdfCopyRGBA, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "12_Debug" + strName, pdfCopyRGBA.rt.graphicsFormat);
                });
        }

        private static void Rescale(RTHandle tex, RTHandle minMax, GPUScan.Direction direction, CommandBuffer cmd, bool single = false)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader rescale01 = hdrp.renderPipelineResources.shaders.rescale01CS;

            rescale01.EnableKeyword("READ_WRITE");
            string addon0 = "";
            if (single)
            {
                addon0 += "S";
            }
            else if (direction == GPUScan.Direction.Horizontal)
            {
                addon0 += "H";
            }
            else // if (direction == GPUOperation.Direction.Vertical)
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
        }

        public static RTHandle GenerateSamples(uint samplesCount, RTHandle marginal, RTHandle conditionalMarginal, GPUScan.Direction direction, CommandBuffer cmd, bool hemiSphere = false)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader importanceSample2D = hdrp.renderPipelineResources.shaders.importanceSample2DCS;

            string addon = "";
            if (hemiSphere)
                addon += "Hemi";

            if (direction == GPUScan.Direction.Horizontal)
            {
                addon += "H";
            }
            else
            {
                addon += "V";
            }

            RTHandle samples = RTHandles.Alloc((int)samplesCount, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);

            int kernel = importanceSample2D.FindKernel("CSMain" + addon);

            int numTilesX = (samples.rt.width + (8 - 1))/8;
            if (cmd != null)
            {
                cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._Marginal,               marginal);
                cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._ConditionalMarginal,    conditionalMarginal);
                cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._Output,                 samples);
                if (direction == GPUScan.Direction.Horizontal)
                    cmd.SetComputeFloatParams(importanceSample2D,         HDShaderIDs._Sizes,
                                                conditionalMarginal.rt.height, 1.0f/conditionalMarginal.rt.height, 0.5f/conditionalMarginal.rt.height, samplesCount);
                else
                    cmd.SetComputeFloatParams(importanceSample2D, HDShaderIDs._Sizes,
                                                conditionalMarginal.rt.width,  1.0f/conditionalMarginal.rt.width,  0.5f/conditionalMarginal.rt.width,  samplesCount);
                cmd.DispatchCompute(importanceSample2D, kernel, numTilesX, 1, 1);
            }
            else
            {
                importanceSample2D.SetTexture(kernel, HDShaderIDs._Marginal,            marginal);
                importanceSample2D.SetTexture(kernel, HDShaderIDs._ConditionalMarginal, conditionalMarginal);
                importanceSample2D.SetTexture(kernel, HDShaderIDs._Output,              samples);
                if (direction == GPUScan.Direction.Horizontal)
                    importanceSample2D.SetFloats(HDShaderIDs._Sizes,
                                                conditionalMarginal.rt.height, 1.0f/conditionalMarginal.rt.height, 0.5f/conditionalMarginal.rt.height, samplesCount);
                else
                    importanceSample2D.SetFloats(HDShaderIDs._Sizes,
                                                conditionalMarginal.rt.width,  1.0f/conditionalMarginal.rt.width,  0.5f/conditionalMarginal.rt.width,  samplesCount);
                importanceSample2D.Dispatch(kernel, numTilesX, 1, 1);
            }

            return samples;
        }
    }
}
