using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class VrsDebugTestPass : CustomPass
{
    public float scale = 1.0f;

    private RTHandle m_ColorMaskTexture;
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // color mask
        m_ColorMaskTexture = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
            useDynamicScale: true, name: "VRS Debug Custom Pass"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!Vrs.IsColorMaskTextureConversionSupported())
        {
            Debug.LogWarning("VrsDebugTestPass: shading rate to color is not supported!");
            return;
        }

        if (m_ColorMaskTexture != null)
        {
            // SrcRect
            float w = 1.0f;
            float h = 1.0f;

            // Convert custom CPU texture to VRS texture.
            RTHandle sriTexture = ctx.shadingRateBuffer;
            if (sriTexture != null)
            {
                // Clear temp texture for clarity in case only part of the buffer is used.
                ctx.cmd.SetRenderTarget(m_ColorMaskTexture);
                CoreUtils.ClearRenderTarget(ctx.cmd, ClearFlag.Color, Color.black);

                // Convert VRS/SRI into color texture
                Vrs.ShadingRateImageToColorMaskTextureBlit(ctx.cmd, sriTexture, m_ColorMaskTexture);

                // If we render at lower resolution and use only part of the reused buffer, only blit the used part.
                w = ctx.hdCamera.actualWidth / (float)sriTexture.referenceSize.x;
                h = ctx.hdCamera.actualHeight / (float)sriTexture.referenceSize.y;
            }

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);
            Blitter.BlitQuad(ctx.cmd, m_ColorMaskTexture, new Vector4(w,h,0f,0f), new Vector4(scale, scale, 0f, 0f), 0, false);
        }
    }

    protected override void Cleanup()
    {
        m_ColorMaskTexture?.Release();
        m_ColorMaskTexture = null;
    }
}
