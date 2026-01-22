# Introduction to lighting model customization with Shader Graph

You can customize lighting models to do the following:

* Improve rendering performance. You can remove or simplify one or more types of lighting calculations and sacrifice visual quality for rendering speed.

* Make your lighting look different from Unity's default lighting. For example, to make your project look like a watercolor painting or a pen and ink drawing.


## Lighting model customization examples

The assets available in the **Custom Lighting** sample allow you to [get started with lighting model customization](Shader-Graph-Sample-Custom-Lighting-Get-Started.md) in different contexts and for various purposes. 

| **Context** | **Description** | **Template** |
| :--- | :--- | :--- |
| Model for devices where performance is critical | With XR or low-end mobile devices where performance is critical, you likely need to reduce the cost of calculations in your shaders. For example, you might skip any fog, specular, reflections, light cookies, or SSAO support and only keep the basics. | [Custom Lighting Basic](Shader-Graph-Sample-Custom-Lighting-Templates.md#custom-lighting-basic) |
| Simplified URP model with performance optimization | If you need to optimize performance in URP, you might want to use a Blinn model, which is similar to the main lighting in URP but slightly cheaper for specular highlights. The Blinn model slightly affects the redering quality but renders faster in most cases. | [Custom Lighting Simple](Shader-Graph-Sample-Custom-Lighting-Templates.md#custom-lighting-simple) |
| Posterization model for cartoon appearance | You can alter the lighting in a shader graph to achieve stylized effects such as posterization, to give the visuals of your project the appearance of a cartoon. | [Custom Lighting Toon](Shader-Graph-Sample-Custom-Lighting-Templates.md#custom-lighting-toon) |
| Lit URP model customization | You might want to explore the process of lighting customization directly from a shader based on the Lit URP model. | [Custom Lighting URP](Shader-Graph-Sample-Custom-Lighting-Templates.md#custom-lighting-urp) |
| Other types of lighting customization | You can use any of the [Lighting Model](Shader-Graph-Sample-Custom-Lighting-Lighting-Models.md) sub graphs available in the **Custom Lighting** sample to create any type of lighting that you need within your own shader graphs. These sub graphs use a recommended pattern that illustrates how Unity expects custom lighting models to be defined. | N/A |


## Limitations

Before you start to customize lighting models with Shader Graph, be aware of the following limitations:

### Shader Graph doesn't support deferred rendering

Customizable lighting is only intended to be used when you set the render type to Forward or Forward+ in the Render Asset.

When you set the render type to Deferred or Deferred+, it's not possible to control the lighting directly in an object’s shader, because the lighting occurs in a pass that is not directly connected to the object's shader.

If you need to customize lighting in a deferred rendering context, you have to [write shaders in code](xref:um-shader-writing) instead of using Shader Graph.

### Handling multiple light sources in Shader Graph requires HLSL coding

To support multiple light sources in Shader Graph, you have to write a small amount of code.

For the main directional light, you can create custom lighting in Shader Graph without coding. However, the part of the graph that does multiple light calculations requires the use of a [Custom Function node](Custom-Function-Node.md), because Shader Graph doesn’t support `For` loops.

The sample includes multiple examples of Additional Lights nodes, but if you want to create your own, you need to know a little bit of HLSL coding.
