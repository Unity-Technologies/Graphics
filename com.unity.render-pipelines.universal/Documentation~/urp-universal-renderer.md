# Universal Renderer

This page describes the URP Universal Renderer settings.

For more information on rendering in URP, see also [Rendering in the Universal Render Pipeline](rendering-in-universalrp.md).

## Rendering Paths

The URP Universal Renderer implements two Rendering Paths:

* Forward Rendering Path.

* [Deferred Rendering Path](rendering/deferred-rendering-path.md).

### Rendering Path comparison

The following table shows the differences between the Forward and the Deferred Rendering Paths in URP.

| Feature | Forward | Deferred |
|---------|---------|----------|
| Maximum number of real-time lights per object. | 9 Lights per object. | Unlimited number of real-time lights. |
| Per-pixel normal encoding | No encoding (accurate normal values). | Two options:<ul><li>Quantization of normals in G-buffer (loss of accuracy, better performance).</li><li>Octahedron encoding (accurate normals, might have significant performance impact on mobile GPUs).</li></ul>For more information, see the section [Encoding of normals in G-buffer](rendering/deferred-rendering-path.md#accurate-g-buffer-normals). |
| MSAA | Yes | No |
| Vertex lighting | Yes | No |
| Camera stacking | Yes | Supported with a limitation: Unity renders only the base Camera using the Deferred Rendering Path. Unity renders all overlay Cameras using the Forward Rendering Path. |

## How to find the Universal Renderer asset

To find the Universal Renderer asset that a URP asset is using:

1. Select a URP asset.

2. In the Renderer List section, click a renderer item or the vertical ellipsis icon (&vellip;) next to a renderer.

    ![How to find the Universal Renderer asset](Images/urp-assets/find-renderer.png)

## Universal Renderer asset reference

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
| **Rendering&#160;Path** | Select the Rendering Path.<br/>Options:<ul><li>**Forward**: The Forward Rendering Path.</li><li>**Deferred**: The Deferred Rendering Path. For more information, see [Deferred Rendering Path](rendering/deferred-rendering-path.md).</li></ul> |
| &#160;&#160;&nbsp;&nbsp;**Depth&#160;Priming&#160;Mode** | Specifies when to perform depth priming. Depth priming is an optimization method that checks for pixels URP doesn't need to render during a [Base Camera's](camera-types-and-render-type.md#base-camera) opaque render pass. It uses the depth buffer generated in a depth prepass. The options are:<br />&#8226; **Disabled**: URP doesn't perform depth priming.<br />&#8226; **Auto**: URP performs depth priming for render passes that require a depth prepass.<br />&#8226; **Forced**: URP always performs depth priming. To do this, it also performs a depth prepass for every render pass.<br /><br />this property only appears if you set **Rendering Path** to **Forward** |
| &#160;&#160;&nbsp;&nbsp;**Accurate&#160;G-buffer&#160;normals** | Indicates whether to use a more resource-intensive normal encoding/decoding method to improve visual quality.<br /><br />This property only appears if you set **Rendering Path** to **Deferred**. |
| **Copy Depth Mode** | Specifies the stage in the render pipeline at which to copy the scene depth to a depth texture. The options are:<br/>&#8226; **After Opaques**: URP copies the scene depth after the opaques render pass.<br/>&#8226; **After Transparents**: URP copies the scene depth after the transparents render pass.<br/><br/>**Note**: On mobile devices, the **After Transparents** option can lead to a significant improvement in memory bandwidth. |

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

### Compatibility

This section contains settings related to backwards compatibility.

| Property | Description |
|:-|:-|
| **Intermediate Texture** | Controls when URP renders via an intermediate texture. Options: <ul><li>**Auto**: Uses information declared by active Renderer Features to automatically determine whether to render through an intermediate texture or not.</li><li>**Always**: Forces rendering via an intermediate texture, enabling compatibility with renderer features that do not declare their needed inputs, but can have a significant performance impact on some platforms.</li></ul> |

### Renderer Features

This section contains the list of Renderer Features assigned to the selected Renderer.

For information on how to add a Renderer Feature, see [How to add a Renderer Feature to a Renderer](urp-renderer-feature-how-to-add.md).

URP contains the pre-built Renderer Feature called [Render Objects](urp-renderer-feature.md#render-objects-renderer-feature).
