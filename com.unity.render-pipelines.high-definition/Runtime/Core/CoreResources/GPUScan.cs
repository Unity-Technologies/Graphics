using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    // Texture1D 1xN -> Texture2D 1x1
    // Texture1D Nx1 -> Texture2D 1x1
    // Texture2D NxM -> Texture2D 1xM
    // Texture2D NxM -> Texture2D NxM
    class GPUScan
    {
        public enum Direction
        {
            Vertical,
            Horizontal
        }

        public enum Operation
        {
            Add,
            Total,
            MinMax
        }

        internal static int m_TileSizes = 64;

        //static private void Dispatch(CommandBuffer cmd, ComputeShader cs, int kernel, RTHandle output, RTHandle input, Direction direction, int inSize, int outSize, int opPerThread)
        //{
        //    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Input,  input);
        //    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Output, output);
        //    int numTilesX;
        //    int numTilesY;
        //    if (direction == Direction.Vertical)
        //    {
        //        numTilesX =  output.rt.width;
        //        numTilesY = (outSize + (m_TileSizes - 1))/m_TileSizes;
        //        cmd.SetComputeIntParams(cs, HDShaderIDs._Sizes, input.rt.width, inSize, output.rt.width, outSize);
        //    }
        //    else
        //    {
        //        numTilesX = (outSize + (m_TileSizes - 1))/m_TileSizes;
        //        numTilesY =  output.rt.height;
        //        cmd.SetComputeIntParams(cs, HDShaderIDs._Sizes, inSize, input.rt.height, outSize, output.rt.height);
        //    }
        //    cmd.SetComputeIntParam  (cs, HDShaderIDs._Iteration, opPerThread);
        //    cmd.DispatchCompute     (cs, kernel, numTilesX, numTilesY, 1);
        //}

        static public RTHandle ComputeOperation(RTHandle input, CommandBuffer cmd, Operation operation, Direction opDirection, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            if (input == null)
            {
                return null;
            }

            int width  = input.rt.width;
            int height = input.rt.height;

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader scanCS = hdrp.renderPipelineResources.shaders.gpuScanCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
            {
                format = input.rt.graphicsFormat;
            }
            else
            {
                format = sumFormat;
            }

            RTHandle temp0  = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            RTHandle temp1  = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            RTHandle cdf    = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(temp0);
            RTHandleDeleter.ScheduleRelease(temp1);

            uint iteration;
            string addon;

            int channelsCount = HDUtils.GetFormatChannelsCount(format);
            if (channelsCount == 1)
            {
                addon = "1";
            }
            else if (channelsCount == 2)
            {
                addon = "2";
            }
            else
            {
                addon = "4";
            }

            Debug.Assert((operation == Operation.MinMax && HDUtils.GetFormatChannelsCount(input.rt.graphicsFormat) == 1) ||
                         operation == Operation.Add || operation == Operation.Total);

            string preAddOn = "";
            switch(operation)
            {
            case Operation.Add:
            case Operation.Total:
                    addon += "Add";
            break;
            case Operation.MinMax:
                preAddOn = "First";
                addon += "MinMax";
            break;
            }

            string dirAddOn;
            if (opDirection == Direction.Vertical)
            {
                dirAddOn = "V";
                iteration = (uint)Mathf.Log((float)height, 2.0f);
            }
            else
            {
                dirAddOn = "H";
                iteration = (uint)Mathf.Log((float)width,  2.0f);
            }

            int numTilesX;
            int numTilesY;

            int kernel = scanCS.FindKernel("CSMainFloat" + addon + preAddOn + dirAddOn);
            if (cmd != null)
            {
                cmd.SetComputeTextureParam(scanCS, kernel, HDShaderIDs._Input,      input);
                cmd.SetComputeTextureParam(scanCS, kernel, HDShaderIDs._Output,     temp0);
                cmd.SetComputeIntParam    (scanCS,         HDShaderIDs._Iteration,  0);
                cmd.SetComputeIntParams   (scanCS,         HDShaderIDs._Sizes,
                                           input.rt.width, input.rt.height, temp0.rt.width, temp0.rt.height);
            }
            else
            {
                scanCS.SetTexture(kernel, HDShaderIDs._Input,      input);
                scanCS.SetTexture(kernel, HDShaderIDs._Output,     temp0);
                scanCS.SetInt    (HDShaderIDs._Iteration,  0);
                scanCS.SetInts   (HDShaderIDs._Sizes,
                                              input.rt.width, input.rt.height, temp0.rt.width, temp0.rt.height);
            }
            if (opDirection == Direction.Horizontal)
            {
                numTilesX = (temp0.rt.width  + (m_TileSizes - 1))/m_TileSizes;
                numTilesY =  temp0.rt.height;
            }
            else
            {
                numTilesX =  temp0.rt.width;
                numTilesY = (temp0.rt.height + (m_TileSizes - 1))/m_TileSizes;
            }
            if (cmd != null)
                cmd.DispatchCompute(scanCS, kernel, numTilesX, numTilesY, 1);
            else
                scanCS.Dispatch(kernel, numTilesX, numTilesY, 1);

            // Loop
            kernel = scanCS.FindKernel("CSMainFloat" + addon + dirAddOn);
            RTHandle ping = temp0;
            RTHandle pong = temp1;
            for (uint i = 0; i < iteration; ++i)
            {
                if (cmd != null)
                {
                    cmd.SetComputeTextureParam(scanCS, kernel, HDShaderIDs._Input,      ping);
                    cmd.SetComputeTextureParam(scanCS, kernel, HDShaderIDs._Output,     pong);
                    cmd.SetComputeIntParam    (scanCS,         HDShaderIDs._Iteration,  (int)Mathf.Pow(2.0f, (float)i));
                    cmd.SetComputeIntParams   (scanCS,         HDShaderIDs._Sizes,
                                               ping.rt.width, input.rt.height, pong.rt.width, pong.rt.height);
                }
                else
                {
                    scanCS.SetTexture(kernel, HDShaderIDs._Input,      ping);
                    scanCS.SetTexture(kernel, HDShaderIDs._Output,     pong);
                    scanCS.SetInt    (HDShaderIDs._Iteration,  (int)Mathf.Pow(2.0f, (float)i));
                    scanCS.SetInts   (HDShaderIDs._Sizes,
                                                  ping.rt.width, input.rt.height, pong.rt.width, pong.rt.height);
                }
                if (opDirection == Direction.Horizontal)
                {
                    numTilesX = (pong.rt.width  + (m_TileSizes - 1))/m_TileSizes;
                    numTilesY =  pong.rt.height;
                }
                else
                {
                    numTilesX =  pong.rt.width;
                    numTilesY = (pong.rt.height + (m_TileSizes - 1))/m_TileSizes;
                }
                if (cmd != null)
                {
                    cmd.DispatchCompute(scanCS, kernel, numTilesX, numTilesY, 1);
                }
                else
                {
                    scanCS.Dispatch(kernel, numTilesX, numTilesY, 1);
                }
                if (i == iteration - 1)
                {
                    cdf = pong;
                }
                CoreUtils.Swap(ref ping, ref pong);
            }

            if (operation == Operation.Add)
            {
                return cdf;
            }
            else if (operation == Operation.Total || operation == Operation.MinMax)
            {
                RTHandle output;
                if (opDirection == Direction.Horizontal)
                {
                    output = RTHandles.Alloc(1, height, colorFormat: format, enableRandomWrite: true);
                    if (cmd != null)
                    {
                        cmd.CopyTexture(cdf, 0, 0, width - 1, 0, 1, height, output, 0, 0, 0, 0);
                    }
                    else
                    {
                        Graphics.CopyTexture(cdf, 0, 0, width - 1, 0, 1, height, output, 0, 0, 0, 0);
                    }
                }
                else
                {
                    output = RTHandles.Alloc(width, 1, colorFormat: format, enableRandomWrite: true);
                    if (cmd != null)
                    {
                        cmd.CopyTexture(cdf, 0, 0, 0, height - 1, width, 1, output, 0, 0, 0, 0);
                    }
                    else
                    {
                        Graphics.CopyTexture(cdf, 0, 0, 0, height - 1, width, 1, output, 0, 0, 0, 0);
                    }
                }

                return output;
            }
            else
            {
                return null;
            }
        }
    }
}
