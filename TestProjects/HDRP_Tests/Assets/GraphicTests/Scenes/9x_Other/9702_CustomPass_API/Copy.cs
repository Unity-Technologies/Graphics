using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class Copy : CustomPass
{
    RTHandle halfResTarget;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        halfResTarget = RTHandles.Alloc(
            Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, // We don't need alpha for this effect
            useDynamicScale: true, name: "Half Res Custom Pass",
            useMipMap: true, autoGenerateMips: false
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Top right
        CustomPassUtils.Copy(
            ctx, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0.5f, 0.5f), new Vector4(0.5f, 0.5f, 0, 0),
            0, 0
        );
        CustomPassUtils.Copy(
            ctx, ctx.customColorBuffer.Value, ctx.cameraColorBuffer,
            new Vector4(0.5f, 0.5f, 0, 0), new Vector4(0.25f, 0.25f, 0.75f, 0.75f),
            0, 0
        );

        // Bottom left
        CustomPassUtils.Copy(
            ctx, ctx.cameraColorBuffer, halfResTarget,
            new Vector4(0.5f, 0.5f, 0.0f, 0.5f), new Vector4(0.5f, 0.5f, 0, 0),
            0, 0
        );
        CustomPassUtils.Copy(
            ctx, halfResTarget, ctx.cameraColorBuffer,
            new Vector4(0.5f, 0.5f, 0, 0), new Vector4(0.25f, 0.25f, 0.5f, 0.5f),
            0, 0
        );

        // Bottom right
        CustomPassUtils.Copy(
            ctx, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0.0f, 0.0f), new Vector4(0.5f, 0.5f, 0, 0),
            0, 0
        );
        CustomPassUtils.Copy(
            ctx, ctx.customColorBuffer.Value, ctx.cameraColorBuffer,
            new Vector4(0.5f, 0.5f, 0, 0), new Vector4(0.25f, 0.25f, 0.75f, 0.5f),
            0, 0
        );

        // top left
        CustomPassUtils.Copy(
            ctx, ctx.cameraColorBuffer, halfResTarget,
            new Vector4(0.5f, 0.5f, 0.5f, 0.0f), new Vector4(0.5f, 0.5f, 0, 0),
            0, 1
        );
        CustomPassUtils.Copy(
            ctx, halfResTarget, ctx.cameraColorBuffer,
            new Vector4(0.5f, 0.5f, 0, 0), new Vector4(0.25f, 0.25f, 0.5f, 0.75f),
            1, 0
        );
    }

    protected override void Cleanup() => halfResTarget.Release();
}
