using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class Blur : CustomPass
{
    protected override void Execute(CustomPassContext ctx)
    {
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0, 0), new Vector4(0.5f, 0.5f, 0, 0),
            4, 1, 0, 0
        );
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0.5f, 0), new Vector4(0.5f, 0.5f, 0.5f, 0),
            16, 4, 0, 0
        );
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0.5f, 0.5f), new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
            32, 8, 0, 0
        );
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, ctx.customColorBuffer.Value,
            new Vector4(0.5f, 0.5f, 0, 0.5f), new Vector4(0.5f, 0.5f, 0, 0.5f),
            64, 32, 0, 0
        );
    }
}