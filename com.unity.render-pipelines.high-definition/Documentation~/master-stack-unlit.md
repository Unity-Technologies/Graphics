# Unlit

The Unlit Master Stack lets you create shaders that are not affected by lighting.



![](Images/HDRPFeatures-UnlitShader.png)

## Creating an Unlit Shader Graph

To create an Unlit material in Shader Graph, you can either:

* Modify an existing Shader Graph.

    1. Open the Shader Graph in the Shader Editor.
    2. In **Graph Settings**, select the **HDRP** Target. If there isn't one, go to **Active Targets,** click the **Plus** button and select **HDRP**.
    3. In the **Material** drop-down, select **Unlit**.

* Create a new Shader Graph.

    1. Go to **Assets > Create >Shader Graph > HDRP** and click **Unlit Shader Graph**.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

When you create a new Unlit Master Stack, the Vertex Context contains the following Blocks by default:

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

When you create a new Unlit Master Stack, the Fragment Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>
[!include[](snippets/shader-graph-blocks/base-color.md)]
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
[!include[](snippets/shader-graph-blocks/depth-offset.md)]
[!include[](snippets/shader-graph-blocks/distortion.md)]
[!include[](snippets/shader-graph-blocks/distortion-blur.md)]
[!include[](snippets/shader-graph-blocks/shadow-tint.md)]
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
[!include[](snippets/shader-properties/surface-options/alpha-clipping.md)]
[!include[](snippets/shader-properties/surface-options/use-shadow-threshold.md)]
[!include[](snippets/shader-properties/surface-options/alpha-to-mask.md)]
[!include[](snippets/shader-properties/surface-options/double-sided.md)]
[!include[](snippets/shader-properties/surface-options/ss-depth-offset.md)]
[!include[](snippets/shader-properties/surface-options/conservative-depth-offset.md)]
[!include[](snippets/shader-properties/surface-options/velocity.md)]
[!include[](snippets/shader-properties/surface-options/tessellation.md)]
[!include[](snippets/shader-properties/surface-options/shadow-matte.md)]
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
[!include[](snippets/shader-properties/distortion/distortion-only.md)]
</table>

### Advanced Options

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
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
