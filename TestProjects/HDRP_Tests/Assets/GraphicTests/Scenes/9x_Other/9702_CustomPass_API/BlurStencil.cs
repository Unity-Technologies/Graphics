using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using static UnityEngine.Rendering.HighDefinition.CustomPassUtils;

class BlurStencil : CustomPass
{
    RTHandle    stencilMaskBuffer;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        stencilMaskBuffer = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            depthBufferBits: DepthBits.Depth32, // We don't need a hig quality depth, only the stencil
            useDynamicScale: true, name: "Custom Stencil Mask"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        float radius = 8.0f;
        radius *= ctx.cameraColorBuffer.rtHandleProperties.rtHandleScale.x;

        RenderStencilFromCamera(
            ctx, ctx.hdCamera.camera, stencilMaskBuffer, ClearFlag.Depth,
            LayerMask.GetMask("Test Layer 1"), UserStencilUsage.UserBit0
        );

        using (new StencilMask(ctx, stencilMaskBuffer, UserStencilUsage.UserBit0, CompareFunction.Equal))
        {
            GaussianBlur(
                ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
                new Vector4(0.5f, 0.5f, 0.5f, 0), new Vector4(0.5f, 0.5f, 0.5f, 0),
                16, radius, 0, 0, false
            );
        }
    }

    protected override void Cleanup()
    {
        stencilMaskBuffer.Release();   
    }
}