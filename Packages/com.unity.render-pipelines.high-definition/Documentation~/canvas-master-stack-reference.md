# Canvas Master Stack reference

You can modify the properties of a Canvas Shader Graph in the Canvas Master Stack.

Refer to [Canvas material](canvas-material.md) for more information.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

The Vertex context represents the vertex stage of this shader. Unity executes any block you connect to this context in the vertex function of this shader. For more information, refer to [Master Stack](https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Master-Stack.html).

Vertex blocks are not compatible with the Canvas Master Stack.

### Fragment Context

The Fragment Context contains the default and relevant Blocks for the Canvas Master Stack target.

#### Default Fragment Context Blocks

When you create a new Canvas Master Stack, the Fragment Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/base-color.md)]
[!include[](snippets/shader-graph-blocks/alpha.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]

</table>

#### Relevant Fragment Context Blocks

Depending on the [Graph Settings](#graph-settings) you use, Shader Graph might add the following Blocks to the Fragment Context:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/alpha-clip-threshold.md)]

## Graph Settings

The following table describes the Surface options:

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>

[!include[](snippets/shader-properties/surface-options/material-type.md)]
[!include[](snippets/shader-properties/surface-options/alpha-clipping.md)]

