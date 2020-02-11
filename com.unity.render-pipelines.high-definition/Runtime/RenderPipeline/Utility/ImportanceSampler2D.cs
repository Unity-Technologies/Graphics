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

        public static void GenerateMarginals(
                                out RTHandle invCDFRows, out RTHandle invCDFFull,
                                RTHandle density,
                                int elementIndex, int mipIndex,
                                CommandBuffer cmd,
                                bool dumpFile, int idx)
        {
            int width   = Mathf.RoundToInt((float)density.rt.width /Mathf.Pow(2.0f, (float)mipIndex));
            int height  = Mathf.RoundToInt((float)density.rt.height/Mathf.Pow(2.0f, (float)mipIndex));

            //GraphicsFormat internalFormat = GraphicsFormat.R16G16B16A16_SFloat;
            //GraphicsFormat internalFormat = GraphicsFormat.R16_SFloat;
            invCDFRows = null;
            invCDFFull = null;

            //RTHandle pdfCopy = RTHandles.Alloc(width, height, colorFormat: density.graphicsFormat, enableRandomWrite: true);
            //RTHandleDeleter.ScheduleRelease(pdfCopy);

#if DUMP_IMAGE
            string strName = string.Format("{0}S{1}M{1}", idx, elementIndex, mipIndex);
#endif

            //cmd.CopyTexture(density, elementIndex, mipIndex, pdfCopy, 0, 0);
#if DUMP_IMAGE
            //if (dumpFile)
            //    cmd.RequestAsyncReadback(density, delegate (AsyncGPUReadbackRequest request)
            //    {
            //        Default(request, "___PDFCopy" + strName, density.rt.graphicsFormat);
            //    });
#endif

            ////////////////////////////////////////////////////////////////////////////////
            /// Full
            ////////////////////////////////////////////////////////////////////////////////
            // MinMax of rows
            RTHandle minMaxFull0;
            using (new ProfilingScope(cmd, new ProfilingSampler("MinMaxOfRows")))
            {
                minMaxFull0 = GPUOperation.ComputeOperation(
                                                density,
                                                cmd,
                                                GPUOperation.Operation.MinMax,
                                                GPUOperation.Direction.Horizontal,
                                                2, // opPerThread
                                                true, // isPDF
                                                GraphicsFormat.R32G32_SFloat);
                RTHandleDeleter.ScheduleRelease(minMaxFull0);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(minMaxFull0, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "00_MinMaxOfRows" + strName, minMaxFull0.rt.graphicsFormat);
                    });
#endif
            }

            RTHandle minMaxFull1;
            using (new ProfilingScope(cmd, new ProfilingSampler("MinMaxOfRowsAndRescale")))
            {
                // MinMax of the MinMax of rows => Single Pixel
                minMaxFull1 = GPUOperation.ComputeOperation(
                                            minMaxFull0,
                                            cmd,
                                            GPUOperation.Operation.MinMax,
                                            GPUOperation.Direction.Vertical,
                                            2, // opPerThread
                                            false, // isPDF
                                            GraphicsFormat.R32G32_SFloat);
                RTHandleDeleter.ScheduleRelease(minMaxFull1);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(minMaxFull1, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "01_MinMaxOfMinMaxOfRows" + strName, minMaxFull1.rt.graphicsFormat);
                    });
#endif
                Rescale(density, minMaxFull1, GPUOperation.Direction.Horizontal, cmd, true);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(density, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "02_PDFRescaled" + strName, density.rt.graphicsFormat);
                    });
#endif
            }
            //*
            RTHandle cdfFull;
            using (new ProfilingScope(cmd, new ProfilingSampler("ComputeCDFRescaled")))
            {
                // Compute the CDF of the rows of the rescaled PDF
                cdfFull = ComputeCDF1D.ComputeCDF(
                                        density,
                                        cmd,
                                        ComputeCDF1D.SumDirection.Horizontal,
                                        GraphicsFormat.R32_SFloat);
                RTHandleDeleter.ScheduleRelease(cdfFull);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "03_CDFFull" + strName, cdfFull.rt.graphicsFormat);
                    });
#endif
            }

            RTHandle minMaxFull;
            RTHandle sumRows;
            using (new ProfilingScope(cmd, new ProfilingSampler("MinMaxRowsCDFAndRescale")))
            {
                // Rescale between 0 & 1 the rows_cdf: to be inverted in UV
                minMaxFull = GPUOperation.ComputeOperation(
                                            cdfFull,
                                            cmd,
                                            GPUOperation.Operation.MinMax,
                                            GPUOperation.Direction.Horizontal,
                                            2,
                                            false,
                                            GraphicsFormat.R32G32_SFloat);
                RTHandleDeleter.ScheduleRelease(minMaxFull);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(minMaxFull, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "04_MinMaxCDF" + strName, minMaxFull.rt.graphicsFormat);
                    });
