#define DUMP_IMAGE

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    class ImportanceSampler2D
    {
        public RTHandle invCDFRows { get; internal set; }
        public RTHandle invCDFFull { get; internal set; }

#if DUMP_IMAGE
        static private void Default(AsyncGPUReadbackRequest request, string name)
        {
            if (!request.hasError)
            {
                Unity.Collections.NativeArray<float> result = request.GetData<float>();
                float[] copy = new float[result.Length];
                result.CopyTo(copy);
                byte[] bytes0 = ImageConversion.EncodeArrayToEXR(copy, Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)request.width, (uint)request.height, 0, Texture2D.EXRFlags.CompressZIP);
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

        public void Init(Texture density, int elementIndex, int mipIndex, CommandBuffer cmd, bool dumpFile, int idx)
        {
            int width   = Mathf.RoundToInt((float)density.width /Mathf.Pow(2.0f, (float)mipIndex));
            int height  = Mathf.RoundToInt((float)density.height/Mathf.Pow(2.0f, (float)mipIndex));

            UnityEngine.Experimental.Rendering.GraphicsFormat internalFormat =
                //density.graphicsFormat;
                Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;

            // Rescale pdf between 0 & 1
            RTHandle pdfCopy = RTHandles.Alloc(width, height, colorFormat: density.graphicsFormat, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(pdfCopy);

            string strName = string.Format("{0}S{1}M{1}", idx, elementIndex, mipIndex);

            cmd.CopyTexture(density, elementIndex, mipIndex, pdfCopy, 0, 0);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(pdfCopy, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "___PDFCopy" + strName);
                });
#endif
//            Rescale(pdfCopy, new Vector2(0.0f, hdriIntegral.w), cmd);
//#if DUMP_IMAGE
//            cmd.RequestAsyncReadback(pdfCopy, delegate (AsyncGPUReadbackRequest request)
//            {
//                Default(request, "___PDFCopyRescaled");
//            });
//#endif

            ////////////////////////////////////////////////////////////////////////////////
            /// Full
            ////////////////////////////////////////////////////////////////////////////////
            // MinMax of rows
            RTHandle minMaxFull0 = GPUOperation.ComputeOperation(
                                    pdfCopy,
                                    cmd,
                                    GPUOperation.Operation.MinMax,
                                    GPUOperation.Direction.Horizontal,
                                    2, // opPerThread
                                    true, // isPDF
                                    internalFormat);
            RTHandleDeleter.ScheduleRelease(minMaxFull0);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxFull0, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "00_MinMaxOfRows" + strName);
                });
#endif

            // MinMax of the MinMax of rows => Single Pixel
            RTHandle minMaxFull1 = GPUOperation.ComputeOperation(
                                    minMaxFull0,
                                    cmd,
                                    GPUOperation.Operation.MinMax,
                                    GPUOperation.Direction.Vertical,
                                    2, // opPerThread
                                    false, // isPDF
                                    internalFormat);
            RTHandleDeleter.ScheduleRelease(minMaxFull1);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxFull1, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "01_MinMaxOfMinMaxOfRows" + strName);
                });
#endif
            Rescale(pdfCopy, minMaxFull1, GPUOperation.Direction.Horizontal, cmd, true);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(pdfCopy, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "02_PDFRescaled" + strName);
                });
#endif

            // Compute the CDF of the rows of the rescaled PDF
            RTHandle cdfFull = ComputeCDF1D.ComputeCDF(
                                    pdfCopy,
                                    cmd,
                                    ComputeCDF1D.SumDirection.Horizontal,
                                    internalFormat);
            RTHandleDeleter.ScheduleRelease(cdfFull);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "03_CDFFull" + strName);
                });
#endif

            // Rescale between 0 & 1 the rows_cdf: to be inverted in UV
            RTHandle minMaxFull = GPUOperation.ComputeOperation(
                                    cdfFull,
                                    cmd,
                                    GPUOperation.Operation.MinMax,
                                    GPUOperation.Direction.Horizontal,
                                    2,
                                    false,
                                    internalFormat);
            RTHandleDeleter.ScheduleRelease(minMaxFull);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxFull, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "04_MinMaxCDF" + strName);
                });
#endif

            ////////////////////////////////////////////////////////////////////////////////
            /// Rows
            // Before Rescaling the CDFFull
            RTHandle sumRows = RTHandles.Alloc(1, height, colorFormat: density.graphicsFormat, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(sumRows);

            // Last columns of "CDF of rows" already contains the sum of rows
            cmd.CopyTexture(cdfFull, 0, 0, width - 1, 0, 1, height, sumRows, 0, 0, 0, 0);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "05_SumRowsFromCopy" + strName);
                });
