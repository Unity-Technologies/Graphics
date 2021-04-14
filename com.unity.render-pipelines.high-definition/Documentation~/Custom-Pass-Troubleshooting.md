# Troubleshooting

This section provides examples of common issues you might encounter when using a Custom Pass component, and how to fix them.

## Display scaling issues

![](images/Custom_Pass_Troubleshooting_01.png)

A scaling issue can appear in your build when you have two cameras that do not use the same resolution. This is most common between in-game and scene views. This can happen when:

- Your code calls [CommandBuffer.SetRenderTarget](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.SetRenderTarget.html), and `CoreUtilsSetRenderTarget` sets the viewport.
- You have not multiplied by `_RTHandleScale.xy` in your shader code for UVs when sampling an `RTHandle` buffer.

To fix the causes in these cases:

- Use `CoreUtils.SetRenderTarget` instead of `CommandBuffer.SetRenderTarget`.
- Use `_RTHandleScale.xy` in your shader code when sampling an `RTHandle` buffer.

## Opaque objects disappear in build

If GameObjects with an opaque material in your scene disappear when you build your program, you might need to reconfigure your HDRP Asset settings.

To fix this:

1. In the **Project** window, navigate to your **HDRenderPipelineAsset** (if you are using the default HDRP build, go to `Assets > Settings`).
2. In the HDRP asset, change the **Lit Shader Mode** to **Both**.

## Jittering GameObjects when TAA is enabled

In some cases when [Temporal anti-aliasing (TAA)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.1/manual/Anti-Aliasing.html?q=anti#TAA) is enabled, some GameObjects appear to jitter.

![](images/Custom_Pass_Troubleshooting_02.gif)

Jittering can happen when both of the following conditions are met:

* The object is rendered in the **AfterPostProcess** [Injection Point](Custom-Pass-Injection-Points.md) . To fix this, change the **Injection Point** in the [Custom Pass Volume](Custom-Pass-Creating.md#Custom-Pass-Volume) component.
* The object has **Depth Test** enabled. To fix this, disable **Depth Test** in the [shader properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.2/manual/Lit-Shader.html), or set the Depth Test property in the [draw renders Custom Pass component](Custom-Pass-Creating.md#Draw-Renderers-Custom-Pass) to **Disabled** .

## Shuriken particles facing the wrong direction

The following conditions can cause particles in the scene to face the wrong direction:

- The particle system is only visible in a Custom Pass.
- There is no override implemented for`AggregateCullingParameters`.

Unity calculates the orientation of the particles in the Built-in Particle System when it executes `AggregateCullingParameters`  during the culling step. Therefore, if there is no override, HRDP does not render it properly.

## Decals are not visible

The following conditions can make a decal in your scene invisible:

- The decal is applied to a GameObject that Unity renders in a Custom Pass
- The decal is on a transparent object that Unity renders before the **AfterOpaqueDepthAndNormal** [Injection point](Custom-Pass-Injection-Points.md)

To fix this issue, change the Injection point of the GameObject to any Injection Point after **AfterOpaqueDepthAndNormal**

## Culling issues

If you can’t see some objects in your scene, this might be because the `cullingResult` you receive in the `Execute` method doesn’t contain objects that HDRP only renders in a Custom Pass.

This can happen if you disable the layer of your objects in the [Camera](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.3/manual/HDRP-Camera.html) **Culling mask**.

This happens because, by default, this `cullingResult` is the camera `cullingResult`. To fix this issue, override this method in the `CustomPass` class:

```c#
protected virtual void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera camera) {}
```

You can then add more layers or custom culling options to the `cullingResult` you receive in the `Execute` method using [ScriptableCullingParameters](https://docs.unity3d.com/ScriptReference/Rendering.ScriptableCullingParameters.html) .

## Screen turns black when Unity loads shaders

If your screen turns black when Unity loads shaders, this could be because Unity is trying to render a shader that is not referenced in the scene. This can happen when:

- The Custom Pass only uses `Shader.Find` to look for your shader.
- You have not included your shader in the Resources folder.
- Your shader is not referenced in the Custom Pass.

To fix this, you can add the following lines of code to reference your shader in the Custom Pass:

 ```C#
    [SerializeField, HideInInspector]
    Shader shaderName;
 ```

For example:

```C#
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class Outline : CustomPass
{
    public LayerMask    outlineLayer = 0;
    [ColorUsage(false, true)]
    public Color        outlineColor = Color.black;
    public float        threshold = 1;

    // To make sure the shader ends up in the build, we keep a reference to it
    [SerializeField, HideInInspector]
    Shader                  outlineShader;

    Material                fullscreenOutline;
    RTHandle                outlineBuffer;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        outlineShader = Shader.Find("Hidden/Outline");
        fullscreenOutline = CoreUtils.CreateEngineMaterial(outlineShader);

        // Define the outline buffer
        outlineBuffer = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
// We don't need alpha for this effect
            useDynamicScale: true, name: "Outline Buffer"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Render meshes we want to apply the outline effect to in the outline buffer
        CoreUtils.SetRenderTarget(ctx.cmd, outlineBuffer, ClearFlag.Color);
        CustomPassUtils.DrawRenderers(ctx, outlineLayer);

        // Set up outline effect properties
        ctx.propertyBlock.SetColor("_OutlineColor", outlineColor);
        ctx.propertyBlock.SetTexture("_OutlineBuffer", outlineBuffer);
        ctx.propertyBlock.SetFloat("_Threshold", threshold);

        // Render the outline buffer fullscreen
        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
        CoreUtils.DrawFullScreen(ctx.cmd, fullscreenOutline, ctx.propertyBlock, shaderPassId: 0);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(fullscreenOutline);
        outlineBuffer.Release();
    }
}
```
