#define DUMP_IMAGE

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    static class ImportanceSampler2D
    {
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
        public static void GenerateMarginals(
                                out RTHandle marginal, out RTHandle conditionalMarginal,
                                RTHandle density,
                                CommandBuffer cmd)
        {
            int width   = density.rt.width;
            int height  = density.rt.height;

            Debug.Assert(HDUtils.GetFormatChannelsCount(density.rt.graphicsFormat) == 1);

            bool isFullPrecision = HDUtils.GetFormatMaxPrecisionBits(density.rt.graphicsFormat) == 32;

            GraphicsFormat format1 = GetFormat(1, isFullPrecision);
            GraphicsFormat format2 = GetFormat(2, isFullPrecision);
            GraphicsFormat format4 = GetFormat(4, isFullPrecision);

            // 1. CDF of Rows (density where each rows is a CDF)
            RTHandle cdfFull = GPUScan.ComputeOperation(density, cmd, GPUScan.Operation.Add, GPUScan.Direction.Horizontal, format1);
            RTHandleDeleter.ScheduleRelease(cdfFull);
            // 2. CDF L (L: CDF of {Sum of Rows})
            RTHandle sumRows = RTHandles.Alloc(1, height, colorFormat: format1, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(sumRows);
            // Last columns contains the data we want
            cmd.CopyTexture(cdfFull, 0, 0, width - 1, 0, 1, height, sumRows, 0, 0, 0, 0);
            // Pre-Normalize to avoid overflow
            RTHandle minMaxSumRows = GPUScan.ComputeOperation(sumRows, cmd, GPUScan.Operation.MinMax, GPUScan.Direction.Vertical, format2);
            RTHandleDeleter.ScheduleRelease(minMaxSumRows);
            Rescale01(sumRows, minMaxSumRows, GPUScan.Direction.Vertical, cmd, true);
            RTHandle cdfL = GPUScan.ComputeOperation(sumRows, cmd, GPUScan.Operation.Add, GPUScan.Direction.Vertical);
            RTHandleDeleter.ScheduleRelease(cdfL);
            // 3. Min Max of each Rows
            RTHandle minMaxRows = GPUScan.ComputeOperation(cdfFull, cmd, GPUScan.Operation.MinMax, GPUScan.Direction.Horizontal);
            RTHandleDeleter.ScheduleRelease(minMaxRows);
            // 4. Min Max of L
            RTHandle minMaxCDFL = GPUScan.ComputeOperation(cdfL, cmd, GPUScan.Operation.MinMax, GPUScan.Direction.Vertical);
            RTHandleDeleter.ScheduleRelease(minMaxCDFL);
            // 5. Rescale CDF Rows (to 01)
            Rescale01(cdfFull, minMaxRows, GPUScan.Direction.Horizontal, cmd);
            // 6. Rescale CDF L (to 01)
            Rescale01(cdfL, minMaxCDFL, GPUScan.Direction.Vertical, cmd, true);
            // 7. Inv CDF Rows
            conditionalMarginal = InverseCDF1D.ComputeInverseCDF(cdfFull, density, cmd, InverseCDF1D.SumDirection.Horizontal, format4);
            // 8. Inv CDF L
            marginal            = InverseCDF1D.ComputeInverseCDF(cdfL, density, cmd, InverseCDF1D.SumDirection.Vertical, format1);
        }

        /// <summary>
        /// Helper to Rescale a RenderTarget between 0 & 1, with a Given MinMax
        /// </summary>
        /// <param name="tex">Render Targer Handle</param>
        /// <param name="minMax">Renter Target Handle which contains the MinMax</param>
        /// <param name="direction">Direction: {Vertical, Horizontal}</param>
        /// <param name="cmd">Constant Buffer</param>
        /// <param name="single">Single, true if the minMax is a 1x1 texture</param>
        private static void Rescale01(RTHandle tex, RTHandle minMax, GPUScan.Direction direction, CommandBuffer cmd, bool single = false)
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

        /// <summary>
        /// Helper to generate Importance Sampled samples (RG: UV on Equirectangular Map, G: PDF, B: CDF)
        /// </summary>
        /// <param name="samplesCount">Samples Count</param>
        /// <param name="marginal">Marginal from ImportanceSamplersSystem</param>
        /// <param name="conditionalMarginal">Conditional Marginal from ImportanceSamplersSystem</param>
        /// <param name="direction">Direction: {Vertical, Horizontal}</param>
        /// <param name="cmd">Command Buffer</param>
        /// <param name="hemiSphere">true if the Marginal was generated for Hemisphere</param>
        /// <returns></returns>
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
