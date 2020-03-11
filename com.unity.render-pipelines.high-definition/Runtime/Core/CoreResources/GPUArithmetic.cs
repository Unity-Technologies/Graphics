using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    class GPUArithmetic
    {
        /// <summary>
        /// Allowed operation for GPUArithmetic
        /// </summary>
        public enum Operation
        {
            /// <summary>
            /// Addition
            /// </summary>
            Add,
            /// <summary>
            /// Multiply
            /// </summary>
            Mult,
            /// <summary>
            /// Divide
            /// </summary>
            Div,
            /// <summary>
            /// RGB Mean
            /// </summary>
            Mean,
            /// <summary>
            /// MAD: Multiply and Addition (a*x + b)
            /// </summary>
            MAD,
            /// <summary>
            /// MAD_RB: Multiply and Addition (with each needed informations are stored on red & green channels: in.r*x + in.b)
            /// </summary>
            MAD_RG
        }

        /// <summary>
        /// Compute operation
        /// </summary>
        /// <param name="output">Output (Internally supported: +=, *=, /= ...)</param>
        /// <param name="input">Input (Internally supported: +=, *=, /= ...)</param>
        /// <param name="paramsRT">Parameters for add, mult, ... {paramsRT[uint2(0, 0)], paramsRT[uint2(1, 0)]}, or paramsRT[uint2(0, 0)].xy</param>
        /// <param name="cmd">Command Buffer (can be null for immediate context)</param>
        /// <param name="operation">Supported {Add: output = input + param, Mult: output = input*param, Div: output = input/param, Mean: output = dot(input, float3(1.0f/3.0f).xxx), MAD: output = param[0]*input + param[1], MAD_RG: output = param[0].x*input + param[0].y}</param>
        static public void ComputeOperation(RTHandle output, RTHandle input, RTHandle paramsRT, CommandBuffer cmd, Operation operation)
        {
            Debug.Assert(input != null);
            Debug.Assert(output != null);
            Debug.Assert(operation == Operation.Mean || paramsRT != null);
            Debug.Assert(output.rt.width == input.rt.width && output.rt.height == input.rt.height);

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

        /// <summary>
        /// Compute operation
        /// </summary>
        /// <param name="output">Output (Internally supported: +=, *=, /= ...)</param>
        /// <param name="input">Input (Internally supported: +=, *=, /= ...)</param>
        /// <param name="params">Parameters for add, mult, ... {paramsRT[uint2(0, 0)], paramsRT[uint2(1, 0)]}, or paramsRT[uint2(0, 0)].xy</param>
        /// <param name="cmd">Command Buffer (can be null for immediate context)</param>
        /// <param name="operation">Supported {Add: output = input + param, Mult: output = input*param, Div: output = input/param, Mean: output = dot(input, float3(1.0f/3.0f).xxx), MAD: output = param[0]*input + param[1], MAD_RG: output = param[0].x*input + param[0].y}</param>
        static public void ComputeOperation(RTHandle output, RTHandle input, Vector4 param, CommandBuffer cmd, Operation operation)
        {
            Debug.Assert(input != null);
            Debug.Assert(output != null);
            Debug.Assert(output.rt.width == input.rt.width && output.rt.height == input.rt.height);

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
                addon += "AddVal";
            break;
            case Operation.Mult:
                addon += "MultVal";
            break;
            case Operation.Div:
                addon += "DivVal";
            break;
            case Operation.Mean:
                addon += "MeanVal";
            break;
            case Operation.MAD:
                addon += "MADVal";
            break;
            case Operation.MAD_RG:
                addon += "MAD_RGVal";
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
                    cmd.SetComputeVectorParam (arithmeticsCS,         HDShaderIDs._InputVal, param);
                    cmd.SetComputeIntParams   (arithmeticsCS, HDShaderIDs._Sizes,
                                               input.rt.width, input.rt.height, input.rt.width, input.rt.height);
                    cmd.DispatchCompute(arithmeticsCS, kernel, numTilesX, numTilesY, 1);
                }
                else
                {
                    cmd.SetComputeTextureParam(arithmeticsCS, kernel, HDShaderIDs._Output,   output);
                    cmd.SetComputeTextureParam(arithmeticsCS, kernel, HDShaderIDs._Input,    input);
                    cmd.SetComputeVectorParam (arithmeticsCS,         HDShaderIDs._InputVal, param);
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
                    arithmeticsCS.SetVector (        HDShaderIDs._InputVal, param);
                    arithmeticsCS.SetInts   (        HDShaderIDs._Sizes,
                                               input.rt.width, input.rt.height, input.rt.width, input.rt.height);
                    arithmeticsCS.Dispatch  (kernel, numTilesX, numTilesY, 1);
                }
                else
                {
                    arithmeticsCS.SetTexture(kernel, HDShaderIDs._Output,   output);
                    arithmeticsCS.SetTexture(kernel, HDShaderIDs._Input,    input);
                    arithmeticsCS.SetVector (        HDShaderIDs._InputVal, param);
                    arithmeticsCS.SetInts   (        HDShaderIDs._Sizes,
                                               input.rt.width, input.rt.height, input.rt.width, input.rt.height);
                    arithmeticsCS.Dispatch  (kernel, numTilesX, numTilesY, 1);
                }
            }
        }
    }
}
