# Six Way Master Stack reference

The Six Way master stack refers to the settings and contexts that the Six Way shader graph includes by default.

# Contexts

A Shader Graph contains the following contexts:

- [Vertex context](#vertex-context)
- [Fragment context](#fragment-context)

The Six Way Master Stack has its own [Graph Settings](#graph-settings) that determine which blocks you can use in the Shader Graph contexts. For more information about the relationship between Graph Settings and blocks, refer to [Contexts and blocks](understand-shader-graph-in-hdrp.md).

This section contains information on the blocks that this master stack material type uses by default, and which blocks you can use to affect the Graph Settings.

<a name="vertex-context"></a>

## Vertex context

The [Vertex context](vertex-context-reference.md) represents the vertex stage of a shader.

### Default

When you create a new Six Way Master Stack, the Vertex Context contains the following blocks by default:

| **Property** | **Description** | **Setting Dependency** | **Default Value** |
|---|---|---|---|
| **Position** | Defines the per-vertex position in object space. | None | `CoordinateSpace.Object` |
| **Normal** | Defines the per-vertex normal vector in object space; expects a unit-length direction (magnitude 1). | None | `CoordinateSpace.Object` |
| **Tangent** | Defines the per-vertex tangent vector in object space; expects a unit-length direction aligned with mesh UVs. | None | `CoordinateSpace.Object` |

<a name="fragment-context"></a>

## Fragment context

The [Fragment context](fragment-context-reference.md) represents the fragment stage of a shader.

### Default

When you create a new Six Way Master Stack, the Fragment Context contains the following blocks by default:

| **Property** | **Description** | **Setting Dependency** | **Default Value** |
|---|---|---|---|
| **Base Color** | Defines the base color of the material. | None | `Color.grey` |
| **Emission** | Defines the color of light emitted from the material’s surface. Emissive materials appear as light sources in the scene. | None | `Color.black` |
| **Ambient Occlusion** | Defines the material’s ambient occlusion (range 0–1). A value of 0 fully occludes a fragment (appears black) and 1 applies no occlusion (ambient color unchanged). | None | 1.0 |
| **Alpha** | Sets the material’s alpha (range 0–1). Determines transparency. | None | 1.0 |
| **Right Top Back** | Sets the color applied to surfaces facing the positive x-axis (right), positive y-axis (top), and negative z-axis (back). | None | `Color.grey` |
| **Left Bottom Front** | Sets the color applied to surfaces facing the negative x-axis (left), negative y-axis (bottom), and positive z-axis (front). | None | `Color.grey` |
| **Color Absorption Strength** | Controls how strongly the material absorbs light color as it passes through the object. Higher values increase the influence of the opposite face’s colors on visible shading. Default 0.5. Available only when **Use Color Absorption** is enabled in the graph settings. | **Use Color Absorption** enabled | 0.5 |

<a name="graph-settings"></a>

## Graph settings

The Graph Inspector contains the following properties.

### Surface options

| **Property** | **Option** | **Description** |
|---|---|---|
| **Surface Type** | N/A | Specifies whether the material supports transparency. Selecting **Transparent** increases rendering cost and exposes additional properties. The options are: <ul><li>**Opaque**</li><li>**Transparent:** Simulates a translucent material that light can penetrate (for example, clear plastic or glass).</li></ul> |
| **Surface Type** | **Rendering Pass** | Specifies the rendering pass HDRP uses for this material. Appears only when **Surface Type** is **Transparent**. The options are: <ul><li>**Default:** Draws the GameObject in the default opaque or transparent pass, based on the Surface Type.</li><li>**Before Refraction:** Draws the GameObject before the refraction pass so it contributes to refraction.</li><li>**After post-process:** Draws after post-processing (Unlit Materials only).</li></ul> |
| **Surface Type** | **Blending Mode** | Specifies how HDRP blends the material’s pixels with the background. Appears only when **Surface Type** is **Transparent**. The options are: <ul><li>**Alpha:** Uses the material alpha to control transparency (0 = fully transparent, 1 = visually opaque but rendered in the Transparent pass).</li><li>**Additive:** Adds the material RGB to the background; alpha scales the added intensity (0–1).</li><li>**Premultiply:** Assumes RGB is pre-multiplied by alpha for better filtering and compositing.</li></ul> |
| **Surface Type** | **Receive fog** | Indicates whether fog affects the transparent surface. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Depth Test** | Specifies the depth-test comparison function HDRP uses. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Depth Write** | Indicates whether HDRP writes depth for GameObjects using this material. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Sorting Priority** | Adjusts the rendering order of overlapping transparent surfaces. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Back Then Front Rendering** | Enables two-pass rendering that draws back faces first and front faces second. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Transparent Depth Prepass** | Adds transparent surface polygons to the depth buffer before the lighting pass to improve sorting. Not supported when the rendering pass is Low Resolution. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Transparent Depth Postpass** | Adds transparent surface polygons to the depth buffer after the lighting pass so they affect post-processing (for example, motion blur or depth of field). Not supported when the rendering pass is Low Resolution. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Transparent Writes Motion Vectors** | Enables writing motion vectors for transparent GameObjects to support effects like motion blur. Not supported when the rendering pass is Low Resolution. Appears only when **Surface Type** is **Transparent**. |
| **Surface Type** | **Cull Mode** | Specifies which faces to cull. The options are: <ul><li>**Back:** Culls back faces.</li><li>**Front:** Culls front faces.</li></ul> |
| **Alpha Clipping** | N/A | Enables cutout rendering by discarding pixels below a threshold to create sharp transparency edges. |
| **Double Sided** | N/A | Specifies how HDRP renders polygon faces. The options are: <ul><li>**Disabled:** Renders front faces only.</li><li>**Enabled:** Renders both faces with identical normals.</li><li>**Flipped Normals:** Renders both faces and flips back-face normals by 180°, making the material appear the same on both sides.</li><li>**Mirrored Normals:** Renders both faces and mirrors back-face normals, effectively inverting the material on the back face (useful for leaves).</li></ul> |
| **Depth Offset** | N/A | Modifies the depth buffer according to displacement so depth-based effects (for example, Contact Shadows) capture pixel-level displacement. |
| **Add Custom Velocity** | N/A | Adds a provided object-space velocity to motion-vector calculation to support procedural geometry while still accounting for other deformations (for example, skinning or vertex animation). |
| **Exclude from Temporal Upscaling and Anti Aliasing** | N/A | Excludes the surface from temporal upscalers and temporal anti-aliasing to reduce blurring on content such as animated textures. Works only for **Transparent** surfaces. |
| **Tessellation** | N/A | Enables mesh subdivision using tessellation according to the material’s tessellation options. |
| **Tessellation** | **Max Displacement** | Specifies the maximum world-space displacement in meters to aid culling. Sets this to the maximum displacement magnitude from the displacement map. |
| **Tessellation** | **Triangle Culling Epsilon** | Specifies the culling epsilon for tessellated triangles. Sets to -1.0 to disable back-face culling; increases the value for more aggressive culling and better performance. |
| **Tessellation** | **Start Fade Distance** | Sets the camera distance (meters) at which tessellation begins to fade out. Fades out until the value set for the **End Fade Distance**, where tessellation stops. |
| **Tessellation** | **End Fade Distance** | Sets the maximum camera distance (meters) at which HDRP tessellates triangles; beyond this distance, no tessellation occurs. |
| **Tessellation** | **Triangle Size** | Sets the screen-space triangle size (pixels) that triggers subdivision (for example, 100 subdivides triangles that cover 100 pixels). Lower values tessellate smaller triangles but increase cost. |
| **Tessellation** | **Tessellation Mode** | Specifies whether HDRP applies Phong tessellation. Materials can use a displacement map for tessellation. The options are: <ul><li>**None:** Uses displacement only (no tessellation if no displacement map is assigned).</li><li>**Phong:** Applies vertex interpolation to smooth geometry and displacement.</li></ul> |
| **Receive Shadows** | N/A | Determines whether the material receives and displays shadows cast by other objects; may affect batching performance. |
| **Use Color Absorption** | N/A | Enables simulation of light passing through the object and picking up color from the opposite side. When enabled, Unity adds the **Color Absorption Strength** block to the Fragment context. |
### Advanced options

The Graph Inspector contains the following advanced options.

| **Property** | **Description** |
|---|---|
| **Support Lod Crossfade** | Indicates whether HDRP processes dithering when a mesh moves moves from one LOD level to another. |
| **Add Precomputed Velocity** |   Indicates whether to use precomputed velocity information stored in an Alembic file.   |

### Other options

The Graph Inspector also contains the following options.

| **Property** | **Description** |
|---|---|
| **Custom Editor GUI** |   Renders a custom editor GUI in the **Inspector** window of the material. Enter the name of the GUI class in the field. For more information, refer to [Control material properties in the Inspector window](../writing-shader-display-types).   |
| **Support VFX Graph** |  Indicates whether this Shader Graph supports the Visual Effect Graph. If you enable this property, output contexts can use this Shader Graph to render particles. The internal setup that Shader Graph does to support visual effects happens when Unity imports the Shader Graph. This means that if you enable this property, but don't use the Shader Graph in a visual effect, there is no impact on performance. It only affects the Shader Graph import time.  |
| **Support High Quality Line Rendering** |  Indicates whether this Shader Graph supports the High Quality Line Rendering feature. Enabling this property will only have an effect on renderers with line topology.   |
