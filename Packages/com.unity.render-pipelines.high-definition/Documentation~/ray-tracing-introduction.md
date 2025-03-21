# Introduction to ray tracing

Use ray tracing to simulate realistic lighting in your project.

Ray tracing is a technique that implements advanced lighting and reflection effects by modeling light transport. This technique allows you to access data that's not on screen. For example, you can use it to request position data, normal data, or lighting data, and then use this data to compute quantities that are hard to approximate using classic rasterization techniques. For more information on the techniques you can use with ray tracing, refer to [Ray-traced effects](lighting-ray-traced-effects.md). Ray tracing support in the High Definition Render Pipeline (HDRP) is in a preview state.

**Note:** Ray tracing is a resource-intensive process. The more ray tracing effects a project uses, the longer it takes the GPU to process the effects and render a frame.

## Ray tracing requirements

For information about the hardware ray tracing requires, refer to [Ray tracing hardware requirements](raytracing-requirements.md).

## Ray tracing modes

HDRP includes two ray tracing modes that define how it evaluates certain ray-traced effects. The modes are:

* **Performance**: This mode is optimized for real-time applications and provides a good balance of performance and quality.
* **Quality**: This mode prioritizes quality over performance and is suitable for applications that require highest visual fidelity.

HDRP exposes different properties for some ray-traced effects based on the ray tracing mode you use.

You can change which ray tracing mode HDRP uses on either a project level or effect level. To change the ray tracing mode for your entire project:

1. Select your [HDRP Asset](HDRP-Asset.md) in the **Project** window to view it in the **Inspector** window.
2. In the **Rendering** section, enable the **Realtime Raytracing** checkbox.
3. Open the **Supported Ray Tracing Mode** dropdown and select a ray tracing mode.

If you select the **Both** option, you can change the ray tracing mode for each ray-traced effect. To do this:

1. Select a GameObject that contains a **Volume** component that includes a ray-traced effect.
2. In the **Inspector** window for the ray-traced effect, change the **Mode** property to use the ray tracing mode you want the effect to use. This changes the properties available in the **Inspector** window.

## Ray tracing effects

Using ray tracing in your project changes the way HDRP handles meshes and light culling.

### Ray tracing with meshes

HDRP changes how it handles meshes in your scene when you integrate a ray traced effect into your project.

When you enable ray tracing, HDRP automatically creates a ray tracing acceleration structure. This structure allows Unity to calculate ray tracing for meshes in your scene efficiently in real time.

As a result, ray tracing can change how some meshes appear in your scene in the following ways:

- If your mesh has a material assigned that doesn't have the **HDRenderPipeline** tag, HDRP doesn't add it to the acceleration structure and doesn't apply any ray traced effects to the mesh as a result.
- If a mesh has a combination of materials that are single and double-sided, HDRP considers all the materials you have assigned to this mesh as double-sided.

To include a GameObject in ray tracing effects, adjust the Ray Tracing settings in the [Mesh Renderer component](https://docs.unity3d.com/Manual/class-MeshRenderer.html#ray-tracing).

### Ray tracing with light culling

Ray tracing requires HDRP to cull lights differently to how it culls lights for rasterization. With rasterization, only lights that affect the current frustum matter. Since ray tracing uses off-screen data for effects such as reflection, HDRP needs to consider lights that affect off-screen geometry. For this reason, HDRP defines a range around the camera where it gathers light. To control this range, use the [Light Cluster](Ray-Tracing-Light-Cluster.md) Volume override. It's important to set a range that accurately represents the environment scale. A higher range makes HDRP include lights further away, but it also increases the resource intensity of light culling for ray tracing.

## Ray tracing limitations

### Platform support

HDRP supports ray tracing for [DirectX 12](https://docs.unity3d.com/Manual/UsingDX11GL3Features.html#comparison-of-directx11-and-directx12-in-unity) and specific console platforms. For more information, refer to console-specific documentation.

### Feature compatibility

Ray tracing in HDRP isn't compatible with the following features.

#### Rendering techniques

- Ray-traced and screen space effects. These effects don't appear in [ray-traced reflections](Ray-Traced-Reflections.md), [ray-traced global illumination](Ray-Traced-Global-Illumination.md), or [recursive ray tracing](Ray-Tracing-Recursive-Rendering.md). For example, this means you can't see [screen space global illumination](Override-Screen-Space-GI.md) in [ray-traced reflections](Ray-Traced-Reflections.md).
- [Graphics.DrawMesh](https://docs.unity3d.com/ScriptReference/Graphics.DrawMesh.html) and [Graphics.RenderMesh](https://docs.unity3d.com/ScriptReference/Graphics.RenderMesh.html), because rasterization and ray tracing are different ways of generating an image.
- [Orthographic projection](hdrp-camera-component-reference.md). If you enable orthographic projection mode, you might experience rendering problems with transparent materials, volumetrics, and planar reflections.
- [Multi-sample anti-aliasing (MSAA)](Anti-Aliasing.md#MSAA).

#### Lighting and shadows

- Fully accurate shadow culling. You might see missing shadows in ray-traced effects. To improve the accuracy of shadow culling, use the **Extend Shadow Culling** property in [Ray Tracing Settings](reference-ray-tracing-settings.md).
- Volumetric [fog](create-a-local-fog-effect.md).
- Water.

#### Geometry and materials

- Per-pixel displacement techniques such as parallax occlusion mapping, depth offset, and non-terrain height maps.
- The detail meshes and trees in the [Terrain system](https://docs.unity3d.com/Manual/script-Terrain.html). To include detailed meshes and trees in ray traced reflections, use [mixed tracing](Override-Screen-Space-Reflection.md#mixed-tracing).
- Vertex animation, for example wind deformation of vegetation.
- [Decals](decals.md).
- [Tessellation](Tessellation.md).

#### Framework and workflow

- Data-Oriented Technology Stack (DOTS).
- Entity Component System (ECS).

#### Shader Graph compatibility

- [Custom Interpolators](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Custom-Interpolators.html).
- [DDX](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/DDX-Node.html), [DDY](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/DDY-Node.html), and [DDXY](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/DDXY-Node.html) nodes.
- [Geometry nodes](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Input-Nodes.html#geometry) in View Space mode.
- [Normal From Height](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Normal-From-Height-Node.html) node.

Use the [Ray Tracing Quality node](SGNode-Raytracing-Quality.md) instead of these features.

#### Reflection Probe compatibility

Although ray-traced rendering results include data from [Reflection Probes](Reflection-Probe.md), Reflection Probes don't capture the geometry that HDRP renders with ray tracing.

## Additional resources

- [Small Office Ray Tracing sample project](https://github.com/Unity-Technologies/SmallOfficeRayTracing)
- [Path tracing limitations](path-tracing-limitations.md)
- [Set up ray tracing](Ray-Tracing-Getting-Started.md)