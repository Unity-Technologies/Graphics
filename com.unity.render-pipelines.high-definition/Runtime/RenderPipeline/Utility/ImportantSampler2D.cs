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
        //ComputeShader m_ComputeCDF;
        //ComputeShader m_ComputeInvCDF;

        Texture2D       m_CFDinv; // Cumulative Function Distribution Inverse
        ComputeBuffer   m_GeneratedSamples;

        RTHandle m_CDFFull;
        RTHandle m_MinMaxFull;

        public ImportantSampler2D()
        {
            //m_Shader = shader;
            //k_SampleKernel_xyzw2x_8 = m_Shader.FindKernel("KSampleCopy4_1_x_8");
            //k_SampleKernel_xyzw2x_1 = m_Shader.FindKernel("KSampleCopy4_1_x_1");
            //var hdrp = HDRenderPipeline.defaultAsset;
            //m_ComputeCDF = hdrp.renderPipelineResources.shaders.sum2DCS;
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
            //RTHandle sum = ParallelOperation.ComputeOperation(pdfDensity,
            //                                        cmd,
            //                                        ParallelOperation.Operation.Sum,
            //                                        ParallelOperation.Direction.Horizontal,
            //                                        2,
            //                                        Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);

            cmd.RequestAsyncReadback(pdfDensity, SavePDFDensity);
            m_CDFFull = ComputeCDF1D.ComputeCDF(pdfDensity,
                                                cmd,
                                                ComputeCDF1D.SumDirection.Horizontal,
                                                Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(m_CDFFull, SaveCDFFull);
            m_MinMaxFull = ParallelOperation.ComputeOperation(m_CDFFull,
                                                    cmd,
                                                    ParallelOperation.Operation.MinMax,
                                                    ParallelOperation.Direction.Horizontal,
                                                    2,
                                                    Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
            cmd.RequestAsyncReadback(m_MinMaxFull, SaveMinMaxFull);

            //RTHandle minMax = ParallelOperation.ComputeOperation(sum,
            //                                        cmd,
            //                                        ParallelOperation.Operation.MinMax,
            //                                        ParallelOperation.Direction.Horizontal,
            //                                        1,
            //                                        Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);

            //_Idx = 0;
            Rescale(m_CDFFull, m_MinMaxFull, ParallelOperation.Direction.Horizontal, cmd);
            //Rescale(sum,     minMax,     ParallelOperation.Direction.Horizontal, cmd);
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

        private void Rescale(RTHandle tex, RTHandle minMax, ParallelOperation.Direction direction, CommandBuffer cmd)
        {
            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader rescale01 = hdrp.renderPipelineResources.shaders.Rescale01CS;

            rescale01.EnableKeyword("MINMAX");
            rescale01.EnableKeyword("READ_WRITE");
            if (direction == ParallelOperation.Direction.Horizontal)
            {
                rescale01.EnableKeyword ("HORIZONTAL");
                rescale01.DisableKeyword("VERTICAL");
            }
            else
            {
                rescale01.DisableKeyword("HORIZONTAL");
                rescale01.EnableKeyword ("VERTICAL");
            }

            int kernel = rescale01.FindKernel("CSMain");

            cmd.SetComputeTextureParam(rescale01, kernel, HDShaderIDs._Output, tex);
            cmd.SetComputeTextureParam(rescale01, kernel, HDShaderIDs._MinMax, minMax);
            cmd.SetComputeIntParams   (rescale01,         HDShaderIDs._Sizes,
                                       tex.rt.width, tex.rt.height, tex.rt.width, tex.rt.height);

            int numTilesX = (tex.rt.width  + (8 - 1))/8;
            int numTilesY = (tex.rt.height + (8 - 1))/8;

            cmd.DispatchCompute(rescale01, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(tex, SaveTempImg);
        }

        public void GenerateSamples(uint samplesCount)
        {
            
        }

        /*
        static readonly int _RectOffset = Shader.PropertyToID("_RectOffset");
        static readonly int _Result1 = Shader.PropertyToID("_Result1");
        static readonly int _Source4 = Shader.PropertyToID("_Source4");
        static int[] _IntParams = new int[2];

        void SampleCopyChannel(
            CommandBuffer cmd,
            Rendering.RectInt rect,
            int _source,
            RenderTargetIdentifier source,
            int _target,
            RenderTargetIdentifier target,
            int slices,
            int kernel8,
            int kernel1)
        {
            Rendering.RectInt main, topRow, rightCol, topRight;
            unsafe
            {
                Rendering.RectInt* dispatch1Rects = stackalloc Rendering.RectInt[3];
                int dispatch1RectCount = 0;
                Rendering.RectInt dispatch8Rect = Rendering.RectInt.zero;

                if (TileLayoutUtils.TryLayoutByTiles(
                    rect,
                    8,
                    out main,
                    out topRow,
                    out rightCol,
                    out topRight))
                {
                    if (topRow.width > 0 && topRow.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRow;
                        ++dispatch1RectCount;
                    }
                    if (rightCol.width > 0 && rightCol.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = rightCol;
                        ++dispatch1RectCount;
                    }
                    if (topRight.width > 0 && topRight.height > 0)
                    {
                        dispatch1Rects[dispatch1RectCount] = topRight;
                        ++dispatch1RectCount;
                    }
                    dispatch8Rect = main;
                }
                else if (rect.width > 0 && rect.height > 0)
                {
                    dispatch1Rects[dispatch1RectCount] = rect;
                    ++dispatch1RectCount;
                }

                cmd.SetComputeTextureParam(m_Shader, kernel8, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _source, source);
                cmd.SetComputeTextureParam(m_Shader, kernel8, _target, target);
                cmd.SetComputeTextureParam(m_Shader, kernel1, _target, target);

                if (dispatch8Rect.width > 0 && dispatch8Rect.height > 0)
                {
                    var r = dispatch8Rect;
                    // Use intermediate array to avoid garbage
                    _IntParams[0] = r.x;
                    _IntParams[1] = r.y;
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, _IntParams);
                    cmd.DispatchCompute(m_Shader, kernel8, (int)Mathf.Max(r.width / 8, 1), (int)Mathf.Max(r.height / 8, 1), slices);
                }

                for (int i = 0, c = dispatch1RectCount; i < c; ++i)
                {
                    var r = dispatch1Rects[i];
                    // Use intermediate array to avoid garbage
                    _IntParams[0] = r.x;
                    _IntParams[1] = r.y;
                    cmd.SetComputeIntParams(m_Shader, _RectOffset, _IntParams);
                    cmd.DispatchCompute(m_Shader, kernel1, (int)Mathf.Max(r.width, 1), (int)Mathf.Max(r.height, 1), slices);
                }
            }
        }
        public void SampleCopyChannel_xyzw2x(CommandBuffer cmd, RTHandle source, RTHandle target, Rendering.RectInt rect)
        {
            Debug.Assert(source.rt.volumeDepth == target.rt.volumeDepth);
            SampleCopyChannel(cmd, rect, _Source4, source, _Result1, target, source.rt.volumeDepth, k_SampleKernel_xyzw2x_8, k_SampleKernel_xyzw2x_1);
        }
        */
    }
}
