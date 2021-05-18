# Decal Shader Graph

If you assign the Decal Shader Graph to a Material, the [Decal Projector](renderer-feature-decal.md#decal-projector-component) component can project such Material as a decal onto meshes in a Scene.

### Advanced Options

These properties allow you to change the rendering behavior of the decal.

| __Property__              | __Description__                                              |
| --------------------------| ------------------------------------------------------------ |
| __Enable GPU Instancing__ | Makes URP render meshes with the same geometry and Material in one batch, when possible. This makes rendering faster. URP cannot render Meshes in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| __Priority__              | Controls the order in which URP draws decals in the Scene. URP draws decals with lower values first, so it draws decals with a higher draw order value on top of those with lower values. <br />__Note__: This property only applies to decals the [Decal Projector](decal-projector.md) creates and has no effect on Mesh decals. Additionally, if you have multiple Decal Materials with the same __Priority__, the order URP renders them in depends on the order you create the Materials. URP renders Decal Materials you create first before those you create later with the same __Priority__. |
| __Mesh Decal Bias Type__  | Determines the type of bias that URP applies to the decal’s Mesh to stop it from overlapping with other Meshes. |
| _View Bias_               | A world-space bias (in meters) that URP applies to the decal’s Mesh to stop it from overlapping with other Meshes along the view vector. A positive value draws the decal in front of any overlapping Mesh, while a negative value offsets the decal and draws it behind. This property only affects decal Materials directly attached to GameObjects with a Mesh Renderer, so Decal Projectors do not use this property. This property is only visible if __Mesh Decal Bias Type__ is set to __View Bias__. |
| _Depth Bias_              | A depth bias that URP applies to the decal’s Mesh to stop it from overlapping with other Meshes. A negative value draws the decal in front of any overlapping Mesh, while a positive value offsets the decal and draw it behind. This property only affects decal Materials directly attached to GameObjects with a Mesh Renderer, so Decal Projectors do not use this property. This property is only visible if __Mesh Decal Bias Type__ is set to __Depth Bias__. |
