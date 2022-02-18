# Tessellation

The **Tessellation** options control how Unity tessellates your Material's surface and smooths geometry.

If you enable this feature, HDRP exposes the following properties to control how Unity tessellates your Material's surface and smooths geometry:

| **Properties**               | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Triangle Culling Epsilon** | Specifies how HDRP culls tessellated triangles. If you want to disable back-face culling, set this to **-1.0**. If you want more aggressive culling and better performance, set this to a higher value. |
| **Start Fade Distance**      | The distance (in meters) to the Camera at which tessellation begins to fade out. HDRP fades tessellation out from this distance up until **End Fade Distance**, at which point it stops tessellating triangles altogether. |
| **End Fade Distance**        | The maximum distance (in meters) to the Camera at which HDRP tessellates triangles. HDRP does not tessellate triangles at distances that are further from the Camera further than this distance. |
| **Triangle Size**            | The screen space size (in pixels) at which HDRP should subdivide a triangle. For example, if you set this value to **100**, HDRP subdivides triangles that take up 100 pixels. If you want HDRP to tessellate smaller triangles, and thus produce smoother geometry, set this to a lower value.<br/>Note: increasing the number of triangles that this Shader tessellates makes the effect more resource intensive to process. |
| **Tessellation Mode**        | Specifies whether HDRP applies Phong tessellation or not. Materials can use a [displacement map](Displacement-Mode.md) to tessellate a mesh. To smooth the result of displacement, you can also apply Phong tessellation. The options for the property are:<br/>&#8226; **None**: HDRP only uses the displacement map to tessellate the mesh. If you do not assign a displacement map for this Material and select this option, HDRP does not apply tessellation.<br/>&#8226; **Phong**: HDRP applies Phong tessellation to the mesh. Phong tessellation applies vertex interpolation to make geometry smoother. If you assign a displacement map for this Material and select this option, HDRP applies smoothing to the displacement map. |
| **Shape Factor**             | To smooth the Mesh surface, Phong tessellation spherizes the Mesh. This property represents the strength of the spherization effect. If you do not want HDRP to spherize the Mesh, set this to **0**. If you want HDRP to fully spherize the Mesh, set this to **1**.<br/>This property only appears when you select **Phong** from **Tessellation Mode**. |

The following property is only available in the [Layered Lit Tessellation Shader](layered-lit-tessellation-shader.md) and [Lit Tessellation Shader](Lit-Tessellation-Shader.md). For [Master Stacks](master-stack-hdrp.md) this option appears as an input.

| **Properties**               | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Tessellation Factor**      | The number of subdivisions that a triangle can have. If you want more subdivisions, set this to a higher value. More subdivisions increase the strength of the tessellation effect and further smooths the geometry. Note that higher values also increase the resource intensity of the tessellation effect. To maintain good performance on the Xbox One or PlayStation 4, do not use values greater than 15. This is because these platforms cannot consistently handle this many subdivisions. |

The following property is only available in Master Stacks. In the [Layered Lit Tessellation Shader](layered-lit-tessellation-shader.md) and [Lit Tessellation Shader](Lit-Tessellation-Shader.md), Unity calculates it automatically from displacement.

| **Properties**               | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Max Displacement**         | Maximun possible world displacement in meter for the Material. This is required to help the culling algorithm to not discard vertices which could be out of bounds. It is recommend to setup this value to maximun size extracted from the displacement map. |

## Limitation

- [Decal Meshes](Decal.md) do not support tessellation.
- The [Visual Effect Graph](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html) does not support tessellation.
- HDRP does not support tessellation for [ray tracing](Ray-Tracing-Getting-Started.md), tessellated meshes fall back to a non-tessellated version.
- When using world-space displacement, the object can expand out of its original bounds. You need to adjust the bounds manually to get correct rendering.
- Motion vectors do not work correctly if the tessellation factor differs for vertices between two frames.

## Performance
- Enabling the Tessellation option have an extra GPU cost, even if the tessellationFactor is 0.0 or 1.0. It is recommened to have additional LOD with shader without tessellation enabled for good performance.
- Tessellation is an expensive GPU operation and it is often lest costly to pre-tessellate a mesh and doing vertex displacement than doing the tessellation process, but it have the benefit of being adapatative.
