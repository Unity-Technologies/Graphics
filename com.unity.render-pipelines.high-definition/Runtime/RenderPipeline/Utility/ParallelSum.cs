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
                string path = @"C:\UProjects\Sum_" + _Idx.ToString() + " .exr";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.SetAttributes(path, System.IO.FileAttributes.Normal);
                    System.IO.File.Delete(path);
                }
                System.IO.File.WriteAllBytes(path, bytes0);
                ++_Idx;
            }
        }

        static private void Dispatch(CommandBuffer cmd, ComputeShader cs, int kernel, RTHandle input, RTHandle output, SumDirection direction, int inSize, int outSize)
        {
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Input,  input);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Output, output);
            int numTilesX;
            int numTilesY;
            if (direction == SumDirection.Vertical)
            {
                numTilesX =  m_Temp0.rt.width;
                numTilesY = (outSize + (8 - 1))/8;
                cmd.SetComputeIntParams(cs, HDShaderIDs._Sizes, input.rt.width, inSize, output.rt.width, outSize);
            }
            else
            {
                numTilesX = (outSize + (8 - 1))/8;
                numTilesY =  m_Temp0.rt.height;
                cmd.SetComputeIntParams(cs, HDShaderIDs._Sizes, inSize, input.rt.height, outSize, output.rt.height);
            }
            cmd.DispatchCompute(cs, kernel, numTilesX, numTilesY, 1);
            cmd.RequestAsyncReadback(output, SaveTempImg);
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

            _Idx = 0u;

            int kernelFirst = sumStep.FindKernel("CSMainFirst");
            int kernel      = sumStep.FindKernel("CSMain");

            Dispatch(cmd, sumStep, kernelFirst, input, m_Temp0, sumDirection, curSize, curSize/sumPerThread);

            RTHandle inRT   = m_Temp0;
            RTHandle outRT  = m_Temp1;
            do
            {
                curSize /= sumPerThread;
                Dispatch(cmd, sumStep, kernel, inRT, outRT, sumDirection, curSize, curSize/sumPerThread);

                CoreUtils.Swap(ref inRT, ref outRT);
            } while (curSize > sumPerThread);

            kernel = sumFinalStep.FindKernel("CSMain");
            Dispatch(cmd, sumStep, kernel, inRT, m_Final, sumDirection, curSize/sumPerThread, 1);

            return m_Final;
        }
    }
}
