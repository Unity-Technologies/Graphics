using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    // CDF: Cumulative Distribution Function
    class ComputeCDF1D
    {
        public enum SumDirection
        {
            Vertical,
            Horizontal
        }

        static public RTHandle ComputeCDF(RTHandle input, CommandBuffer cmd, SumDirection direction, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            if (input == null)
            {
                return null;
            }

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader cdfStep = hdrp.renderPipelineResources.shaders.cdf1DCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
                format = GraphicsFormat.R16G16B16A16_SFloat;
            else
                format = sumFormat;

            int width  = input.rt.width;
            int height = input.rt.height;

            RTHandle temp0  = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            RTHandle temp1  = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            RTHandle cdf    = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);
            RTHandleDeleter.ScheduleRelease(temp0);
            RTHandleDeleter.ScheduleRelease(temp1);

            uint iteration;
            string addon;
            if (direction == SumDirection.Vertical)
            {
                addon = "V";
                iteration = (uint)Mathf.Log((float)height, 2.0f);
            }
            else
            {
                addon = "H";
                iteration = (uint)Mathf.Log((float)width,  2.0f);
            }

            // RGB to Greyscale
            int kernel = cdfStep.FindKernel("CSMainFirst" + addon);
            cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Input,     input);
            cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Output,    temp0);
            cmd.SetComputeIntParams   (cdfStep,         HDShaderIDs._Sizes,
                                       input.rt.width, input.rt.height, temp0.rt.width, temp0.rt.height);
            int numTilesX;
            int numTilesY;
            if (direction == SumDirection.Horizontal)
            {
                numTilesX = (temp0.rt.width  + (8 - 1))/8;
                numTilesY =  temp0.rt.height;
            }
            else
            {
                numTilesX =  temp0.rt.width;
                numTilesY = (temp0.rt.height + (8 - 1))/8;
            }
            cmd.DispatchCompute     (cdfStep, kernel, numTilesX, numTilesY, 1);

            // Loop
            kernel = cdfStep.FindKernel("CSMain" + addon);
            RTHandle ping = temp0;
            RTHandle pong = temp1;
            for (uint i = 0; i < iteration; ++i)
            {
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Input,     ping);
                cmd.SetComputeTextureParam(cdfStep, kernel, HDShaderIDs._Output,    pong);
                cmd.SetComputeIntParams   (cdfStep,         HDShaderIDs._Sizes,
                                           ping.rt.width, input.rt.height, pong.rt.width, pong.rt.height);
                cmd.SetComputeIntParam    (cdfStep,         HDShaderIDs._Iteration, (int)Mathf.Pow(2.0f, (float)i));
                if (direction == SumDirection.Horizontal)
                {
                    numTilesX = (pong.rt.width  + (8 - 1))/8;
                    numTilesY =  pong.rt.height;
                }
                else
                {
                    numTilesX =  pong.rt.width;
                    numTilesY = (pong.rt.height + (8 - 1))/8;
                }
                cmd.DispatchCompute(cdfStep, kernel, numTilesX, numTilesY, 1);
                if (i == iteration - 1)
                {
                    cdf = pong;
                }
                CoreUtils.Swap(ref ping, ref pong);
            }

            return cdf;
        }

        static public RTHandle ComputeInverseCDF(RTHandle cdf, RTHandle fullPDF, Vector4 integral, CommandBuffer cmd, SumDirection direction, GraphicsFormat sumFormat = GraphicsFormat.None)
        {
            if (cdf == null)
            {
                return null;
            }

            var hdrp = HDRenderPipeline.defaultAsset;
            ComputeShader invCDFCS = hdrp.renderPipelineResources.shaders.inverseCDF1DCS;

            GraphicsFormat format;
            if (sumFormat == GraphicsFormat.None)
                format = GraphicsFormat.R16G16B16A16_SFloat;
            else
                format = sumFormat;

            int width  = cdf.rt.width;
            int height = cdf.rt.height;

            RTHandle invCDF = RTHandles.Alloc(width, height, colorFormat: format, enableRandomWrite: true);

            string addon;
            if (direction == SumDirection.Vertical)
            {
                addon = "V";
            }
            else
            {
                addon = "H";
            }

            // RGB to Greyscale
            int kernel = invCDFCS.FindKernel("CSMain" + addon);
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._Input,    cdf);
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._Output,   invCDF);
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._PDF,      fullPDF);
            cmd.SetComputeVectorParam (invCDFCS,         HDShaderIDs._Integral,
                                        new Vector4(integral.x, integral.y, integral.z, 1.0f/Mathf.Max(integral.x, integral.y, integral.z)));
            cmd.SetComputeIntParams   (invCDFCS,         HDShaderIDs._Sizes,
                                        cdf.rt.width, cdf.rt.height, invCDF.rt.width, invCDF.rt.height);
            int numTilesX = (invCDF.rt.width  + (8 - 1))/8;
            int numTilesY = (invCDF.rt.height + (8 - 1))/8;
            cmd.DispatchCompute(invCDFCS, kernel, numTilesX, numTilesY, 1);

            return invCDF;
        }
    }
}
