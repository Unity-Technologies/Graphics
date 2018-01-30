namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class PyramidUtils
    {
        public static RenderTextureDescriptor CalculateRenderTextureDescriptor(HDCamera hdCamera, bool enableStereo)
        {
            var desc = hdCamera.renderTextureDesc;
            desc.depthBufferBits = 0;
            desc.useMipMap = true;
            desc.autoGenerateMips = false;

            desc.msaaSamples = 1; // These are approximation textures, they don't need MSAA

            // for stereo double-wide, each half of the texture will represent a single eye's pyramid
            //var widthModifier = 1;
            //if (stereoEnabled && (desc.dimension != TextureDimension.Tex2DArray))
            //    widthModifier = 2; // double-wide

            //desc.width = pyramidSize * widthModifier;
            desc.width = (int)hdCamera.screenSize.x;
            desc.height = (int)hdCamera.screenSize.y;

            return desc;
        }
    }
}
