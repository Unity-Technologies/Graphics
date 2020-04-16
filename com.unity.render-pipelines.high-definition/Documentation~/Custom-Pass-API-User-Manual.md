# Custom Pass Utils API User Manual

## Blur

### Gaussian Blur

The gaussian blur function will allow you to blur an image with an arbitrary radius and quality (number of samples). For performance reasons, you also have the possibility to run the blur kernel after a downsample pass.

Here's an example of Custom Pass that blurs the camera color buffer:

```CSharp
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class GaussianBlur : CustomPass
{
    RTHandle    halfResTarget;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        halfResTarget = RTHandles.Alloc(
            // Note the * 0.5 here, it will allocate a half res target, which saves a lot of memory
            Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
            // We don't need alpha for this effect so we use an HDR no alpha texture format
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
            // Never forget to name your textures, it'll be very useful for debugging
            useDynamicScale: true, name: "Half Res Custom Pass"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // We choose an arbitrary 8 pixel radius for our blur
        float radius = 8.0f;
        // Precision of the blur, also affect the cost of the shader, 9 is a good value for real time apps
        int sampleCount = 9;

        // In case you have multiple camera at different resolution, make the blur coherent across these cameras.
        radius *= ctx.cameraColorBuffer.rtHandleProperties.rtHandleScale.x;

        // Our gaussian blur call, with the camera color buffer in source and destination
        // The half res target is used as temporary render target between the passes of our blur
        // Note that it's content will be cleared by the Gaussian Blur function.
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, halfResTarget,
            sampleCount, radius, downSample: true
        );
    }

    // Release the GPU memory, otherwise it'll leak
    protected override void Cleanup() => halfResTarget.Release();   
}
```

Note that we use a half res target `halfResTarget` because we first do a downsample pass. Alternatively you can also use the custom pass buffer we provide in HDRP, even if it's not a half res buffer, the algorithm will use only half of the texture.

<!-- TODO

### Downsample

### Vertical Blur & Horizontal Blur

## Copy

## DrawRenderers -->