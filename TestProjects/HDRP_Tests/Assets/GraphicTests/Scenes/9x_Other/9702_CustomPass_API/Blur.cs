using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class Blur : CustomPass
{
    RTHandle    halfResTarget;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        halfResTarget = RTHandles.Alloc(
            Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, // We don't need alpha for this effect
            useDynamicScale: true, name: "Half Res Custom Pass"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        float radius = 8.0f;

        // make radius screen size dependent to have blur size consistent accross dimensions.
        radius *= ctx.cameraColorBuffer.rtHandleProperties.rtHandleScale.x;
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, halfResTarget,
            new Vector4(0.5f, 0.5f, 0, 0), new Vector4(0.5f, 0.5f, 0, 0),
            4, radius / 2, 0, 0, true
        );
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, halfResTarget,
            new Vector4(0.5f, 0.5f, 0.5f, 0), new Vector4(0.5f, 0.5f, 0.5f, 0),
            16, radius, 0, 0, false
        );
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0.5f, 0.5f), new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
            16, radius * 2, 0, 0, false
        );
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0, 0.5f), new Vector4(0.5f, 0.5f, 0, 0.5f),
            64, radius * 4, 0, 0, true
        );
    }

    protected override void Cleanup()
    {
        halfResTarget.Release();   
    }
}