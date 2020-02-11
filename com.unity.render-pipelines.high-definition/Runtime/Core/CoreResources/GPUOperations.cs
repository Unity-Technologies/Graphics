using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    // Texture1D 1xN -> Texture2D 1x1
    // Texture1D Nx1 -> Texture2D 1x1
    // Texture2D NxM -> Texture2D 1xM
    // Texture2D NxM -> Texture2D NxM
    class GPUOperation
    {
        public enum Direction
        {
            Vertical,
            Horizontal
        }

        public enum Operation
        {
            Sum,
            MinMax
        }

        static private void Dispatch(CommandBuffer cmd, ComputeShader cs, int kernel, RTHandle output, RTHandle input, Direction direction, int inSize, int outSize, int opPerThread)
        {
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Input,  input);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Output, output);
            int numTilesX;
            int numTilesY;
            if (direction == Direction.Vertical)
            {
                numTilesX =  output.rt.width;
                numTilesY = (outSize + (8 - 1))/8;
                cmd.SetComputeIntParams(cs, HDShaderIDs._Sizes, input.rt.width, inSize, output.rt.width, outSize);
            }
            else
            {
                numTilesX = (outSize + (8 - 1))/8;
                numTilesY =  output.rt.height;
                cmd.SetComputeIntParams(cs, HDShaderIDs._Sizes, inSize, input.rt.height, outSize, output.rt.height);
            }
            cmd.SetComputeIntParam  (cs, HDShaderIDs._Iteration, opPerThread);
            cmd.DispatchCompute     (cs, kernel, numTilesX, numTilesY, 1);
        }

        static public RTHandle ComputeOperation(RTHandle input, CommandBuffer cmd, Operation operation, Direction opDirection, int opPerThread = 8, bool isPDF = false, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            Debug.Assert(input != null);
            Debug.Assert(opPerThread >= 1 && opPerThread <= 64);
            Debug.Assert(opPerThread%2 == 0 || opPerThread == 1);

            RTHandle temp0 = null;
            RTHandle temp1 = null;
            RTHandle final = null;

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader opStep = hdrp.renderPipelineResources.shaders.gpuOperationsCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
                format = GraphicsFormat.R16G16_SFloat;
            else
                format = sumFormat;

            int width  = input.rt.width;
            int height = input.rt.height;

            int outWidth;
            int outHeight;
            bool singlePass = false;
            if (opDirection == Direction.Vertical)
            {
                outWidth  = width;
                outHeight = 1;

                if (height/(opPerThread*opPerThread) > 0)
                {
                    temp0 = RTHandles.Alloc(outWidth, height/opPerThread,               colorFormat: format, enableRandomWrite: true);
                    temp1 = RTHandles.Alloc(outWidth, height/(opPerThread*opPerThread), colorFormat: format, enableRandomWrite: true);
                }
                else
                {
                    singlePass  = true;
                    opPerThread = height;
                }
            }
            else // if (opDirection == SumDirection.Horizontal)
            {
                outWidth  = 1;
                outHeight = height;

                if (width/(opPerThread*opPerThread) > 0)
                {
                    temp0 = RTHandles.Alloc(width/opPerThread,               outHeight, colorFormat: format, enableRandomWrite: true);
                    temp1 = RTHandles.Alloc(width/(opPerThread*opPerThread), outHeight, colorFormat: format, enableRandomWrite: true);
                }
                else
                {
                    singlePass  = true;
                    opPerThread = width;
                }
            }

            Debug.Assert(singlePass == true || (singlePass == false && temp0.rt.width != 0 && temp0.rt.height != 0));
            Debug.Assert(singlePass == true || (singlePass == false && temp1.rt.width != 0 && temp1.rt.height != 0));

            string addon = "";
            switch (operation)
            {
                case Operation.Sum:
                    addon += "Sum";
                    break;
                case Operation.MinMax:
                    addon += "MinMax";
                    break;
            };

            string strDir = "";
            int curSize;
            if (opDirection == Direction.Vertical)
            {
                curSize = height;
                strDir += "V";
                final = RTHandles.Alloc(width: outWidth, height: 1, colorFormat: format, enableRandomWrite: true);
            }
            else
            {
                curSize = width;
                strDir += "H";
                final = RTHandles.Alloc(width: 1, height: outHeight, colorFormat: format, enableRandomWrite: true);
            }
            string firstAddon = "";
            //if (isPDF)
            //    firstAddon += "PDF";

            int kernelFirst = opStep.FindKernel("CSMain" + firstAddon + addon + "First" + strDir);
            int kernel      = opStep.FindKernel("CSMain" + addon + strDir);

            Dispatch(cmd, opStep, kernelFirst,
                     opPerThread == 1 || singlePass ? final : temp0,
                     input,
                     opDirection,
                     curSize,
                     curSize/opPerThread,
                     opPerThread);

            if (singlePass == false && opPerThread > 1)
            {
                RTHandle inRT   = temp0;
                RTHandle outRT  = temp1;

                do
                {
                    curSize /= opPerThread;
                    Dispatch(cmd, opStep, kernel, outRT, inRT, opDirection, curSize, curSize/opPerThread, opPerThread);

                    CoreUtils.Swap(ref inRT, ref outRT);
                } while (curSize > opPerThread);

                Dispatch(cmd, opStep, kernel, final, inRT, opDirection, curSize/opPerThread, 1, opPerThread);
            }

            RTHandleDeleter.ScheduleRelease(temp0);
            RTHandleDeleter.ScheduleRelease(temp1);

            return final;
        }
    }
}
