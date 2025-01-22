using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class CopyColorToCustom_9703 : CustomPass
{
    protected override void Execute(CustomPassContext ctx)
    {
        for (int i = 0; i < ctx.cameraColorBuffer.rt.volumeDepth; i++)
            ctx.cmd.CopyTexture(ctx.cameraColorBuffer, i, 0, ctx.customColorBuffer.Value, i, 0);
    }
}
