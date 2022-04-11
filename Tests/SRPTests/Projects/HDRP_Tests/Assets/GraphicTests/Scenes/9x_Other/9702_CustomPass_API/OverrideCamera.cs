using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Linq;

class OverrideCamera : CustomPass
{
    public Camera customCamera0 = null;
    public Camera customCamera1 = null;
    public Camera customCamera2 = null;
    public Camera customCamera3 = null;
    public Camera customCamera4 = null;

    RTHandle temp;
    RTHandle halfResColor;
    RTHandle halfResDepth;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        temp = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, // We don't need alpha for this effect
            useDynamicScale: true, name: "Override Camera Temp"
        );
        halfResColor = RTHandles.Alloc(
            Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, // We don't need alpha for this effect
            useDynamicScale: true, name: "Override Camera Temp"
        );
        halfResDepth = RTHandles.Alloc(
            Vector2.one * 0.5f, TextureXR.slices, DepthBits.Depth16, // 16Bits for half res target is enough
            dimension: TextureXR.dimension, useDynamicScale: true, name: "Override Camera Temp"
        );
    }

    // In a real application we should override the culling params and aggregate all the cameras

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        => cullingParameters.cullingMask = ~0u;

    protected override void Execute(CustomPassContext ctx)
    {
        if (customCamera0 == null || customCamera1 == null || customCamera2 == null || customCamera3 == null || customCamera4 == null)
            return;

        // Render from camera 0
        // Internal API, can't be tested right now
        // using (new HDRenderPipeline.OverrideCameraRendering(ctx.cmd, customCamera0))
        // {
        //     CoreUtils.SetRenderTarget(ctx.cmd, temp, ClearFlag.Color);
        //     CustomPassUtils.DrawRenderers(ctx, -1);
        // }
        // CustomPassUtils.Copy(
        //     ctx, temp, ctx.cameraColorBuffer,
        //     CustomPassUtils.fullScreenScaleBias,
        //     new Vector4(.5f, .5f, 0f, 0f)
        // );

        RenderStateBlock overrideDepth = new RenderStateBlock(RenderStateMask.Depth)
        {
            depthState = new DepthState(true, CompareFunction.LessEqual)
        };

        // Render from camera 1
        CustomPassUtils.RenderFromCamera(ctx, customCamera1, temp, ctx.customDepthBuffer.Value, ClearFlag.All, -1, overrideRenderState: overrideDepth);
        CustomPassUtils.Copy(
            ctx, temp, ctx.cameraColorBuffer,
            CustomPassUtils.fullScreenScaleBias,
            new Vector4(.5f, .5f, .5f, 0f)
        );

        // Render from camera 4 (at same position than the test camera but uses a different FoV)
        // And with the camera depth buffer (which already contains opaque objects)
        CustomPassUtils.RenderFromCamera(ctx, customCamera4, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None, customCamera4.cullingMask, overrideRenderState: overrideDepth);

        // Render from camera 3 using different buffers
        CustomPassUtils.RenderDepthFromCamera(ctx, customCamera3, temp, ctx.customDepthBuffer.Value, ClearFlag.All, -1);
        CustomPassUtils.Copy(
            ctx, temp, ctx.cameraColorBuffer,
            CustomPassUtils.fullScreenScaleBias,
            new Vector4(.25f, .25f, .5f, .5f)
        );

        CustomPassUtils.RenderNormalFromCamera(ctx, customCamera3, temp, ctx.customDepthBuffer.Value, ClearFlag.All, -1);
        CustomPassUtils.Copy(
            ctx, temp, ctx.cameraColorBuffer,
            CustomPassUtils.fullScreenScaleBias,
            new Vector4(.25f, .25f, .75f, .5f)
        );

        CustomPassUtils.RenderTangentFromCamera(ctx, customCamera3, temp, ctx.customDepthBuffer.Value, ClearFlag.All, -1);
        CustomPassUtils.Copy(
            ctx, temp, ctx.cameraColorBuffer,
            CustomPassUtils.fullScreenScaleBias,
            new Vector4(.25f, .25f, .5f, .75f)
        );

        // Render from camera 2 in an half res buffer
        CustomPassUtils.RenderFromCamera(ctx, customCamera2, halfResColor, halfResDepth, ClearFlag.All, -1, overrideRenderState: overrideDepth);
        CustomPassUtils.Copy(
            ctx, halfResColor, ctx.cameraColorBuffer,
            CustomPassUtils.fullScreenScaleBias,
            new Vector4(.25f, .25f, .75f, .75f)
        );
    }

    protected override void Cleanup()
    {
        temp.Release();
        halfResColor.Release();
        halfResDepth.Release();
    }
}
