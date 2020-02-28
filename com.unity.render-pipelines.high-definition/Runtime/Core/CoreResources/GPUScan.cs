using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    // For Add (CommulativeAdd)
    // Texture2D NxM -> Texture2D NxM
    // For Total or MinMax
    // Texture1D 1xN -> Texture2D 1x1
    // Texture1D Nx1 -> Texture2D 1x1
    // Texture2D NxM -> Texture2D 1xM
    // Texture2D NxM -> Texture2D NxM
    class GPUScan
    {
        /// <summary>
        /// Allowed direction for GPUScan
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// Vertical (Add: Vertical Inclusive Cumulative Add, Total/MinMax: Last column of a Add/MinMax)
            /// </summary>
            Vertical,
            /// <summary>
            /// Horizontal (Add: Horizontal Inclusive Cumulative Add, Total/MinMax: Last row of a Add/MinMax)
            /// </summary>
            Horizontal
        }

        /// <summary>
        /// Allowed operation for GPUScan
        /// </summary>
        public enum Operation
        {
            /// <summary>
            /// Cumulative Add
            /// </summary>
            Add,
            /// <summary>
            /// Total, last element (rows/columns), of a cumulative Add
            /// </summary>
            Total,
            /// <summary>
            /// MinMax, last element (rows/columns), of a cumulative Add
            ///     Vertical: MinMax of each columns stored on the red (min) and the green (max) channels
            ///     Horizontal: MinMax of each rows stored on the red (min) and the green (max) channels
            /// </summary>
            MinMax
        }

        internal static int m_TileSizes = 64; // Default value

        internal static GraphicsFormat GetFormat(int channelCount, bool isFullPrecision = false)
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
        /// Compute operation
        /// </summary>
        /// <param name="input">Texture used to performe operation</param>
        /// <param name="cmd">Commande Buffer: null supported for immediate context</param>
        /// <param name="operation">Operation needed</param>
        /// <param name="direction">Direction to performe the scan</param>
        /// <param name="outFormat">Format used for the output, if not setted, use the same precision as the input</param>
        /// <returns>A RTHandle which contains the results of the performed scan. The lifetime of the RTHandle is managed by the user (cf. RTHandleDeleter.ScheduleRelease(...), myRTHandle.Release()).</returns>
        static public RTHandle ComputeOperation(RTHandle input, CommandBuffer cmd, Operation operation, Direction direction, GraphicsFormat outFormat = GraphicsFormat.None)
        {
            if (input == null)
            {
                return null;
            }

            int width  = input.rt.width;
            int height = input.rt.height;

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader scanCS = hdrp.renderPipelineResources.shaders.gpuScanCS;

            bool isFullPrecision;
            if (HDUtils.GetFormatMaxPrecisionBits(input.rt.graphicsFormat) == 32)
            {
                isFullPrecision = true;
            }
            else
            {
                isFullPrecision = false;
            }

            GraphicsFormat format2 = GetFormat(2, isFullPrecision);

            GraphicsFormat format;
            if (outFormat == GraphicsFormat.None)
            {
                if (operation == Operation.MinMax)
                    format = format2;
                else
                    format = input.rt.graphicsFormat;
            }
            else
            {
                format = outFormat;
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

            Debug.Assert((operation == Operation.MinMax && (channelsCount == 1 || channelsCount == 2)) ||
                          operation == Operation.Add || operation == Operation.Total);

            string preAddOn = "";
            switch(operation)
            {
            case Operation.Add:
            case Operation.Total:
                    addon += "Add";
            break;
            case Operation.MinMax:
                //if (channelsCount == 1)
                preAddOn = "First";
                addon += "MinMax";
            break;
            }

            string dirAddOn;
            if (direction == Direction.Vertical)
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
            if (direction == Direction.Horizontal)
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
                if (direction == Direction.Horizontal)
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
                if (direction == Direction.Horizontal)
                {
                    output = RTHandles.Alloc(1, height, colorFormat: format, enableRandomWrite: true);
                    RTHandleDeleter.ScheduleRelease(cdf);
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
                    RTHandleDeleter.ScheduleRelease(cdf);
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
