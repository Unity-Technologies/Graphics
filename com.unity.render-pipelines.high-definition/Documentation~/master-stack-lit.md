# Lit

The Lit Master Stack enables you to render custom physically based materials in the High Definition Render Pipeline (HDRP). It supports various effects, such as subsurface scattering, anisotropy, iridescence, specular color and translucency.

![](Images/HDRPFeatures-LitShader.png)

## Creating a Lit Shader Graph

To create a Lit material in Shader Graph, use one of the following methods:

* Modify an existing Shader Graph.
    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button, and select **HDRP**.
    3. In the **Material** drop-down, select **Lit**.

* Create a new Shader Graph. Go to **Assets** > **Create** > **Shader Graph** > **HDRP** and select **Lit Shader Graph**.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

When you create a new Lit Master Stack, the Vertex Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/vertex-position.md)]
[!include[](snippets/shader-graph-blocks/vertex-normal.md)]
[!include[](snippets/shader-graph-blocks/vertex-tangent.md)]
</table>

#### Relevant
Depending on the [Graph Settings](#graph-settings) you use, Shader Graph can add the following Blocks to the Vertex Context:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/tessellation-factor.md)]
[!include[](snippets/shader-graph-blocks/tessellation-displacement.md)]
</table>


### Fragment Context

#### Default

When you create a new Lit Master Stack, the Fragment Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/base-color.md)]
[!include[](snippets/shader-graph-blocks/normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/bent-normal.md)]
[!include[](snippets/shader-graph-blocks/metallic.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/ambient-occlusion.md)]
[!include[](snippets/shader-graph-blocks/alpha.md)]
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
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-depth-postpass.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-depth-prepass.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-shadow.md)]
[!include[](snippets/shader-graph-blocks/anisotropy.md)]
[!include[](snippets/shader-graph-blocks/baked-back-gi.md)]
[!include[](snippets/shader-graph-blocks/baked-gi.md)]
[!include[](snippets/shader-graph-blocks/coat-mask.md)]
[!include[](snippets/shader-graph-blocks/depth-offset.md)]
[!include[](snippets/shader-graph-blocks/diffusion-profile.md)]
[!include[](snippets/shader-graph-blocks/distortion.md)]
[!include[](snippets/shader-graph-blocks/distortion-blur.md)]
[!include[](snippets/shader-graph-blocks/iridescence-mask.md)]
[!include[](snippets/shader-graph-blocks/iridescence-thickness.md)]
[!include[](snippets/shader-graph-blocks/normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/refraction-index.md)]
[!include[](snippets/shader-graph-blocks/refraction-color.md)]
[!include[](snippets/shader-graph-blocks/refraction-distance.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-screen-space-variance.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-threshold.md)]
[!include[](snippets/shader-graph-blocks/specular-color.md)]
[!include[](snippets/shader-graph-blocks/specular-occlusion.md)]
[!include[](snippets/shader-graph-blocks/subsurface-mask.md)]
[!include[](snippets/shader-graph-blocks/transmission-mask.md)]
[!include[](snippets/shader-graph-blocks/tangent-object-space.md)]
[!include[](snippets/shader-graph-blocks/tangent-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/tangent-world-space.md)]
[!include[](snippets/shader-graph-blocks/thickness.md)]
</table>

## Graph Settings

### Surface Options

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/surface-options/material-type.md)]
[!include[](snippets/shader-properties/surface-options/recursive-rendering.md)]
[!include[](snippets/shader-properties/surface-options/surface-type.md)]
[!include[](snippets/shader-properties/surface-options/rendering-pass.md)]
[!include[](snippets/shader-properties/surface-options/blending-mode.md)]
[!include[](snippets/shader-properties/surface-options/receive-fog.md)]
[!include[](snippets/shader-properties/surface-options/depth-test.md)]
[!include[](snippets/shader-properties/surface-options/depth-write.md)]
[!include[](snippets/shader-properties/surface-options/cull-mode.md)]
[!include[](snippets/shader-properties/surface-options/sorting-priority.md)]
[!include[](snippets/shader-properties/surface-options/back-then-front-rendering.md)]
[!include[](snippets/shader-properties/surface-options/transparent-depth-prepass.md)]
[!include[](snippets/shader-properties/surface-options/transparent-depth-postpass.md)]
[!include[](snippets/shader-properties/surface-options/transparent-writes-motion-vectors.md)]
[!include[](snippets/shader-properties/surface-options/preserve-specular-lighting.md)]
[!include[](snippets/shader-properties/surface-options/alpha-clipping.md)]
[!include[](snippets/shader-properties/surface-options/use-shadow-threshold.md)]
[!include[](snippets/shader-properties/surface-options/alpha-to-mask.md)]
[!include[](snippets/shader-properties/surface-options/double-sided-mode.md)]
[!include[](snippets/shader-properties/surface-options/fragment-normal-space.md)]
[!include[](snippets/shader-properties/surface-options/receive-decals.md)]
[!include[](snippets/shader-properties/surface-options/receive-ssr.md)]
[!include[](snippets/shader-properties/surface-options/receive-ssr-transparent.md)]
[!include[](snippets/shader-properties/surface-options/geometric-specular-aa.md)]
[!include[](snippets/shader-properties/surface-options/ss-depth-offset.md)]
[!include[](snippets/shader-properties/surface-options/conservative-depth-offset.md)]
[!include[](snippets/shader-properties/surface-options/velocity.md)]
[!include[](snippets/shader-properties/surface-options/tessellation.md)]
[!include[](snippets/shader-properties/surface-options/clear-coat.md)]
[!include[](snippets/shader-properties/surface-options/transmission.md)]
[!include[](snippets/shader-properties/transparency-inputs/refraction-model.md)]
[!include[](snippets/shader-properties/surface-options/energy-conserving-specular-color.md)]
</table>

### Distortion

This set of settings only appears if you set **Surface Type** to **Transparent**.

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/distortion/distortion.md)]
[!include[](snippets/shader-properties/distortion/distortion-blend-mode.md)]
[!include[](snippets/shader-properties/distortion/distortion-depth-test.md)]
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
[!include[](snippets/shader-properties/support-vfx-graph.md)]
</table>