#endif
            ////////////////////////////////////////////////////////////////////////////////

            Rescale(cdfFull, minMaxFull, GPUOperation.Direction.Horizontal, cmd);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "06_CDFRescaled" + strName);
                });
#endif

            ////////////////////////////////////////////////////////////////////////////////
            /// Rows
            ////////////////////////////////////////////////////////////////////////////////

            // Minmax of rows
            RTHandle minMaxRows = GPUOperation.ComputeOperation(sumRows,
                                                    cmd,
                                                    GPUOperation.Operation.MinMax,
                                                    GPUOperation.Direction.Vertical,
                                                    2,
                                                    false,
                                                    internalFormat);
            RTHandleDeleter.ScheduleRelease(minMaxRows);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "07_MinMaxSumOfRows" + strName);
                });
#endif

            // Rescale sum of rows
            Rescale(sumRows, minMaxRows, GPUOperation.Direction.Vertical, cmd, true);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "08_SumRowsRescaled" + strName);
                });
#endif
            RTHandle cdfRows = ComputeCDF1D.ComputeCDF(
                                    sumRows,
                                    cmd,
                                    ComputeCDF1D.SumDirection.Vertical,
                                    internalFormat);
            RTHandleDeleter.ScheduleRelease(cdfRows);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "09_CDFRows" + strName);
                });
#endif
            RTHandle minMaxCDFRows = GPUOperation.ComputeOperation(cdfRows,
                                                    cmd,
                                                    GPUOperation.Operation.MinMax,
                                                    GPUOperation.Direction.Vertical,
                                                    2,
                                                    false,
                                                    internalFormat);
            RTHandleDeleter.ScheduleRelease(minMaxCDFRows);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(minMaxCDFRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "10_MinMaxCDFRows" + strName);
                });
#endif
            Rescale(cdfRows, minMaxCDFRows, GPUOperation.Direction.Vertical, cmd, true);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(cdfRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "11_MinMaxCDFRowsRescaled" + strName);
                });
#endif

            // Compute inverse of CDFs
            invCDFFull = ComputeCDF1D.ComputeInverseCDF(cdfFull,
                                                        pdfCopy,
                                                        Vector4.one,
                                                        cmd,
                                                        ComputeCDF1D.SumDirection.Horizontal,
                                                        internalFormat);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(invCDFFull, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "12_InvCDFFull" + strName);
                });
#endif
            invCDFRows = ComputeCDF1D.ComputeInverseCDF(cdfRows,
                                                        pdfCopy,
                                                        Vector4.one,
                                                        //hdriIntegral,
                                                        cmd,
                                                        ComputeCDF1D.SumDirection.Vertical,
                                                        internalFormat);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(invCDFRows, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "13_InvCDFRows" + strName);
                });
#endif

            // Generate sample from invCDFs
            uint threadsCount        = 64u;
            uint samplesPerThread    = 8u;
            // SamplesCount can only be in the set {32, 64, 512} cf. "ImportanceLatLongIntegration.compute" kernel available 'ThreadsCount'
            uint samplesCount        = threadsCount*samplesPerThread;
            RTHandle samples = GenerateSamples(samplesCount, invCDFRows, invCDFFull, GPUOperation.Direction.Horizontal, cmd);
            RTHandleDeleter.ScheduleRelease(samples);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(samples, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "Samples" + strName);
                });
#endif

            int kernel;

            var hdrp = HDRenderPipeline.defaultAsset;
            /////////////////////////////////////////////////////////
            /// Integrate Sphere
            RTHandle sphereIntegralTexture = RTHandles.Alloc(3, 1, colorFormat: internalFormat, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(sphereIntegralTexture);

            ComputeShader importanceLatLongIntegration = hdrp.renderPipelineResources.shaders.importanceLatLongIntegrationCS;

            kernel = importanceLatLongIntegration.FindKernel("CSMain" + samplesCount.ToString());

            cmd.SetComputeTextureParam(importanceLatLongIntegration, kernel, HDShaderIDs._Input, density);
            cmd.SetComputeTextureParam(importanceLatLongIntegration, kernel, HDShaderIDs._Samples, samples);
            cmd.SetComputeTextureParam(importanceLatLongIntegration, kernel, HDShaderIDs._Output, sphereIntegralTexture);
            cmd.SetComputeIntParams   (importanceLatLongIntegration,         HDShaderIDs._Sizes,
                                       width, height, (int)samplesCount, 1);
            cmd.SetComputeIntParams   (importanceLatLongIntegration, "_SamplesPerThread", (int)samplesPerThread);
            cmd.SetComputeFloatParams (importanceLatLongIntegration,         HDShaderIDs._Params, 1.0f/((float)samplesPerThread), 0.0f, 0.0f, 0.0f);

            cmd.DispatchCompute(importanceLatLongIntegration, kernel, 1, 1, 1);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(sphereIntegralTexture, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "Integrations" + strName);
                });
