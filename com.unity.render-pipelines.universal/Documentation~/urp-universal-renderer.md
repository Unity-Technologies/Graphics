# Universal Renderer

This page describes the URP Universal Renderer settings. For more information on rendering in URP, see also [Rendering in the Universal Render Pipeline](rendering-in-universalrp.md).

To find the Universal Renderer asset that a URP asset is using:

1. Select a URP asset.

2. In the Renderer List section, click a renderer item or the vertical ellipsis icon (&vellip;) next to a renderer.

    ![How to find the Universal Renderer asset](Images/urp-assets/find-renderer.png)

## Asset reference

This section describes the properties of the Forward Renderer asset.

![URP Universal Renderer](Images/urp-assets/urp-universal-renderer.png)

### Filtering

This section contains properties that define which layers the renderer draws.

| Property | Description |
|:-|:-|
| **Opaque Layer Mask** | Select which opaque layers this Renderer draws |
| **Transparent Layer Mask** | Select which transparent layers this Renderer draws |

### Rendering

This section contains properties related to rendering.

| Property | Description |
|:-|:-|
| **Rendering&#160;Path** | Select the Rendering Path. The options are:<br />&#8226; **Forward**: The Forward Rendering Path.<br />&#8226; **Deferred**: The Deferred Rendering Path. For more information, see [Deferred Rendering Path](rendering/deferred-rendering-path.md).<br /><br />For a comparison of the options, see the section [Rendering Path comparison](#rendering-path-comparison). |
| &#160;&#160;**Depth&#160;Priming&#160;Mode** | This property determines when Unity performs depth priming.<br/>Depth Priming can improve GPU frame timings by reducing the number of pixel shader executions. The performance improvement depends on the amount of overlapping pixels in the opaque pass and the complexity of the pixel shaders that Unity can skip by using depth priming.<br/>The feature has an upfront memory and performance cost. The feature uses a depth prepass to determine which pixel shader invocations Unity can skip, and the feature adds the depth prepass if it's not available yet.<br/>The options are:<br />&#8226; **Disabled**: Unity does not perform depth priming.<br />&#8226; **Auto**: If there is a Render Pass that requires a depth prepass, Unity performs the depth prepass and depth priming.<br />&#8226; **Forced**: Unity always performs depth priming. To do this, Unity also performs a depth prepass for every render pass. **NOTE**: depth priming is disabled at runtime on certain hardware (Tile Based Deferred Rendering) regardless of this setting.<br /><br />On Android, iOS, and Apple TV, Unity performs depth priming only in the Forced mode. On tiled GPUs, which are common to those platforms, depth priming might reduce performance when combined with MSAA.<br/><br/>This property is available only if **Rendering Path** is set to **Forward** |
| &#160;&#160;**Accurate G-buffer normals** | Indicates whether to use a more resource-intensive normal encoding/decoding method to improve visual quality. The options are:<br />&#8226; **On**: Unity uses the octahedron encoding to store values of normal vectors in the RGB channel of a normal texture. With this encoding, values of normal vectors are more accurate, but the encoding and decoding operations put extra load on the GPU.<br />&#8226; **Off**: This option increases performance, especially on mobile GPUs, but might lead to color banding artifacts on smooth surfaces.<br /><br />This property is available only if **Rendering Path** is set to **Deferred**. For more information about this setting, see the section [Encoding of normals in G-buffer](rendering/deferred-rendering-path.md#accurate-g-buffer-normals). |
| **Copy&#160;Depth&#160;Mode** | Specifies the stage in the render pipeline at which to copy the scene depth to a depth texture. The options are:<br/>&#8226; **After Opaques**: URP copies the scene depth after the opaques render pass.<br/>&#8226; **After Transparents**: URP copies the scene depth after the transparents render pass.<br/><br/>**Note**: On mobile devices, the **After Transparents** option can lead to a significant improvement in memory bandwidth. |

### Native RenderPass

This section contains properties related to URP's Native RenderPass API.

| Property | Description |
|:-|:-|
| **Native RenderPass** | Indicates whether to use URP's Native RenderPass API. When enabled, URP uses this API to structure render passes. As a result, you can use [programmable blending](https://docs.unity3d.com/Manual/SL-PlatformDifferences.html#using-shader-framebuffer-fetch) in custom URP shaders. For more information about the RenderPass API, see [ScriptableRenderContext.BeginRenderPass](https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.BeginRenderPass.html).<br/><br/>**Note**: Enabling this property has no effect on OpenGL ES. |

### Shadows

This section contains properties related to rendering shadows.

