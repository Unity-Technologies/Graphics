# StackLit

The StackLit Master Stack can render materials that are more complex than the [Lit Master Stack](master-stack-lit.md). It includes all the features available in the Lit shader and, sometimes, provides more advanced or higher quality versions. For example, it uses a more advanced form of specular occlusion and also calculates anisotropic reflections for area lights in the same way the Lit shader does for other light types. It also takes into account light interactions between two vertically stacked physical layers, along with a more complex looking general base layer.

![](Images/HDRPFeatures-StackLitShader.png)

## Creating a StackLit Shader Graph

To create a StackLit material in Shader Graph, you can either:

* Modify an existing Shader Graph.
    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button, and select **HDRP**.
    3. In the **Material** drop-down, select **StackLit**.

* Create a new Shader Graph. Go to **Assets** > **Create** > **Shader Graph** > **HDRP** and select **StackLit Shader Graph**.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

When you create a new Hair Master Stack, the Vertex Context contains the following Blocks by default:
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

When you create a new Hair Master Stack, the Fragment Context contains the following Blocks by default:

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
[!include[](snippets/shader-graph-blocks/tangent-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/metallic.md)]
[!include[](snippets/shader-graph-blocks/dielectric-ior.md)]
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]
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
[!include[](snippets/shader-graph-blocks/coat-extinction.md)]
[!include[](snippets/shader-graph-blocks/coat-ior.md)]
[!include[](snippets/shader-graph-blocks/coat-smoothness.md)]
[!include[](snippets/shader-graph-blocks/coat-thickness.md)]
[!include[](snippets/shader-graph-blocks/coat-mask.md)]
[!include[](snippets/shader-graph-blocks/coat-normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/coat-normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/coat-normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/depth-offset.md)]
[!include[](snippets/shader-graph-blocks/diffusion-profile.md)]
[!include[](snippets/shader-graph-blocks/iridescence-coat-fixup-tir.md)]
[!include[](snippets/shader-graph-blocks/iridescence-coat-fixup-tir-clamp.md)]
[!include[](snippets/shader-graph-blocks/iridescence-thickness.md)]
[!include[](snippets/shader-graph-blocks/iridescence-mask.md)]
[!include[](snippets/shader-graph-blocks/haze-extent.md)]
[!include[](snippets/shader-graph-blocks/haziness.md)]
[!include[](snippets/shader-graph-blocks/hazy-gloss-max-dielectric-f0.md)]
[!include[](snippets/shader-graph-blocks/lobe-mix.md)]
[!include[](snippets/shader-graph-blocks/normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/smoothness-b.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-screen-space-variance.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-threshold.md)]
[!include[](snippets/shader-graph-blocks/specular-color.md)]
[!include[](snippets/shader-graph-blocks/specular-occlusion.md)]
[!include[](snippets/shader-graph-blocks/subsurface-mask.md)]
[!include[](snippets/shader-graph-blocks/tangent-object-space.md)]
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
[!include[](snippets/shader-properties/surface-options/base-color-parametrization.md)]
[!include[](snippets/shader-properties/surface-options/energy-conserving-specular.md)]
[!include[](snippets/shader-properties/surface-options/anisotropy-stacklit.md)]
[!include[](snippets/shader-properties/surface-options/coat.md)]
[!include[](snippets/shader-properties/surface-options/coat-normal.md)]
[!include[](snippets/shader-properties/surface-options/dual-specular-lobe.md)]
[!include[](snippets/shader-properties/surface-options/dual-specular-lobe-parametrization.md)]
[!include[](snippets/shader-properties/surface-options/cap-haziness-for-non-metallic.md)]
[!include[](snippets/shader-properties/surface-options/iridescence.md)]
[!include[](snippets/shader-properties/surface-options/transmission.md)]
[!include[](snippets/shader-properties/surface-options/specular-occlusion-mode-stacklit.md)]
[!include[](snippets/shader-properties/surface-options/anisotropy-for-area-lights.md)]
[!include[](snippets/shader-properties/surface-options/base-layer-uses-refracted-angles.md)]
[!include[](snippets/shader-properties/surface-options/recompute-stack-and-iridescence.md)]
[!include[](snippets/shader-properties/surface-options/honor-per-light-max-smoothness.md)]
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
