## Path tracing limitations

This section contains information on the limitations of HDRP's path tracing implementation. Mainly, this is a list of features that HDRP supports in its rasterized render pipeline, but not in its path-traced render pipeline.

### Unsupported features of path tracing

Currently, you can only use HDR path tracing on platforms that use DX12.

HDRP path tracing in Unity currently has the following limitations:

- If a Mesh in your scene has a Material assigned that does not have the `HDRenderPipeline` tag, the mesh will not appear in your scene. For more information, see [Ray tracing and Meshes](Ray-Tracing-Getting-Started.md#RayTracingMeshes).
- Path tracing in HDRP doesn't support the following:
  - 3D Text and TextMeshPro.
  - Shader Graph nodes that use derivatives (for example, a normal map that derives from a texture).
  - Shader Graphs that use [Custom Interpolators](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Custom-Interpolators.html).
  - Local Volumetric Fog.
  - Tessellation.
  - Translucent Opaque Materials.
  - Several of HDRP's Materials. This includes Eye and Hair.
  - Emissive [Decals](decals.md). To disable emission, go to the [Decal Material Inspector window](decal-material-inspector-reference.md) and disable **Affect Emissive**.
  - Per-pixel displacement (parallax occlusion mapping, height map, depth offset).
  - Emissive Decals.
  - Volumetric Clouds.
  - Water. 
  - MSAA.
  - [Graphics.DrawMesh](https://docs.unity3d.com/ScriptReference/Graphics.DrawMesh.html) or [Graphics.RenderMesh](https://docs.unity3d.com/2022.1/Documentation/ScriptReference/Graphics.RenderMesh.html), because rasterization and ray tracing are different ways of generating an image.
  - [Streaming Virtual Texturing](https://docs.unity3d.com/Documentation/Manual/svt-streaming-virtual-texturing.html).
  - Vertex animation, for example wind deformation of vegetation.

### Unsupported shader graph nodes for path tracing

When building your custom shaders using shader graph, some nodes are incompatible with ray/path tracing. You need either to avoid using them or provide an alternative behavior using the [ray tracing shader node](SGNode-Raytracing-Quality.md). Here is the list of the incompatible nodes:

- DDX, DDY, DDXY, NormalFromHeight and HDSceneColor nodes.
- All the nodes under Inputs > Geometry (Position, View Direction, Normal, etc.) in View Space mode.
  Furthermore, Shader Graphs that use [Custom Interpolators](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Custom-Interpolators.html) aren't supported in ray/path tracing.

### Unsupported features of ray tracing

For information about unsupported features of ray tracing in general, see [Ray tracing limitations](Ray-Tracing-Getting-Started.md#limitations).
