using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class WriteNormals : CustomPass
{
    public Material material = null;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
    }

    protected override void Execute(CustomPassContext ctx)
    {
        HDUtils.DrawFullScreen(ctx.cmd, material, ctx.cameraNormalBuffer);
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}