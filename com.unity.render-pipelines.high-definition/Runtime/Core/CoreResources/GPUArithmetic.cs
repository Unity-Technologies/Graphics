using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    class GPUArithmetic
    {
        public enum Operation
        {
            Add,
            Mult,
            Div,
            Mean,
            MAD, // MulAdd
            MAD_RG // MulAdd with multiplier stored in the red channel and the adder stored in the green channel
        }

        static public void ComputeOperation(RTHandle output, RTHandle input, RTHandle paramsRT, CommandBuffer cmd, Operation operation)
        {
            Debug.Assert(input != null);
            Debug.Assert(operation == Operation.Mean || paramsRT != null);

            Debug.Assert(output == null || (output.rt.width == input.rt.width && output.rt.height == input.rt.height));

            string addon = "";
            bool self = false;
            if (input == output)
            {
                self = true;
                addon += "Self";
            }

            int width  = input.rt.width;
            int height = input.rt.height;

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader arithmeticsCS = hdrp.renderPipelineResources.shaders.gpuArithmeticsCS;

            switch(operation)
            {
            case Operation.Add:
                addon += "Add";
            break;
            case Operation.Mult:
                addon += "Mult";
            break;
            case Operation.Div:
                addon += "Div";
            break;
            case Operation.Mean:
                addon += "Mean";
            break;
            case Operation.MAD:
                addon += "MAD";
            break;
            case Operation.MAD_RG:
                addon += "MAD_RG";
            break;
            }

            int numTilesX = (width  + (8 - 1))/8;
            int numTilesY = (height + (8 - 1))/8;

            int kernel = arithmeticsCS.FindKernel("CSMain" + addon);
            if (cmd != null)
            {
                if (self)
                {
                    cmd.SetComputeTextureParam(arithmeticsCS, kernel, HDShaderIDs._Output,   input);
                    if (paramsRT != null)
                        cmd.SetComputeTextureParam(arithmeticsCS, kernel, HDShaderIDs._InputVal, paramsRT);
                    cmd.SetComputeIntParams   (arithmeticsCS, HDShaderIDs._Sizes,
                                               input.rt.width, input.rt.height, input.rt.width, input.rt.height);
                    cmd.DispatchCompute(arithmeticsCS, kernel, numTilesX, numTilesY, 1);
                }
                else
                {
                    cmd.SetComputeTextureParam(arithmeticsCS, kernel, HDShaderIDs._Output,   output);
                    cmd.SetComputeTextureParam(arithmeticsCS, kernel, HDShaderIDs._Input,    input);
                    if (paramsRT != null)
                        cmd.SetComputeTextureParam(arithmeticsCS, kernel, HDShaderIDs._InputVal, paramsRT);
                    cmd.SetComputeIntParams   (arithmeticsCS, HDShaderIDs._Sizes,
                                               input.rt.width, input.rt.height, input.rt.width, input.rt.height);
                    cmd.DispatchCompute(arithmeticsCS, kernel, numTilesX, numTilesY, 1);
                }
            }
            else
            {
                if (self)
                {
                    arithmeticsCS.SetTexture(kernel, HDShaderIDs._Output,   input);
                    if (paramsRT != null)
                        arithmeticsCS.SetTexture(kernel, HDShaderIDs._InputVal, paramsRT);
                    arithmeticsCS.SetInts   (HDShaderIDs._Sizes,
                                               input.rt.width, input.rt.height, input.rt.width, input.rt.height);
                    arithmeticsCS.Dispatch  (kernel, numTilesX, numTilesY, 1);
                }
                else
                {
                    arithmeticsCS.SetTexture(kernel, HDShaderIDs._Output,   output);
                    arithmeticsCS.SetTexture(kernel, HDShaderIDs._Input,    input);
                    if (paramsRT != null)
                        arithmeticsCS.SetTexture(kernel, HDShaderIDs._InputVal, paramsRT);
                    arithmeticsCS.SetInts   (HDShaderIDs._Sizes,
                                               input.rt.width, input.rt.height, input.rt.width, input.rt.height);
                    arithmeticsCS.Dispatch  (kernel, numTilesX, numTilesY, 1);
                }
            }
        }
    }
}
