# Stencil Buffer Usage in HDRP

In HDRP, the stencil buffer plays a crucial role in optimizing various rendering effects throughout the frame. For users wanting to customize the rendering, two bits are available to implement their effects.

## HDRP Reserved Stencil Bits

HDRP reserves specific bits for features such as subsurface scattering, SSR, receive decal on materials, object exclusion from TAA, and more. While it's possible to directly use these bits by hardcoding their values, it's important to note that their meanings can evolve between versions. The most accurate and current information can be found in the **HDStencilUsage.cs** file.

During HDRP rendering, a "Clear Stencil Buffer" pass occurs just before starting to render transparent objects, and it doesn't modify user bits. The only instance where user bits are cleared is at the start of rendering when the depth buffer is cleared.

## Free Stencil Bits

Within the stencil buffer during rendering, bits 6 and 7 remain untouched by HDRP code. Any other bits are HDRP reserved and may be cleared or overwritten at any moment during the frame.

When writing to the stencil buffer, it's crucial to set the write mask value to either `UserStencilUsage.UserBit0` or `UserStencilUsage.UserBit1` (or both) to prevent unintended writes to reserved bits. Utilize the following C# code snippet to set the stencil write mask in a shader:

```csharp
stencilMaterial.SetInt("_StencilWriteMask", (int)UserStencilUsage.UserBit0);
```

In the shader, declare the `_StencilWriteMask` property in both the Properties section and the stencil block:

```csharp
Properties
{
    [HideInInspector] _StencilWriteMask("_StencilWriteMask", Float) = 0
}

Stencil
{
    Ref -1
    Comp Always
    WriteMask [_StencilWriteMask]
    Pass replace
}
```

**Note:**  Writing to HDRP reserved bits can lead to undesired rendering artifacts and undefined behavior.

For more information about stencil write masks, refer to the [ShaderLab Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html) documentation.

The table below givews you an idea of the bits used throughout the pipeline in HDRP. To get the most up to date information regarding stencil usage, please see the [HDStencilUsage.cs](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDStencilUsage.cs) file in the HDRP package.

| Rendering pass                     | Stencil bit number | Stencil bit name                   | HDRP feature that this bit affects                                                                                                                                                                                                                                                       |
| ---------------------------------- | ------------------ | ---------------------------------- | --------------------------- |
| Before transparent rendering
|                                    | 0                  | **Unlit**                             | [Unlit Material](unlit-material.md)  |
|                                    | 1                  | **RequiresDeferredLighting** | Deferred lighting. |
|                                    | 2                  | **SubsurfaceScattering**           | [Subsurface scattering (SSS)](skin-and-diffusive-surfaces-subsurface-scattering.md). |
|                                    | 3                  | **TraceReflectionRay**             | [Screen Space Reflection (SSR)](Override-Screen-Space-Reflection.md)<br/> [Ray-traced reflections (RTR)](Ray-Traced-Reflections.md). |
|                                    | 4                  | **Decals**                         | [Decal](Decal.md)  |
|                                    | 5                  | **ObjectMotionVector**             | [Motion blur](Post-Processing-Motion-Blur.md)<br/> [Screen Space Reflection (SSR)](Override-Screen-Space-Reflection.md)<br/> [Screen-space Ambient-Occlusion (SSAO)](Override-Ambient-Occlusion.md#SSAO)<br/> [Temporal anti-aliasing (TAA)](Anti-Aliasing.md#TAA). |
| After opaque rendering             |                    |                                    |      |
|                                    | 0            | **WaterExclusion** | [Exclude part of a water surface](water-exclude-part-of-the-water-surface.md).     |
|                                    | 1            | **ExcludeFromTUAndAA** | [Temporal anti-aliasing (TAA)](Anti-Aliasing.md#TAA)<br/> [Temporal Anti-Aliasing Upscale](Dynamic-Resolution.md#notes-on-temporal-anti-aliasing-taa-upscale).    |
|                                    | 2                  | **SMAA** and **DistortionVectors**    | [Subpixel morphological anti-aliasing (SMAA)](Anti-Aliasing.md#SMAA) |
|                                    | 3                  |    | Reserved for future use. |
|                                    | 4                  | **Refractive**    | [Refraction](understand-refraction.md). |
|                                    | 5                  | **WaterSurface**    | [Water](water.md). |

## More Stencil Bits for Custom Passes

If your custom pass requires more than two stencil bits, consider using the custom depth buffer which contains 8 stencil bits. HDRP doesn't utilize the bits in this buffer, but be aware that other custom passes might. To bind the custom depth buffer, use the following function:

```csharp
protected override void Execute(CustomPassContext ctx)
{
    CoreUtils.SetRenderTarget(ctx.cmd, ctx.customDepthBuffer.Value, ClearFlag.DepthStencil);

    // Render objects...
}
```
