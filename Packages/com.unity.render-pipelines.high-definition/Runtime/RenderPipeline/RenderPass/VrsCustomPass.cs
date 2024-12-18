using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Variable Rate Shading (VRS) Custom Pass
    /// </summary>
    [System.Serializable]
    public class VrsCustomPass : CustomPass
    {
        /// <summary>
        /// Color texture to convert into shading rate.
        /// 
        /// Valid colors are defined by the variable rate shading (VRS) look up table (LUT).
        /// <see cref="VrsLut"/>
        /// </summary>
        public Texture vrsColorMask;

        private Texture m_prevVrsColorMask;
        private int m_prevVrsColorMaskHash;

        private Texture2D m_VrsClearMask;
        private int m_VrsClearMaskHash;

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext">The render context</param>
        /// <param name="cmd">Current command buffer of the frame</param>
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            m_VrsClearMask = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm,
                TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);

            var lut = VrsLut.CreateDefault();
            m_VrsClearMask.SetPixel(0,0, lut[ShadingRateFragmentSize.FragmentSize1x1]);
            m_VrsClearMask.Apply(false, true);

            m_VrsClearMaskHash = CoreUtils.GetTextureHash(m_VrsClearMask);
        }

        /// <summary>
        /// Called when HDRP is destroyed.
        /// Allow you to free custom buffers.
        /// </summary>
        protected override void Cleanup()
        {
            CoreUtils.Destroy(m_VrsClearMask);
        }

        /// <summary>
        /// Generate VRS texture from a color texture.
        /// </summary>
        /// <param name="colorMaskTexture">Texture to convert to a shading rate image.</param>
        /// <param name="cmd">CommandBuffer to record conversion operations.</param>
        /// <param name="hdCamera">Camnera to get settings from.</param>
        /// <returns>Shading rate image handle, null if conversion is not possible.</returns>
        RTHandle GenerateVrsFromTexture(Texture colorMaskTexture, CommandBuffer cmd, HDCamera hdCamera)
        {
            if (colorMaskTexture == null ||
                (colorMaskTexture.dimension != TextureDimension.Tex2D && colorMaskTexture.dimension != TextureDimension.Tex2DArray))
            {
                return null;
            }

            var sriRtHandle = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Vrs);
            if (sriRtHandle == null)
                return null;

            Vrs.ColorMaskTextureToShadingRateImageDispatch(cmd, sriRtHandle, colorMaskTexture, true);
            sriRtHandle.rt.IncrementUpdateCount();

            return sriRtHandle;
        }

        /// <summary>
        /// Execute the pass with the fullscreen setup
        /// </summary>
        /// <param name="ctx">The context of the custom pass. Contains command buffer, render context, buffer, etc.</param>
        protected override void Execute(CustomPassContext ctx)
        {
            if (!Vrs.IsColorMaskTextureConversionSupported())
                return;

            bool textureChanged = m_prevVrsColorMask != vrsColorMask;
            if(vrsColorMask != null)
                textureChanged |= m_prevVrsColorMaskHash != CoreUtils.GetTextureHash(vrsColorMask);

            if (textureChanged)
            {
                if(vrsColorMask != null) // Generate VRS image from color texture
                {
                    var result = GenerateVrsFromTexture(vrsColorMask, ctx.cmd, ctx.hdCamera);
                    m_prevVrsColorMaskHash = CoreUtils.GetTextureHash(vrsColorMask);
                }
                else // Clear VRS image
                {
                    var result = GenerateVrsFromTexture(m_VrsClearMask, ctx.cmd, ctx.hdCamera);
                    m_prevVrsColorMaskHash = m_VrsClearMaskHash;
                }

                m_prevVrsColorMask = vrsColorMask;
            }
        }
    }
}