#endif
            /////////////////////////////////////////////////////////

            //
            RTHandle m_OutDebug = RTHandles.Alloc(width, height, colorFormat: density.graphicsFormat, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(m_OutDebug);

            //var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader outputDebug2D = hdrp.renderPipelineResources.shaders.OutputDebugCS;

            kernel = outputDebug2D.FindKernel("CSMain");

            cmd.CopyTexture(density, m_OutDebug);

            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Output,  m_OutDebug);
            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Samples, samples);
            cmd.SetComputeIntParams   (outputDebug2D, HDShaderIDs._Sizes,
                                       width, height, samples.rt.width, 1);

            int numTilesX = (samples.rt.width  + (8 - 1))/8;
            cmd.DispatchCompute(outputDebug2D, kernel, numTilesX, 1, 1);
#if DUMP_IMAGE
            if (dumpFile)
                cmd.RequestAsyncReadback(m_OutDebug, delegate (AsyncGPUReadbackRequest request)
                {
                    Default(request, "Debug" + strName);
                });
#endif
        }

        private void Rescale(RTHandle tex, RTHandle minMax, GPUOperation.Direction direction, CommandBuffer cmd, bool single = false)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader rescale01 = hdrp.renderPipelineResources.shaders.rescale01CS;

            rescale01.EnableKeyword("MINMAX");
            rescale01.EnableKeyword("READ_WRITE");
            string addon0 = "";
            if (single)
            {
                addon0 += "S";
            }
            else if (direction == GPUOperation.Direction.Horizontal)
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
        }

        private void Rescale(RTHandle tex, Vector2 minMax, CommandBuffer cmd)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader rescale01 = hdrp.renderPipelineResources.shaders.rescale01CS;

            rescale01.EnableKeyword("MINMAX");
            rescale01.EnableKeyword("READ_WRITE");

            int kernel = rescale01.FindKernel("CSMainValue");

            cmd.SetComputeTextureParam(rescale01, kernel, HDShaderIDs._Output, tex);
            cmd.SetComputeVectorParam (rescale01,         HDShaderIDs._MinMax, minMax);
            cmd.SetComputeIntParams   (rescale01,         HDShaderIDs._Sizes,
                                       tex.rt.width, tex.rt.height, tex.rt.width, tex.rt.height);

            int numTilesX = (tex.rt.width  + (8 - 1))/8;
            int numTilesY = (tex.rt.height + (8 - 1))/8;

            cmd.DispatchCompute(rescale01, kernel, numTilesX, numTilesY, 1);
        }

        public RTHandle GenerateSamples(uint samplesCount, RTHandle sliceInvCDF, RTHandle fullInvCDF, GPUOperation.Direction direction, CommandBuffer cmd)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader importanceSample2D = hdrp.renderPipelineResources.shaders.importanceSample2DCS;

            string addon = "";
            if (direction == GPUOperation.Direction.Horizontal)
            {
                addon += "H";
            }
            else
            {
                addon += "V";
            }

            RTHandle samples = RTHandles.Alloc((int)samplesCount, 1, colorFormat: fullInvCDF.rt.graphicsFormat, enableRandomWrite: true);

            int kernel = importanceSample2D.FindKernel("CSMain" + addon);

            cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._SliceInvCDF, sliceInvCDF);
            cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._InvCDF,      fullInvCDF);
            cmd.SetComputeTextureParam(importanceSample2D, kernel, HDShaderIDs._Output,      samples);
            cmd.SetComputeIntParams   (importanceSample2D,         HDShaderIDs._Sizes,
                                       fullInvCDF.rt.width, fullInvCDF.rt.height, (int)samplesCount, 1);

            int numTilesX = (samples.rt.width + (8 - 1))/8;

            cmd.DispatchCompute(importanceSample2D, kernel, numTilesX, 1, 1);

            return samples;
        }
    }
}