#endif

                ////////////////////////////////////////////////////////////////////////////////
                /// Rows
                // Before Rescaling the CDFFull
                sumRows = RTHandles.Alloc(1, height, colorFormat: density.rt.graphicsFormat, enableRandomWrite: true);
                RTHandleDeleter.ScheduleRelease(sumRows);

                // Last columns of "CDF of rows" already contains the sum of rows
                cmd.CopyTexture(cdfFull, 0, 0, width - 1, 0, 1, height, sumRows, 0, 0, 0, 0);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "05_SumRowsFromCopy" + strName, sumRows.rt.graphicsFormat);
                    });
#endif
            ////////////////////////////////////////////////////////////////////////////////

                Rescale(cdfFull, minMaxFull, GPUOperation.Direction.Horizontal, cmd);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "06_CDFRescaled" + strName, cdfFull.rt.graphicsFormat);
                    });
#endif
            }

            ////////////////////////////////////////////////////////////////////////////////
            /// Rows
            ////////////////////////////////////////////////////////////////////////////////
            RTHandle minMaxRows;
            using (new ProfilingScope(cmd, new ProfilingSampler("MinMaxColsCDFAndRescale")))
            {
                // Minmax of rows
                minMaxRows = GPUOperation.ComputeOperation(
                                                    sumRows,
                                                    cmd,
                                                    GPUOperation.Operation.MinMax,
                                                    GPUOperation.Direction.Vertical,
                                                    2,
                                                    false,
                                                    GraphicsFormat.R32G32_SFloat);
                RTHandleDeleter.ScheduleRelease(minMaxRows);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(minMaxRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "07_MinMaxSumOfRows" + strName, minMaxRows.rt.graphicsFormat);
                    });
#endif

                // Rescale sum of rows
                Rescale(sumRows, minMaxRows, GPUOperation.Direction.Vertical, cmd, true);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "08_SumRowsRescaled" + strName, sumRows.rt.graphicsFormat);
                    });
#endif
            }

            RTHandle cdfRows;
            using (new ProfilingScope(cmd, new ProfilingSampler("ComputeCDFCols")))
            {
                cdfRows = ComputeCDF1D.ComputeCDF(
                                    sumRows,
                                    cmd,
                                    ComputeCDF1D.SumDirection.Vertical,
                                    GraphicsFormat.R32_SFloat);
                RTHandleDeleter.ScheduleRelease(cdfRows);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(cdfRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "09_CDFRows" + strName, cdfRows.rt.graphicsFormat);
                    });
#endif
            }

            RTHandle minMaxCDFRows;
            using (new ProfilingScope(cmd, new ProfilingSampler("MinMaxCDFAndRescale_")))
            {
                minMaxCDFRows = GPUOperation.ComputeOperation(cdfRows,
                                                        cmd,
                                                        GPUOperation.Operation.MinMax,
                                                        GPUOperation.Direction.Vertical,
                                                        2,
                                                        false,
                                                        GraphicsFormat.R32G32_SFloat);
                RTHandleDeleter.ScheduleRelease(minMaxCDFRows);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(minMaxCDFRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "10_MinMaxCDFRows" + strName, minMaxCDFRows.rt.graphicsFormat);
                    });
#endif
                Rescale(cdfRows, minMaxCDFRows, GPUOperation.Direction.Vertical, cmd, true);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(cdfRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "11_MinMaxCDFRowsRescaled" + strName, cdfRows.rt.graphicsFormat);
                    });
#endif
            }

            using (new ProfilingScope(cmd, new ProfilingSampler("InverseCDFConditional")))
            {
                // Compute inverse of CDFs
                invCDFFull = ComputeCDF1D.ComputeInverseCDF(cdfFull,
                                                            density,
                                                            Vector4.one,
                                                            cmd,
                                                            ComputeCDF1D.SumDirection.Horizontal,
                                                            GraphicsFormat.R32_SFloat);
#if DUMP_IMAGE
                var format = invCDFFull.rt.graphicsFormat;
                if (dumpFile)
                    cmd.RequestAsyncReadback(invCDFFull, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "12_InvCDFFull" + strName, format);
                    });
#endif
            }
            using (new ProfilingScope(cmd, new ProfilingSampler("InverseCDFRows")))
            {
                invCDFRows = ComputeCDF1D.ComputeInverseCDF(cdfRows,
                                                            density,
                                                            Vector4.one,
                                                            //hdriIntegral,
                                                            cmd,
                                                            ComputeCDF1D.SumDirection.Vertical,
                                                            GraphicsFormat.R32_SFloat);
#if DUMP_IMAGE
                var format = invCDFRows.rt.graphicsFormat;
                if (dumpFile)
                    cmd.RequestAsyncReadback(invCDFRows, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "13_InvCDFRows" + strName, format);
                    });
