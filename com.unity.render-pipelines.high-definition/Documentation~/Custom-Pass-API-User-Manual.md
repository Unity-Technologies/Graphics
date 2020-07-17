# Custom Pass Utils API User Manual

## Blur

### Gaussian Blur

The Gaussian blur function allows you to blur an image with an arbitrary radius and quality (number of samples). For performance reasons, you can run the blur kernel after a downsample pass. This decreases the resource intensity of the blur effect but also decreases the quality.

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
            // Note the * 0.5f here. This allocates a half-resolution target, which saves a lot of memory.
            Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
            // Since alpha is unnecessary for Gaussian blur, this effect uses an HDR texture format with no alpha channel.
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
            // When creating textures, be sure to name them as it is useful for debugging.
            useDynamicScale: true, name: "Half Res Custom Pass"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Specifies the radius for the blur in pixels. This example uses an 8 pixel radius.
        float radius = 8.0f;
        // Specifies the precision of the blur. This also affects the resource intensity of the blue. A value of 9 is good for real-time applications.
        int sampleCount = 9;

        // In cases where you have multiple cameras with different resolutions, this makes the blur coherent across these cameras.
        radius *= ctx.cameraColorBuffer.rtHandleProperties.rtHandleScale.x;

        // The actual Gaussian blur call. It specifies the current camera's color buffer as the source and destination.
        // This uses the half-resolution target as a temporary render target between the blur passes.
        // Note that the Gaussian blur function clears the content of the half-resolution buffer when it finishes.
        CustomPassUtils.GaussianBlur(
            ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, halfResTarget,
            sampleCount, radius, downSample: true
        );
    }

    // Releases the GPU memory allocated for the half-resolution target. This is important otherwise the memory will leak.
    protected override void Cleanup() => halfResTarget.Release();   
}
```

Note that this example uses a half-resolution target, halfResTarget, because the example first processes a downsample pass. Alternatively, you can also use the custom pass buffer that HDRP provides. Even if this is not a half-resolution buffer, the algorithm only uses half of the texture.

<!-- TODO

### Downsample

### Vertical Blur & Horizontal Blur

## Copy

## DrawRenderers -->