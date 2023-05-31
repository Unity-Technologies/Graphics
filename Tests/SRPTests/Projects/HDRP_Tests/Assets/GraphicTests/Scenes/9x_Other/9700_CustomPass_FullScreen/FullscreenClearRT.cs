using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class FullscreenClearRT : CustomPass
{
    public Material material = null;
    public RenderTexture rt;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
    }

    protected override void Execute(CustomPassContext ctx)
    {
        CoreUtils.SetRenderTarget(ctx.cmd, rt, ClearFlag.Color, Color.blue);
    }

    protected override void Cleanup()
    {
    }
}
