#define DUMP_IMAGE

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
        public RTHandle m_InvCDFFull = null;
        public RTHandle m_InvCDFRows = null;
        RTHandle m_OutDebug;

#if DUMP_IMAGE
        static public int _Idx = 0;

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
                ++_Idx;
            }
        }
#endif

        public void Init(RTHandle density, Vector4 hdriIntegral, CommandBuffer cmd)
        {
            // Rescale pdf between 0 & 1
            RTHandle pdfCopy = RTHandles.Alloc(density.rt.width, density.rt.height, //slices:(int)Mathf.Log(1024.0f, 2.0f),
                //useMipMap:pdfDensity.rt.useMipMap, autoGenerateMips:true,
                colorFormat: density.rt.graphicsFormat, enableRandomWrite: true);
            //RTHandle pdfCopy = RTHandles.Alloc(pdfDensity.rt.width, pdfDensity.rt.height, slices:(int)Mathf.Log(1024.0f, 2.0f),
            //    useMipMap:pdfDensity.rt.useMipMap, autoGenerateMips:true,
            //    colorFormat: pdfDensity.rt.graphicsFormat, enableRandomWrite: true);
            cmd.CopyTexture(density, pdfCopy);
#if DUMP_IMAGE
            ParallelOperation._Idx = 0;
            _Idx = 0;
            cmd.RequestAsyncReadback(pdfCopy, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "___PDFCopy");
            });
#endif
            Rescale(pdfCopy, new Vector2(0.0f, hdriIntegral.w), cmd);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(pdfCopy, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "___PDFCopyRescaled");
            });
#endif

            ////////////////////////////////////////////////////////////////////////////////
            /// Full
            ////////////////////////////////////////////////////////////////////////////////
            // MinMax of rows
            RTHandle minMaxFull0 = ParallelOperation.ComputeOperation(
                                    pdfCopy,
                                    cmd,
                                    ParallelOperation.Operation.MinMax,
                                    ParallelOperation.Direction.Horizontal,
                                    2,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(minMaxFull0, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "00_MinMaxOfRows");
            });
#endif

            // MinMax of the MinMax of rows => Single Pixel
            RTHandle minMaxFull1 = ParallelOperation.ComputeOperation(
                                    minMaxFull0,
                                    cmd,
                                    ParallelOperation.Operation.MinMax,
                                    ParallelOperation.Direction.Vertical,
                                    2,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(minMaxFull1, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "01_MinMaxOfMinMaxOfRows");
            });
#endif
            Rescale(pdfCopy, minMaxFull1, ParallelOperation.Direction.Horizontal, cmd, true);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(pdfCopy, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "02_PDFRescaled");
            });
#endif

            ////////////////////////////////////////////////////////////////////////////////
            ///
            //
            // 1. Integrate the normalized the sphere
            {
                //var hdrp = HDRenderPipeline.defaultAsset;
                //Material integrator = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.integrateHdriSkyPS);

                //RTHandle intensitySphereTexture = RTHandles.Alloc(1, 1, colorFormat: Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);

                //Texture2D readBackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);

                //Graphics.Blit(Texture2D.whiteTexture, intensitySphereTexture.rt, integrator, 1);

                //RenderTexture.active = intensitySphereTexture.rt;
                //readBackTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                //RenderTexture.active = null;

                //Vector4 skyIntensity = readBackTexture.GetPixel(0, 0);
            }
            //
            ///
            ////////////////////////////////////////////////////////////////////////////////

            // Compute the CDF of the rows of the rescaled PDF
            RTHandle cdfFull = ComputeCDF1D.ComputeCDF(
                                    pdfCopy,
                                    cmd,
                                    ComputeCDF1D.SumDirection.Horizontal,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "03_CDFFull");
            });
#endif

            // Rescale between 0 & 1 the rows_cdf: to be inverted in UV
            RTHandle minMaxFull = ParallelOperation.ComputeOperation(
                                    cdfFull,
                                    cmd,
                                    ParallelOperation.Operation.MinMax,
                                    ParallelOperation.Direction.Horizontal,
                                    2,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(minMaxFull, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "04_MinMaxCDF");
            });
#endif

            ////////////////////////////////////////////////////////////////////////////////
            /// Rows
            // Before Rescaling the CDFFull
            RTHandle sumRows = RTHandles.Alloc(1, density.rt.height, colorFormat: density.rt.graphicsFormat, enableRandomWrite: true);

            // Last columns of "CDF of rows" already contains the sum of rows
            cmd.CopyTexture(cdfFull, 0, 0, density.rt.width - 1, 0, 1, density.rt.height, sumRows, 0, 0, 0, 0);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "05_SumRowsFromCopy");
            });
