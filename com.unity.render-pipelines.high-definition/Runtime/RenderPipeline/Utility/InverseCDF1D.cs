using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    // CDF: Cumulative Distribution Function
    class InverseCDF1D
    {
        public enum SumDirection
        {
            Vertical,
            Horizontal
        }

        static public RTHandle ComputeInverseCDF(RTHandle cdf, RTHandle fullPDF, CommandBuffer cmd, SumDirection direction, GraphicsFormat sumFormat = GraphicsFormat.None)
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
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._CDF,      cdf);
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._Output,   invCDF);
            cmd.SetComputeTextureParam(invCDFCS, kernel, HDShaderIDs._PDF,      fullPDF);
            cmd.SetComputeIntParams   (invCDFCS,         HDShaderIDs._Sizes,
                                        cdf.rt.width, cdf.rt.height, invCDF.rt.width, invCDF.rt.height);
            int numTilesX = (invCDF.rt.width  + (8 - 1))/8;
            int numTilesY = (invCDF.rt.height + (8 - 1))/8;
            cmd.DispatchCompute(invCDFCS, kernel, numTilesX, numTilesY, 1);

            return invCDF;
        }
    }
}
