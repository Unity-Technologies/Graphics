# Use frame data

You can fetch the textures the Universal Render Pipeline (URP) creates for the current frame, for example the active color buffer or a G-buffer texture, and use them in your render passes.

These textures are called frame data, resource data, or frame resources.

Some textures might not exist in the frame data, depending on which injection point you use to insert your custom render pass into the URP frame rendering loop. Refer to the following for information about which textures exist when:

- [Injection points reference](customize/custom-pass-injection-points.md)

## Get frame data

The frame data is in the `ContextContainer` object that URP provides when you override the `RecordRenderGraph` method.

Follow these steps to get a handle to a texture in the frame data:

1. Get all the frame data as a `UniversalResourceData` object, using the `Get` method of the `ContextContainer` object.

    For example:

    ```csharp
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        using (var builder = renderGraph.AddPass<PassData>("Get frame data", out var passData))
        {
            UniversalResourceData frameData = frameContext.Get<UniversalResourceData>();
        }
    }
    ```

2. Get the handle to a texture in the frame data.

    For example, the following gets a handle to the active color texture:

    ```csharp
    TextureHandle activeColorTexture = frameData.activeColorTexture;
    ```

You can then read from and write to the texture. Refer to [Use a texture in a render pass](render-graph-read-write-texture.md) for more information.

You can use the [ConfigureInput](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass.ConfigureInput(UnityEngine.Rendering.Universal.ScriptableRenderPassInput)) API to make sure URP generates the texture you need in the frame data.

## Textures in the frame data

You can fetch the following textures from the frame data.

| **Property** | **Texture** | **URP shader pass that writes to the texture** |
|-|-|-|
| `additionalShadowsTexture ` | The additional shadow map. | `ShadowCaster` |
| `activeColorTexture` | The color texture the camera  currently targets. | Any pass, depending on your settings |
| `activeDepthTexture` | The depth texture the camera is currently targets. | Any pass, depending on your settings |
| `afterPostProcessColor` | The main color texture after URP's post processing passes. | `UberPost` |
| `backBufferColor ` | The color texture of the screen back buffer. If you use [post-processing](integration-with-post-processing.md), URP writes to this texture at the end of rendering, unless you enable [HDR Debug Views](post-processing/hdr-output.md#hdr-debug-views). Refer to `debugScreenTexture` for more information. | Any pass, depending on your settings | 
| `backBufferDepth ` | The depth texture of the screen back buffer. | Any pass, depending on your settings | 
| `cameraColor` | The main color texture for the camera. You can store multiple samples in this texture if you enable [Multisample Anti-aliasing (MSAA)](anti-aliasing.md#msaa). | Any pass, depending on your settings | 
| `cameraDepth` | The main depth texture for the camera. You can store multiple samples in this texture if you enable [Multisample Anti-aliasing (MSAA)](anti-aliasing.md#msaa). | Any pass, depending on your settings | 
| `cameraDepthTexture` | A copy of the depth texture, if you enable **Depth Priming Mode** in the [renderer](urp-universal-renderer.md) or **Depth Texture** in the active [URP Asset](universalrp-asset.md). | `CopyDepth` or `DepthPrepass` |
| `cameraNormalsTexture` | The scene normals texture. Contains the scene depth for objects with shaders that have a `DepthNormals` pass. | `DepthNormals` prepass |
| `cameraOpaqueTexture` | A texture with the opaque objects in the scene, if you enable **Opaque Texture** in the [URP Asset](universalrp-asset.md). | `CopyColor` |
| `dBuffer` | The Decals texture. Refer to [DBuffer](renderer-feature-decal.md#dbuffer) for more information. | `Decals` |
| `dBufferDepth` | The Decals depth texture. Refer to [DBuffer](renderer-feature-decal.md#dbuffer) for more information. | `Decals` |
| `debugScreenTexture` | If you enable [HDR Debug Views](post-processing/hdr-output.md#hdr-debug-views), URP writes the output of [post-processing](integration-with-post-processing.md) to this texture instead of `backBufferColor`. | `uberPost` and `finalPost` |
| `gBuffer` | The G-buffer textures. Refer to [G-buffer](rendering/deferred-rendering-path.md#g-buffer-layout) for more information. | `GBuffer`  |
| `internalColorLut` | The internal look-up textures (LUT) texture. | `InternalLut` |
| `mainShadowsTexture ` | The main shadow map. | `ShadowCaster` |
| `motionVectorColor` | The motion vectors color texture. Refer to [motion vectors](features/motion-vectors.md) for more information. | `Camera Motion Vectors` and `MotionVectors` |
| `motionVectorDepth` | The motion vectors depth texture. Refer to [motion vectors](features/motion-vectors.md) for more information. | `Camera Motion Vectors` and `MotionVectors` |
| `overlayUITexture` | The overlay UI texture. | `DrawScreenSpaceUI` |
| `renderingLayersTexture` | The Rendering Layers texture. Refer to [Rendering layers](features/rendering-layers.md) | `DrawOpaques` or the `DepthNormals` prepass, depending on your settings. |
| `ssaoTexture` | The Screen Space Ambient Occlusion (SSAO) texture. Refer to [Ambient occlusion](post-processing-ssao.md) for more information. | `SSAO` |

## Example

Refer to the following for examples of custom render passes that use the frame data:

- The render graph system samples in the [URP package samples](package-sample-urp-package-samples.md)

## Additional resources

- [Rendering](rendering-in-universalrp.md)
- [Render pipeline concepts](urp-concepts.md)
- [Deferred rendering path in URP](rendering/deferred-rendering-path.md)