#endif
            ////////////////////////////////////////////////////////////////////////////////

            Rescale(cdfFull, minMaxFull, ParallelOperation.Direction.Horizontal, cmd);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(cdfFull, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "06_CDFRescaled");
            });
#endif

            ////////////////////////////////////////////////////////////////////////////////
            /// Rows
            ////////////////////////////////////////////////////////////////////////////////

            // Minmax of rows
            RTHandle minMaxRows = ParallelOperation.ComputeOperation(sumRows,
                                                    cmd,
                                                    ParallelOperation.Operation.MinMax,
                                                    ParallelOperation.Direction.Vertical,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(minMaxRows, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "07_MinMaxSumOfRows");
            });
#endif

            // Rescale sum of rows
            Rescale(sumRows, minMaxRows, ParallelOperation.Direction.Vertical, cmd, true);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(sumRows, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "08_SumRowsRescaled");
            });
#endif
            RTHandle cdfRows = ComputeCDF1D.ComputeCDF(
                                    sumRows,
                                    cmd,
                                    ComputeCDF1D.SumDirection.Vertical,
                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(cdfRows, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "09_CDFRows");
            });
#endif
            RTHandle minMaxCDFRows = ParallelOperation.ComputeOperation(cdfRows,
                                                    cmd,
                                                    ParallelOperation.Operation.MinMax,
                                                    ParallelOperation.Direction.Vertical,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(minMaxCDFRows, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "10_MinMaxCDFRows");
            });
#endif
            Rescale(cdfRows, minMaxCDFRows, ParallelOperation.Direction.Vertical, cmd, true);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(cdfRows, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "11_MinMaxCDFRowsRescaled");
            });
#endif

            // Compute inverse of CDFs
            m_InvCDFFull = ComputeCDF1D.ComputeInverseCDF(cdfFull,
                                                          pdfCopy,
                                                          Vector4.one,
                                                          cmd,
                                                          ComputeCDF1D.SumDirection.Horizontal,
                                                          Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(m_InvCDFFull, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "12_InvCDFFull");
            });
#endif
            m_InvCDFRows = ComputeCDF1D.ComputeInverseCDF(cdfRows,
                                                          pdfCopy,
                                                          hdriIntegral,
                                                          cmd,
                                                          ComputeCDF1D.SumDirection.Vertical,
                                                          Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(m_InvCDFRows, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "13_InvCDFRows");
            });
#endif

            // Generate sample from invCDFs
            RTHandle samples = GenerateSamples(4096, m_InvCDFRows, m_InvCDFFull, ParallelOperation.Direction.Horizontal, cmd);

            //
            m_OutDebug = RTHandles.Alloc(density.rt.width, density.rt.height, colorFormat: density.rt.graphicsFormat, enableRandomWrite: true);
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader outputDebug2D = hdrp.renderPipelineResources.shaders.OutputDebugCS;

            int kernel = outputDebug2D.FindKernel("CSMain");

            cmd.CopyTexture(density, m_OutDebug);

            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Output,  m_OutDebug);
            cmd.SetComputeTextureParam(outputDebug2D, kernel, HDShaderIDs._Samples, samples);
            cmd.SetComputeIntParams   (outputDebug2D, HDShaderIDs._Sizes,
                                       density.rt.width, density.rt.height, samples.rt.width, 1);

            int numTilesX = (samples.rt.width  + (8 - 1))/8;
            cmd.DispatchCompute(outputDebug2D, kernel, numTilesX, 1, 1);
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(m_OutDebug, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "Debug");
            });
#endif
        }

        private void Rescale(RTHandle tex, RTHandle minMax, ParallelOperation.Direction direction, CommandBuffer cmd, bool single = false)
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
            //cmd.RequestAsyncReadback(tex, SaveTempImg);
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

        public RTHandle GenerateSamples(uint samplesCount, RTHandle sliceInvCDF, RTHandle fullInvCDF, ParallelOperation.Direction direction, CommandBuffer cmd)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader importanceSample2D = hdrp.renderPipelineResources.shaders.importanceSample2DCS;

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
#if DUMP_IMAGE
            cmd.RequestAsyncReadback(samples, delegate (AsyncGPUReadbackRequest request)
            {
                Default(request, "Samples");
            });
#endif

            return samples;
        }
    }
}
