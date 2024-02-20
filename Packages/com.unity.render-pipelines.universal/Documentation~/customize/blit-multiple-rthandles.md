# Blit multiple RTHandle textures and draw them on the screen

This page describes a blit operation that involves multiple `RTHandle` textures and a custom shader effect. The description uses the [DistortTunnel](../package-sample-urp-package-samples.md#renderer-features) scene from the URP package samples as an example.

The code samples on the page are from the following Scene from the [URP Package Samples](../package-sample-urp-package-samples.md):

    * Assets > Samples > Universal RP > 14.0.9 > URP Package Samples > RendererFeatures > DistortTunnel

The sample Scene uses the following assets to perform the blit operation:

* A [scriptable Renderer Feature](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature) enqueues three [render passes](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass) for execution.

* Two [render passes](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass) create intermediate textures. The final render pass binds the textures and performs the blit operation.

Import [URP Package Samples](../package-sample-urp-package-samples.md) to access the complete source code and the Scene.

For general information on the blit operation, refer to [URP blit best practices](../customize/blit-overview.md).

## Define the render passes in the Scriptable Renderer Feature

The [Renderer Feature](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature) defines the render passes necessary for rendering the intermediate textures and for the blit operation.

```C#
private DistortTunnelPass_CopyColor m_CopyColorPass;
private DistortTunnelPass_Tunnel m_TunnelPass;
private DistortTunnelPass_Distort m_DistortPass;
```

## The RTHandle variables

Each of three render passes and the renderer feature declare the `RTHandle` variables to create and process the textures for the sample effect.

For example, the `DistortTunnelRendererFeature` class declares the `RTHandle` variables, which it first passes as arguments to the render passes, and then uses the resulting textures in the final render pass (`DistortTunnelPass_Distort`).

The `Distort` shader, which the example uses for the final effect, uses the textures from the following code sample.

```C#
private RTHandle m_CopyColorTexHandle;
private const string k_CopyColorTexName = "_TunnelDistortBgTexture";
private RTHandle m_TunnelTexHandle;
private const string k_TunnelTexName = "_TunnelDistortTexture";
```

To create temporary render textures within the RTHandle system, the `SetupRenderPasses` method in the Renderer Feature uses the `ReAllocateIfNeeded` method:

```C#
RenderingUtils.ReAllocateIfNeeded(ref m_CopyColorTexHandle, desc, FilterMode.Bilinear,
    TextureWrapMode.Clamp, name: k_CopyColorTexName );
RenderingUtils.ReAllocateIfNeeded(ref m_TunnelTexHandle, desc, FilterMode.Bilinear,
    TextureWrapMode.Clamp, name: k_TunnelTexName );
```

## Configure the input and output textures

In this example, the `SetRTHandles` methods in render passes contain code for configuring the input and output textures.

For example, here is the `SetRTHandles` method from the `DistortTunnelPass_Distort` render pass:

```C#
public void SetRTHandles(ref RTHandle copyColorRT, ref RTHandle tunnelRT, RTHandle dest)
{
    if (m_Material == null)
        return;
    
    m_OutputHandle = dest;
    m_Material.SetTexture(copyColorRT.name,copyColorRT);
    m_Material.SetTexture(tunnelRT.name,tunnelRT);
}
```

## Perform the blit operation

There are two blit operations in this example.

The first example blit operation is in the `DistortTunnelPass_CopyColor` render pass. The pass blits the texture rendered by the camera to the `RTHandle` texture called `_TunnelDistortBgTexture`.

```C#
using (new ProfilingScope(cmd, m_ProfilingSampler))
{
    Blitter.BlitCameraTexture(cmd, m_Source, m_OutputHandle, 0);
}
```

The second example blit operation is the `DistortTunnelPass_Distort` render pass. 

Before performing the blit, the pass binds two source textures directly to the Material in the `SetRTHandles` method:

```C#
m_Material.SetTexture(copyColorRT.name,copyColorRT);
m_Material.SetTexture(tunnelRT.name,tunnelRT);
```

And then the pass performs the blit operation:

```C#
using (new ProfilingScope(cmd, m_ProfilingSampler))
{
    Blitter.BlitCameraTexture(cmd, m_OutputHandle, m_OutputHandle, m_Material, 0);
}
```

## Additional resources

* [Perform a full screen blit in URP](../renderer-features/how-to-fullscreen-blit.md)

    This page describes a basic blit operation and provides a complete step-by-step description of the implementation.