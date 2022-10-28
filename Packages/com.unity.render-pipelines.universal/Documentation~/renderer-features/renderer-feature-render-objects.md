# Render Objects Renderer Feature<a name="render-objects-renderer-feature"></a>

URP draws objects in the **DrawOpaqueObjects** and **DrawTransparentObjects** passes. You might need to draw objects at a different point in the frame rendering, or interpret and write rendering data (like depth and stencil) in alternate ways. The Render Objects Renderer Feature lets you do such customizations by letting you draw objects on a certain layer, at a certain time, with specific overrides.

## How to use the Render Objects Renderer Feature

See: [How to use the Render Objects Renderer Feature](how-to-custom-effect-render-objects.md).

## Properties

The Render Objects Renderer Feature contains the following properties.

| Property | Description |
|:-|:-|
| **Name** | Use this field to edit the name of the feature. |
| **Event** | The event in the URP queue when Unity executes this Renderer Feature. |
| **Filters** | Settings that let you configure which objects this Renderer Feature renders. |
| Queue | Select whether the feature renders opaque or transparent objects. |
| Layer Mask | The Renderer Feature renders objects from layers you select in this property. |
| **Pass Names** | If a Pass in a shader has the `LightMode` Pass Tag, this Renderer Feature processes only the shaders where the value of the `LightMode` Pass Tag equals one of the values in the Pass Names property. |
| **Overrides** | Settings in this section let you configure overrides for certain properties when rendering with this Renderer Feature. |
| Override Mode | Specify the material override mode.
| Material | (Override Mode is set to Material) When rendering an object, Unity replaces the Material assigned to it with this Material. This will override all material properties with this material |
| Shader | (Override Mode is set to Shader) When rendering an object, Unity replaces the material assigned to it with this shader. This maintains all material properties and allows the override shader to access these properties. This is currently not SRPBatcher compatible and less performant.
| Depth | Selecting this option lets you specify how this Renderer Feature affects or uses the Depth buffer. This option contains the following items:<br/>Write Depth: this option defines whether the Renderer Feature updates the Depth buffer when rendering objects.<br/>Depth Test: the condition which determines when this Renderer Feature renders pixels of a given object. |
| Stencil | With this check box selected, the Renderer processes the Stencil buffer values.<br/>For more information on how Unity works with the Stencil buffer, see [ShaderLab: Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html). |
| Camera | Selecting this option lets you override the following Camera properties:<br/>Field of View: when rendering objects, the Renderer Feature uses this Field of View instead of the value specified on the Camera.<br/>Position Offset: when rendering objects, the Renderer Feature moves them by this offset.<br/>Restore: with this option selected, the Renderer Feature restores the original Camera matrices after executing the render passes in this Renderer Feature. |
