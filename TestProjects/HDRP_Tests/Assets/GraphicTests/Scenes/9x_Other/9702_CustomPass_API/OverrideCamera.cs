using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class OverrideCamera : CustomPass
{
    public Camera       customCamera0 = null;
    public Camera       customCamera1 = null;
    public Camera       customCamera2 = null;
    public Camera       customCamera3 = null;

    RTHandle            temp;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        temp = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, // We don't need alpha for this effect
            useDynamicScale: true, name: "Override Camera Temp"
        );
    }

    // In a real application we should override the culling params and aggregate all the cameras

    protected override void Execute(CustomPassContext ctx)
    {
        if (customCamera0 == null || customCamera1 == null || customCamera2 == null || customCamera3 == null)
            return;

        // Render from camera 0
        using (new HDRenderPipeline.OverrideCameraRendering(ctx.cmd, customCamera0))
        {
            CoreUtils.SetRenderTarget(ctx.cmd, temp, ClearFlag.Color);
            CustomPassUtils.DrawRenderers(ctx, -1);
        }
        CustomPassUtils.Copy(
            ctx, temp, ctx.cameraColorBuffer,
            CustomPassUtils.fullScreenScaleBias,
            new Vector4(.5f, .5f, 0f, 0f)
        );

        // Render from camera 1
        using (new HDRenderPipeline.OverrideCameraRendering(ctx.cmd, customCamera1))
        {
            CoreUtils.SetRenderTarget(ctx.cmd, temp, ClearFlag.Color);
            CustomPassUtils.DrawRenderers(ctx, -1);
        }
        CustomPassUtils.Copy(
            ctx, temp, ctx.cameraColorBuffer,
            CustomPassUtils.fullScreenScaleBias,
            new Vector4(.5f, .5f, .5f, 0f)
        );
    }

    protected override void Cleanup() => temp.Release();
}