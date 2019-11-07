using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Experimental.Rendering;
//using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    // Texture1D 1xN -> Texture2D 1x1
    // Texture1D Nx1 -> Texture2D 1x1
    // Texture2D NxM -> Texture2D 1xM
    // Texture2D NxM -> Texture2D NxM
    class ParallelSum
    {
        static RTHandle m_Temp0;
        static RTHandle m_Temp1;
        static RTHandle m_Final;

        public enum SumDirection
        {
            Vertical,
            Horizontal
        }

        public ParallelSum()
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
                string path = @"C:\UProjects\Out_" + _Idx.ToString() + " .exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static public RTHandle ComputeSum(RTHandle input, CommandBuffer cmd, SumDirection sumDirection, int sumPerThread = 8, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            if (input == null)
            {
                return null;
            }

            if (sumPerThread < 2 || sumPerThread > 64)
            {
                return null;
            }

            if (sumPerThread % 2 == 1)
            {
                return null;
            }

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader sumStep       = hdrp.renderPipelineResources.shaders.ParallelSumCS;
            ComputeShader sumFinalStep  = hdrp.renderPipelineResources.shaders.ParallelSumFinalCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
                format = GraphicsFormat.R32G32B32A32_SFloat;
            else
                format = sumFormat;

            int width  = input.rt.width;
            int height = input.rt.height;

            int outWidth;
            int outHeight;

            if (width == 1 || height == 1)
            {
                outWidth  = 1;
                outHeight = 1;
            }

            if (sumDirection == SumDirection.Vertical)
            {
                outWidth  = width;
                outHeight = 1;

                m_Temp0 = RTHandles.Alloc(outWidth, height/sumPerThread,                colorFormat: format, enableRandomWrite: true);
                m_Temp1 = RTHandles.Alloc(outWidth, height/(sumPerThread*sumPerThread), colorFormat: format, enableRandomWrite: true);
            }
            else // if (sumDirection == SumDirection.Horizontal)
            {
                outWidth  = 1;
                outHeight = height;

                m_Temp0 = RTHandles.Alloc(width/sumPerThread,                outHeight, colorFormat: format, enableRandomWrite: true);
                m_Temp1 = RTHandles.Alloc(width/(sumPerThread*sumPerThread), outHeight, colorFormat: format, enableRandomWrite: true);
            }

            if (m_Temp1.rt.width == 0 || m_Temp1.rt.height == 0)
            {
                return null;
            }

            int curSize;
            if (sumDirection == SumDirection.Vertical)
            {
                curSize = height;
                sumStep.DisableKeyword("HORIZONTAL");
                sumFinalStep.DisableKeyword("HORIZONTAL");
                sumStep.EnableKeyword("VERTICAL");
                sumFinalStep.EnableKeyword("VERTICAL");
                m_Final = RTHandles.Alloc(width: outWidth, height: 1, colorFormat: format, enableRandomWrite: true);
            }
            else
            {
                curSize = width;
                sumStep.DisableKeyword("VERTICAL");
                sumFinalStep.DisableKeyword("VERTICAL");
                sumStep.EnableKeyword("HORIZONTAL");
                sumFinalStep.EnableKeyword("HORIZONTAL");
                m_Final = RTHandles.Alloc(width: 1, height: outHeight, colorFormat: format, enableRandomWrite: true);
            }
            for (int curSumPerThread = 1; curSumPerThread < 7; ++curSumPerThread)
            {
                sumStep.DisableKeyword("SUM_PER_THREAD_" + (1 << curSumPerThread).ToString());
            }
            sumStep.EnableKeyword("SUM_PER_THREAD_" + sumPerThread.ToString());

            int numTilesX;
            int numTilesY;

            _Idx = 0u;

            int kernel = sumStep.FindKernel("CSMain");

            cmd.SetComputeTextureParam(sumStep, kernel, HDShaderIDs._Input,    input);
            cmd.SetComputeTextureParam(sumStep, kernel, HDShaderIDs._Output,   m_Temp0);
            cmd.SetComputeIntParams(sumStep, HDShaderIDs._Sizes, width, height, m_Temp0.rt.width, m_Temp0.rt.height);
            if (sumDirection == SumDirection.Vertical)
            {
                numTilesX =  m_Temp0.rt.width;
                numTilesY = (m_Temp0.rt.height + (8 - 1))/8;
            }
            else
            {
                numTilesX = (m_Temp0.rt.width  + (8 - 1))/8;
                numTilesY =  m_Temp0.rt.height;
            }
            cmd.DispatchCompute(sumStep, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(m_Temp0, SaveTempImg);

            RTHandle inRT   = m_Temp0;
            RTHandle outRT  = m_Temp1;
            curSize /= sumPerThread;
            while (curSize > sumPerThread)
            {
                cmd.SetComputeTextureParam(sumStep, kernel, HDShaderIDs._Input,    inRT);
                cmd.SetComputeTextureParam(sumStep, kernel, HDShaderIDs._Output,   outRT);
                if (sumDirection == SumDirection.Vertical)
                {
                    numTilesX =  outRT.rt.width;
                    numTilesY = (curSize + (8 - 1))/8;
                    cmd.SetComputeIntParams(sumStep, HDShaderIDs._Sizes, width, curSize, width, curSize/sumPerThread);
                }
                else
                {
                    numTilesX = (curSize + (8 - 1))/8;
                    numTilesY =  outRT.rt.height;
                    cmd.SetComputeIntParams(sumStep, HDShaderIDs._Sizes, curSize, height, curSize/sumPerThread, height);
                }
                cmd.DispatchCompute(sumStep, kernel, numTilesX, numTilesY, 1);
                cmd.RequestAsyncReadback(outRT, SaveTempImg);

                CoreUtils.Swap(ref inRT, ref outRT);
                curSize /= sumPerThread;
            };

            kernel = sumFinalStep.FindKernel("CSMain");

            cmd.SetComputeTextureParam(sumFinalStep, kernel, HDShaderIDs._Input,    inRT);
            cmd.SetComputeTextureParam(sumFinalStep, kernel, HDShaderIDs._Output,   m_Final);
            if (sumDirection == SumDirection.Vertical)
            {
                numTilesX = (m_Final.rt.width + (8 - 1))/8;
                numTilesY = 1;
                cmd.SetComputeIntParams(sumFinalStep, HDShaderIDs._Sizes, width, curSize*sumPerThread, width, m_Final.rt.height);
            }
            else
            {
                numTilesX = 1;
                numTilesY = (m_Final.rt.height + (8 - 1))/8;
                cmd.SetComputeIntParams(sumFinalStep, HDShaderIDs._Sizes, curSize*sumPerThread, height, m_Final.rt.width, height);
            }
            cmd.DispatchCompute(sumFinalStep, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(m_Final, SaveTempImg);

            return m_Final;
        }
    }
}
