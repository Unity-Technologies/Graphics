# Custom Pass buffers and pyramids

This section describes how depth and color buffers work with custom passes in HDRP.

When HDRP renders a scene, it stores some data to a render target or buffer, and uses this data to create a series of mipmaps. This series of mipmaps is called a pyramid. Custom passes in HDRP use depth pyramids and color pyramids. You can reference a pyramid in a shader.

HDRP creates depth and color pyramids at given points in the render pipeline. For more information, see [Depth pyramid and color pyramid generation in HDRP](#Custom-Pass-pyramid-generation).

<a name="Custom-Pass-buffers"></a>

## Custom pass buffers

HDRP custom passes use the following buffers:

- [Depth buffer](#Custom-Pass-depth-buffer)
- [Color buffer](#Custom-Pass-color-buffer)

You can set each of these buffers to use:

- The camera’s color or depth buffer.
- A [custom buffer](custom-pass-create-gameobject.md#use-the-custom-buffer) under your control.
- No buffer to disable color or depth outputs.

<a name="Custom-Pass-depth-buffer"></a>

### Depth buffer

During rendering, HDRP uses a depth buffer to perform depth tests. By default, only opaque objects write to the depth buffer up to and including the BeforePreRefraction [injection point](Custom-Pass-Injection-Points.md).

However, you can also make HDRP write transparent objects to the depth buffer. To do this, go to [Surface options](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.4/manual/Lit-Shader.html) and either enable **Depth Write**, **Transparent Depth Prepass** or **Transparent Depth Postpass** depending on when you want them to write to the depth buffer**.**

You can only read the current depth buffer from a rendering shader if you bind it in a C# custom pass script.

HDRP uses this data to generate a [depth pyramid](#Custom-Pass-depth-pyramid).

<a name="Custom-Pass-color-buffer"></a>

### Color buffer

During rendering, HDRP writes color data from visible objects (renderers) in the scene to the color buffer. You cannot read the current color buffer from a rendering shader.

<a name="Custom-Pass-buffers-pyramids"></a>

## Custom pass pyramids

During a custom pass, HDRP creates the following pyramids:

- [Depth pyramid](#Custom-Pass-depth-pyramid)
- [Color pyramid](#Custom-Pass-color-pyramid)

The term pyramid comes from the fact that each pyramid is a texture with a full chain of mipmaps.

<a name="Custom-Pass-depth-pyramid"></a>

### Depth pyramid

A depth pyramid is a series of mipmaps that HDRP creates from data in the depth buffer at a given point in the rendering pipeline. HDRP builds each successive mipmap level of the depth pyramid by downscaling the previous level’s depth values using the minimum (closest) depth value of each 4x4 block of pixels.

This pyramid reduces the time HDRP takes to render effects that need depth information like SSAO by providing a hierarchical depth buffer.

<a name="Custom-Pass-color-pyramid"></a>

### Color pyramid

A color pyramid is a series of mipmaps that HDRP creates from the color buffer at a given point in the rendering pipeline. HDRP builds each successive mipmap level of the color pyramid by downscaling and blurring the previous level’s color values.

To access the color pyramid in shader graph, use the [Scene Color node.](https://docs.unity3d.com/Packages/com.unity.shadergraph@11.0/manual/Scene-Color-Node.html?q=scene)

<a name="Custom-Pass-pyramid-generation"></a>

## Depth pyramid and color pyramid generation in HDRP

HDRP creates a color pyramid and a depth pyramid at specific points in the rendering pipeline to capture a snapshot of the scene’s color and depth buffers for use as input to shaders in subsequent passes.

HDRP generates a single depth pyramid after the AfterOpaqueDepthAndNormal [injection point](Custom-Pass-Injection-Points.md). This depth pyramid therefore contains only opaque objects. For more information, see [Depth pyramid](#Custom-Pass-depth-pyramid).

HDRP generates color pyramids at these points in the rendering pipeline:

1. After the BeforePreRefraction injection point (in **Color Pyramid PreRefraction**). This contains opaque objects and objects rendered in BeforePreRefraction and is used for [Screen space reflections](Override-Screen-Space-Reflection.md) next frame and the [refraction effect](Override-Screen-Space-Refraction.md) in the current frame.
2. If **Distortion** is enabled in the [Frame Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.4/manual/Frame-Settings.html), after the BeforeTransparent injection point and all the Transparent rendering passes. This is used for distortion and contains all opaque and transparent objects.
3. As part of the bloom post-processing effect. This contains all opaque and transparent objects and the distortion effect.

**Note**: The term color pyramid usually refers to the first pyramid in this list.
