# URP Renderer Feature

A Renderer Feature is an asset that lets you add extra Render passes to a URP Renderer and configure their behavior.

URP contains the pre-built Renderer Feature called **Render Objects**.

For information on how to add a Renderer Feature to a Renderer, see the page [How to add a Renderer Feature to a Renderer](urp-renderer-feature-how-to-add.md).

## Render Objects Renderer Feature<a name="render-objects-renderer-feature"></a>

The Render Objects Renderer Feature contains the following properties.

![Render Objects Renderer Feature Inspector view](Images/urp-assets/urp-renderer-feature-render-objects.png)

| Property | Description |
|:-|:-|
| **Name** | The name of the feature. |
| **Event** | The event in the URP queue when Unity executes this Renderer Feature. |
| **Filters** | Settings that let you configure which objects this Renderer Feature renders. |
| Queue | Select whether the feature renders opaque or transparent objects. |
| Layer Mask | The Renderer Feature renders objects from layers you select in this property. |
| **Pass Names** | If a Pass in a shader has the `LightMode` Pass Tag, this Renderer Feature processes only the shaders where the value of the `LightMode` Pass Tag equals one of the values in the Pass Names property. |
| **Overrides** | Settings in this section let you configure overrides for certain properties when rendering with this Renderer Feature. |
| Material | When rendering an object, Unity replaces the Material assigned to it with this Material. |
| Depth | Selecting this option lets you specify how this Renderer Feature affects or uses the Depth buffer. This option contains the following items:<br/>Write Depth: this option defines whether the Renderer Feature updates the Depth buffer when rendering objects.<br/>Depth Test: the condition which determines when this Renderer Feature renders pixels of a given object. |
| Stencil | With this check box selected, the Renderer processes the Stencil buffer values.<br/>For more information on how Unity works with the Stencil buffer, see [ShaderLab: Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html). |
| Camera | Selecting this option lets you override the following Camera properties:<br/>Field of View: when rendering objects, the Renderer Feature uses this Field of View instead of the value specified on the Camera.<br/>Position Offset: when rendering objects, the Renderer Feature moves them by this offset.<br/>Restore: with this option selected, the Renderer Feature restores the original Camera matrices after executing the render passes in this Renderer Feature. |
