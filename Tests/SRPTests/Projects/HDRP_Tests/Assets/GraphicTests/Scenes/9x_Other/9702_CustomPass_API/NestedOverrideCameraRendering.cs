using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class NestedOverrideCameraRendering : CustomPass
{
    public Camera override1;
    public Camera override2;

    public LayerMask overrideMask1a;
    public LayerMask overrideMask1b;
    public LayerMask overrideMask2;

    RTHandle temp;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        temp = RTHandles.Alloc(
            Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, // We don't need alpha for this effect
            useDynamicScale: true, name: "Nested Override Camera Temp"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        CoreUtils.SetRenderTarget(ctx.cmd, temp, ClearFlag.Color);

        RenderStateBlock overrideDepth = new RenderStateBlock(RenderStateMask.Depth)
        {
            depthState = new DepthState(true, CompareFunction.LessEqual)
        };

        using (new CustomPassUtils.DisableSinglePassRendering(ctx))
        {
            using (new CustomPassUtils.OverrideCameraRendering(ctx, override1))
            {
                CustomPassUtils.DrawRenderers(ctx, overrideMask1a, overrideRenderState: overrideDepth);
                using (new CustomPassUtils.OverrideCameraRendering(ctx, override2))
                {
                    CustomPassUtils.DrawRenderers(ctx, overrideMask2);
                }
                CustomPassUtils.DrawRenderers(ctx, overrideMask1b);
            }
        }

        CustomPassUtils.Copy(ctx, temp, ctx.cameraColorBuffer, CustomPassUtils.fullScreenScaleBias, new Vector4(0.5f, 0.5f, 0.0f, 0.0f));
    }

    protected override void Cleanup()
    {
        temp.Release();
    }
}
