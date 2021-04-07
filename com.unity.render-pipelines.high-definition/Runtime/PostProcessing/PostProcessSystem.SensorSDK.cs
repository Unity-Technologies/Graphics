using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class PostProcessSystem
    {
        RTHandle m_InternalLogLut = null;
        internal RTHandle GenerateColorGrading(CommandBuffer cmd)
        {
            // If already created by the engine, reuse the one created or create a new one
            if (m_InternalLogLut == null)
            {
                m_InternalLogLut = RTHandles.Alloc(
                    name: "Color Grading Log Lut",
                    dimension: TextureDimension.Tex3D,
                    width: m_LutSize,
                    height: m_LutSize,
                    slices: m_LutSize,
                    depthBufferBits: DepthBits.None,
                    colorFormat: m_LutFormat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    anisoLevel: 0,
                    useMipMap: false,
                    enableRandomWrite: true
                );
            }

            // Generate/Update the logLut to apply the color grading
            var parameters = PrepareColorGradingParameters();
            DoColorGrading(parameters, m_InternalLogLut, cmd);

            return m_InternalLogLut;
        }

        internal Vector4 GetColorGradingParameters()
        { 
            // Color grading
            // This should be EV100 instead of EV but given that EV100(0) isn't equal to 1, it means
            // we can't use 0 as the default neutral value which would be confusing to users
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            return new Vector4(1f / m_LutSize, m_LutSize - 1f, postExposureLinear, 0f);
        }
    }
}
