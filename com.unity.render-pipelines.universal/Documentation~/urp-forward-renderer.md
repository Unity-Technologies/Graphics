# Forward Renderer

This page describes the URP Forward Renderer settings.

The Forward Renderer in URP implements the forward rendering path.

For more information on how URP implements and uses the Forward Renderer, see [Rendering in the Universal Render Pipeline](rendering-in-universalrp.md).

## How to find the Forward Renderer asset

To find the Forward Renderer asset that a URP asset is using:

1. Select a URP asset.

2. In the Renderer List section, click a renderer item or the vertical ellipsis icon (&vellip;) next to a renderer.
    
    ![How to find the Forward Renderer asset](Images/urp-assets/find-renderer.png)

When you create a new project using the Universal Render Pipeline template, the Forward Renderer asset is in the following location:

```
/Assets/Settings/ForwardRenderer.asset
```

## Forward Renderer asset reference

This section describes the properties of the Forward Renderer asset.

![URP Forward Renderer](Images/urp-assets/urp-forward-renderer.png)

### Forward Renderer

This section contains properties for advanced customization use cases.

| Property | Description |
|:-|:-|
| Post Process Data | The asset containing references to shaders and Textures that the Renderer uses for post-processing.<br/>**Note:** This property is for advanced customization use cases. |

### Filtering

This section contains properties that define which layers the renderer draws.

| Property | Description |
|:-|:-|
| **Opaque Layer Mask** | Select which opaque layers this Renderer draws |
| **Transparent Layer Mask** | Select which transparent layers this Renderer draws |

### Shadows

This section contains properties related to rendering shadows.

| Property | Description |
|:-|:-|
| **Transparent Receive Shadows** | When this option is on, Unity draws shadows on transparent objects. |

### Overrides

This section contains Render Pipeline properties that this Renderer overrides.

#### Stencil

With this check box selected, the Renderer processes the Stencil buffer values.

![URP Forward Renderer Stencil override](Images/urp-assets/urp-forward-renderer-stencil-on.png)

For more information on how Unity works with the Stencil buffer, see [ShaderLab: Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html).

### Renderer Features

This section contains the list of Renderer Features assigned to the selected Renderer.

For information on how to add a Renderer Feature, see [How to add a Renderer Feature to a Renderer](urp-renderer-feature-how-to-add.md).

URP contains the pre-built Renderer Feature called [Render Objects](urp-renderer-feature.md#render-objects-renderer-feature).