#endif
            }
            //*/

            using (new ProfilingScope(cmd, new ProfilingSampler("DebugInfos")))
            {
                // Generate sample from invCDFs
                //uint threadsCount        = 64u;
                //uint samplesPerThread    = 8u;
                uint threadsCount        = 512u;
                uint samplesPerThread    = 32u;
                // SamplesCount can only be in the set {32, 64, 512} cf. "ImportanceLatLongIntegration.compute" kernel available 'ThreadsCount'
                uint samplesCount        = threadsCount*samplesPerThread;
                RTHandle samples = GenerateSamples(samplesCount, invCDFRows, invCDFFull, GPUOperation.Direction.Horizontal, cmd);
                RTHandleDeleter.ScheduleRelease(samples);
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(samples, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "Samples" + strName, samples.rt.graphicsFormat);
                    });
#endif

                int kernel;

                var hdrp = HDRenderPipeline.defaultAsset;
                //{
                //    ComputeShader buildProbabilityTablesCS = hdrp.renderPipelineResources.shaders.buildProbabilityTablesCS;
                //    int kerCond = buildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
                //    int kerMarg = buildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");

                //    //
                //}

                /////////////////////////////////////////////////////////
                /// Integrate Sphere
                //RTHandle sphereIntegralTexture = RTHandles.Alloc(3, 1, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true);
                /*
                RTHandle sphereIntegralTexture = RTHandles.Alloc(3, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);
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
                //cmd.RequestAsyncReadback(sphereIntegralTexture, delegate (AsyncGPUReadbackRequest request)
                //{
                //    StoreIntegral(request);
                //});
#if DUMP_IMAGE
                if (dumpFile)
                    cmd.RequestAsyncReadback(sphereIntegralTexture, delegate (AsyncGPUReadbackRequest request)
                    {
                        Default(request, "Integrations" + strName, sphereIntegralTexture.rt.graphicsFormat);
                    });
#endif
                */
                /*
                /////////////////////////////////////////////////////////
                /// Store the Integral
                {
                    ComputeShader setChannel = hdrp.renderPipelineResources.shaders.setChannelCS;
                    //sphereIntegralTexture;

                    kernel = setChannel.FindKernel("SetBT");

                    cmd.SetComputeTextureParam(setChannel, kernel, HDShaderIDs._InputTex, sphereIntegralTexture);
                    cmd.SetComputeTextureParam(setChannel, kernel, HDShaderIDs._Output, invCDFFull);
                    cmd.SetComputeIntParams   (setChannel,         HDShaderIDs._Sizes,
                                               invCDFFull.rt.width, invCDFFull.rt.height, invCDFFull.rt.width, invCDFFull.rt.height);

                    int localTilesX = (invCDFFull.rt.width  + (8 - 1))/8;
                    int localTilesY = (invCDFFull.rt.height + (8 - 1))/8;
                    cmd.DispatchCompute(setChannel, kernel, localTilesX, localTilesY, 1);
#if DUMP_IMAGE
                    if (dumpFile)
                        cmd.RequestAsyncReadback(invCDFFull, delegate (AsyncGPUReadbackRequest request)
                        {
                            Default(request, "invCDFFullWithIntegral" + strName);
                        });
#endif
                }
                /////////////////////////////////////////////////////////
                */
                //
                //RTHandle m_OutDebug = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true);
                RTHandle m_OutDebug = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);
                RTHandleDeleter.ScheduleRelease(m_OutDebug);

                //var hdrp = HDRenderPipeline.defaultAsset;
                ComputeShader outputDebug2D = hdrp.renderPipelineResources.shaders.OutputDebugCS;

                kernel = outputDebug2D.FindKernel("CSMain");

                ///cmd.CopyTexture(density, m_OutDebug);

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
                        Default(request, "Debug" + strName, m_OutDebug.rt.graphicsFormat);
                    });
#endif
            }
        }

        private static void Rescale(RTHandle tex, RTHandle minMax, GPUOperation.Direction direction, CommandBuffer cmd, bool single = false)
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

        private static void Rescale(RTHandle tex, Vector2 minMax, CommandBuffer cmd)
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

        public static RTHandle GenerateSamples(uint samplesCount, RTHandle sliceInvCDF, RTHandle fullInvCDF, GPUOperation.Direction direction, CommandBuffer cmd)
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

            //RTHandle samples = RTHandles.Alloc((int)samplesCount, 1, colorFormat: GraphicsFormat.R16G16B16A16_SFloat/*fullInvCDF.rt.graphicsFormat*/, enableRandomWrite: true);
            RTHandle samples = RTHandles.Alloc((int)samplesCount, 1, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);

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
