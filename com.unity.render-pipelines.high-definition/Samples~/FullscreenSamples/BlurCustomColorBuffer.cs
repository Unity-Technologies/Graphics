using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class BlurCustomColorBuffer : CustomPass
{
    public float    radius = 4;
    public int blurSample = 9;
    RTHandle        blurBuffer;

    protected override void Execute(CustomPassContext ctx)
    {
        if (radius > 0)
        {
            RTHandle customColorBuffer = ctx.customColorBuffer.Value;
            CustomPassUtils.GaussianBlur(ctx,customColorBuffer, customColorBuffer, blurBuffer, blurSample, radius: radius);

        }

    }

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        blurBuffer = RTHandles.Alloc(
            Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, // We don't need alpha in the blur
            useDynamicScale: true, name: "BlurBuffer"
        );
    }

     protected override void Cleanup()
    {
        blurBuffer.Release();
    }
    
}
    
    
