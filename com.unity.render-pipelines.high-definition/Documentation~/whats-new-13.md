# What's new in HDRP version 13 / Unity 2022.1

This page contains an overview of new features, improvements, and issues resolved in version 13 of the High Definition Render Pipeline (HDRP), embedded in Unity 2022.1.

## Added

### Material Samples Transparency Scenes

![](Images/HDRP-MaterialSample-ShadowsTransparency.png)

These new scenes include examples and informations on how to setup properly transparents in your projects using different rendering methods (Rasterization, Ray Tracing, Path Tracing).
To take advantage of all the content of the sample, a GPU that supports [Ray Tracing](Ray-Tracing-Getting-Started.md) is needed.

### Material Runtime API

From this HDRP version, you can use new APIs to run shader validation steps from script at runtime, both in the editor and in standalone builds. You can use this to change the keyword state or one or more properties in order to enable or disable HDRP shader features on a Material.
For more information, see the [Material Scripting API documentation](Material-API.md).

### Access main directional light from ShaderGraph

From HDRP version 13.0, [ShaderGraph](https://docs.unity3d.com/Packages/com.unity.shadergraph@13.1/manual/index.html) includes a new node called *Main Light Direction* that you can use to control the direction of the main light.
For more information, see the [Main Light Direction Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@13.1/manual/Main-Light-Direction-Node.html).

## Updated

### Depth Of Field
HDRP version 13 includes optimizations in the physically based depth of field implementation. In particular, image regions that are out-of-focus are now computed at lower resolution, while in-focus regions retain the full resolution. For many scenes this results in significant speedup, without any visible reduction in image quality.
