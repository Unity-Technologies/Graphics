# Eye Master Stack reference

You can modify the properties of an Eye Shader Graph in the Eye Master Stack.

Refer to [Eyes](eyes.md) for more information.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

When you create a new Eye Master Stack, the Vertex Context contains the following Blocks by default:

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

When you create a new Eye Master Stack, the Fragment Context contains the following Blocks by default:

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
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/eye-ior.md)]
[!include[](snippets/shader-graph-blocks/ambient-occlusion.md)]
[!include[](snippets/shader-graph-blocks/mask.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]
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
[!include[](snippets/shader-graph-blocks/baked-back-gi.md)]
[!include[](snippets/shader-graph-blocks/baked-gi.md)]
[!include[](snippets/shader-graph-blocks/depth-offset.md)]
[!include[](snippets/shader-graph-blocks/diffusion-profile.md)]
[!include[](snippets/shader-graph-blocks/iris-normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/iris-normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/iris-normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-screen-space-variance.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-threshold.md)]
[!include[](snippets/shader-graph-blocks/specular-occlusion.md)]
[!include[](snippets/shader-graph-blocks/subsurface-mask.md)]
[!include[](snippets/shader-graph-blocks/iris-plane-offset.md)]
[!include[](snippets/shader-graph-blocks/iris-radius.md)]
[!include[](snippets/shader-graph-blocks/iris-caustic-intensity-multiplier.md)]
[!include[](snippets/shader-graph-blocks/iris-caustic-blend.md)]

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

[!include[](snippets/shader-properties/surface-options/eye-material-type.md)]
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
[!include[](snippets/shader-properties/surface-options/exclude-from-taau.md)]
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
[!include[](snippets/shader-properties/surface-options/subsurface-scattering.md)]
[!include[](snippets/shader-properties/surface-options/iris-normal.md)]

</tbody>
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
[!include[](snippets/shader-properties/support-high-quality-line-rendering.md)]

</table>
