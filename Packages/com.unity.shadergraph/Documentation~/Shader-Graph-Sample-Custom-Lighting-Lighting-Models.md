# Lighting Model sub graph reference

Explore the Lighting Model sub graphs used in the [URP Custom Lighting templates](Shader-Graph-Sample-Custom-Lighting-Templates.md) included in the Custom Lighting sample.

These sub graphs are available in the [**Create Node** menu](Create-Node-Menu.md) under the **Lighting** > **Light Models** section.

> [!NOTE]
> To use any of these sub graphs, you must include it in a shader graph set up with the **Universal** (URP) target and the **Unlit** material type.

## Common sub graph structure

Each Lighting Model sub graph includes the following:
* The [ApplyDecals](Shader-Graph-Sample-Custom-Lighting-Components.md#general) sub graph, to blend decal data with the original material properties.
* The [Debug Lighting and Debug Materials](Shader-Graph-Sample-Custom-Lighting-Components.md#debug) sub graphs, to support the debug rendering modes (available in the Rendering Debugger window).
* A sub graph from the [Core Models](Shader-Graph-Sample-Custom-Lighting-Components.md#core-models) category, to define the lighting behavior according to the model.

> [!NOTE]
> The ApplyDecals, DebugLighting, and DebugMaterials sub graph nodes aren’t strictly required. They enable the decals and debug rendering modes, which are core engine features. When you don't use these features, the corresponding sub graph nodes don't affect your project's performance.

## Available Lighting Model sub graphs

### Lit Basic

This lighting model does very simple lighting and leaves out most lighting features to render as fast as possible. It calculates simple diffuse lighting and a simple form of ambient lighting. It does not support fog, reflections, specular, light cookies, or any other lighting features. But it does render fast and is ideal for low-end mobile devices and XR headsets.

### Lit Colorize

This lighting model is an example of custom behavior type you can create when you can control the lighting model. The main directional light renders the scene in grayscale with no color. Color is introduced with point lights, which allows you to control where the scene has color based on where you place the point lights in the scene.

### Lit Cook Torrance

This lighting model uses the Cook Torrance formula for specular highlights, which works well for brushed metal surfaces. It uses the same Lambert diffuse lighting, ambient lighting, reflections, and fog as the URP lighting model. It calculates lighting from the main light only.

### Lit Oren Nayar

This lighting model uses the Oren Nayar formula for diffuse lighting, which simulates rough surfaces such as clay or plaster. It uses the same ambient lighting, reflections, and fog as the URP lighting model, but doesn't calculate specular highlights. It calculates lighting from the main light only.

### Lit Simple

This lighting model is the same as the URP lighting model, except it uses the Blinn formula for the specular highlights. The Blinn formula makes it slightly cheaper to render than standard URP while looking fairly similar. If you need the full set of lighting features, including specular, fog, screen space ambient occlusion, and reflections, but want cheaper lighting, use this lighting model.

### Lit Toon

This lighting model uses a Posterize operation to break the smooth lighting gradient into distinct bands of shading. It simulates the look of cartoons where lighting is rendered with distinct colors of paint rather than smooth gradients.

### Lit URP

This lighting model closely matches the lighting that the Universal Render Pipeline does by default. If you want to start with the URP lighting and then alter it, this is the node to use.

## Additional resources

* [Introduction to lighting model customization](Shader-Graph-Sample-Custom-Lighting-Introduction.md)
* [Get started with the Custom Lighting sample](Shader-Graph-Sample-Custom-Lighting-Get-Started.md)
* [URP Custom Lighting template reference](Shader-Graph-Sample-Custom-Lighting-Templates.md)
