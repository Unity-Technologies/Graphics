# Terrain Master Stack reference

You can modify the properties of a Terrain shader graph in the Terrain Master Stack.

## Feature support

The Terrain Master Stack in HDRP doesn't support the following features:

- Ray tracing
- Path tracing

## Terrain shader passes

Some properties might not work as expected, because the terrain system adds two additional shader passes when it generates a terrain shader. The passes are the following:

- Basemap Gen, which renders the shader graph on a quad to create a low-resolution baked version. As a result, the **Position** and **Normal (Tangent Space)** values are 2D during this pass.
- Basemap Rendering, which uses a built-in shader and the texture Basemap Gen generates. The **Position** Block has no affect on this pass.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

When you create a new Terrain Master Stack, the Vertex Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/vertex-position.md)]

</table>

#### Relevant

The vertex position passed to the vertex context does not have an effect on the [Basemap rendering pass](#basemap-passes).

### Fragment Context

#### Default

When you create a new Terrain Master Stack, the Fragment Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/base-color.md)]
[!include[](snippets/shader-graph-blocks/normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/metallic.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/ambient-occlusion.md)]

</table>

#### Relevant

Depending on the [Graph Settings](#graph-settings) you use, Shader Graph can add the following Blocks to the Fragment Context:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/alpha-clip-threshold.md)]
[!include[](snippets/shader-graph-blocks/baked-back-gi.md)]
[!include[](snippets/shader-graph-blocks/baked-gi.md)]
[!include[](snippets/shader-graph-blocks/depth-offset.md)]
[!include[](snippets/shader-graph-blocks/diffusion-profile.md)]
[!include[](snippets/shader-graph-blocks/normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-screen-space-variance.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-threshold.md)]
[!include[](snippets/shader-graph-blocks/specular-occlusion.md)]
[!include[](snippets/shader-graph-blocks/subsurface-mask.md)]

</table>

## Graph Settings

### Surface Options

<table>
<thead>
  <tr>
    <th>Property</th>
    <th>Option</th>
    <th>Sub-option</th>
    <th>Description</th>
  </tr>
</thead>
<tbody>

[!include[](snippets/shader-properties/surface-options/rendering-pass.md)]
[!include[](snippets/shader-properties/surface-options/fragment-normal-space.md)]
[!include[](snippets/shader-properties/surface-options/alpha-clipping.md)]
[!include[](snippets/shader-properties/surface-options/ss-depth-offset.md)]
[!include[](snippets/shader-properties/surface-options/receive-decals.md)]
[!include[](snippets/shader-properties/surface-options/receive-ssr.md)]

</tbody>
</table>

</table>

### Advanced Options

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>

[!include[](snippets/shader-properties/advanced-options/specular-occlusion-mode.md)]
[!include[](snippets/shader-properties/advanced-options/override-baked-gi.md)]
[!include[](snippets/shader-properties/advanced-options/support-lod-crossfade.md)]
[!include[](snippets/shader-properties/advanced-options/add-precomputed-velocity.md)]

</table>

### Other top level settings
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>

[!include[](snippets/shader-properties/support-high-quality-line-rendering.md)]

</table>