| Property | Description |
|:-|:-|
| **Transparent Receive Shadows** | When this option is on, Unity draws shadows on transparent objects. |

### Overrides

This section contains Render Pipeline properties that this Renderer overrides.

#### Stencil

With this check box selected, the Renderer processes the Stencil buffer values.

![URP Universal Renderer Stencil override](Images/urp-assets/urp-universal-renderer-stencil-on.png)

For more information on how Unity works with the Stencil buffer, see [ShaderLab: Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html).

### Renderer Features

This section contains the list of Renderer Features assigned to the selected Renderer.

For information on how to add a Renderer Feature, see [How to add a Renderer Feature to a Renderer](urp-renderer-feature-how-to-add.md).

URP contains the pre-built Renderer Feature called [Render Objects](urp-renderer-feature.md#render-objects-renderer-feature).

## <a name="rendering-path-comparison"></a>Rendering Path comparison

The following table shows the differences between the Forward and the Deferred Rendering Paths in URP.

| Feature | Forward | Deferred |
|---------|---------|----------|
| Maximum number of real-time lights per object. | 8 lights per object and 1 main light. | Unlimited number of real-time lights. |
| Per-pixel normal encoding | No encoding (accurate normal values). | Two options:<ul><li>Quantization of normals in G-buffer (loss of accuracy, better performance).</li><li>Octahedron encoding (accurate normals, might have significant performance impact on mobile GPUs).</li></ul>For more information, see the section [Encoding of normals in G-buffer](rendering/deferred-rendering-path.md#accurate-g-buffer-normals). |
| MSAA | Yes | No |
| Vertex lighting | Yes | No |
| Camera stacking | Yes | Supported with a limitation: Unity renders only the base Camera using the Deferred Rendering Path. Unity renders all overlay Cameras using the Forward Rendering Path. |
| Layers | Yes | No |
| Light Layers | Yes | Supported, but likely to have a negative impact on GPU performance due to needing an extra G-buffer render target to store the rendering layer mask (32 bits). |

### Terrain blending

When blending more than four Terrain layers, the Deferred Rendering Path generates slightly different results from the Forward Rendering Path. This happens because in the Forward Rendering Path, Unity processes the first four layers separately from the next four layers using multi-pass rendering.

In the Forward Rendering Path, Unity merges Material properties and calculates lighting for the combined properties of four layers at once. Unity then processes the next four layers in the same way and alpha-blends the lighting results.

In the Deferred Rendering Path, Unity combines Terrain layers in the G-buffer pass, four layers at a time, and then calculates lighting only once during the deferred rendering pass. This difference with the Forward Rendering Path leads to [visually different outcomes](#terrain-visual-diff).

Unity combines the Material properties in the G-buffer using hardware blending (four layers at a time), which limits how correct the combination of property values is. For example, pixel normals cannot be correctly combined using the alpha blend equation alone, because one Terrain layer might contain coarse Terrain detail while another layer might contain fine detail. Averaging or summing normals results in loss of accuracy.

> **NOTE:** Turning the setting [Accurate G-buffer normals](rendering/deferred-rendering-path.md#accurate-g-buffer-normals) on breaks Terrain blending. With this setting turned on, Unity encodes normals using octahedron encoding. Normals in different layers encoded this way cannot be blended together because of the bitwise nature of the encoding (2 x 12 bits). If your application requires more than four Terrain layers, turn the **Accurate G-buffer normals** setting off.

<a name="terrain-visual-diff"></a>The following illustration shows the visual difference when rendering Terrain layers with different Rendering Paths.

![Terrain layers rendered with the Forward Rendering Path](Images/rendering-deferred/terrain-layers-forward.png)<br/>*Terrain layers rendered with the Forward Rendering Path*

![Terrain layers rendered with the Deferred Rendering Path](Images/rendering-deferred/terrain-layers-deferred.png)<br/>*Terrain layers rendered with the Deferred Rendering Path*

### Baked Global Illumination and Lighting Modes

When Baked Global Illumination is enabled, the Subtractive and the Shadowmask Lighting modes put extra load on the GPU in the Deferred Rendering Path.

The Deferred Rendering Path supports the Subtractive and the Shadowmask Lighting modes for compatibility reasons, but, unlike the case with the Forward Rendering Path, these modes do not provide any improvements in performance. In the Deferred Rendering Path, Unity processes all meshes using the same Lighting algorithm and stores the extra Lighting properties required by Subtractive and the Shadowmask modes in the [ShadowMask render target](#g-buffer-layout).

In the Deferred Rendering Path, the Baked Indirect Lighting mode provides better performance, since it does not require the ShadowMask render target.
