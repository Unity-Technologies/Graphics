---
uid: urp-master-stack-fullscreen
---
# Fullscreen Master Stack reference for Shader Graph in URP

Use the Fullscreen Master Stack to create a Shader Graph material to apply to the entire screen at the end of the rendering process. You can use this to create custom post-process and custom pass effects.

![A full-screen shader that applies a raindrop effect to the screen.](../Images/Fullscreen-shader-rain.png)

## Contexts

A shader graph contains the following contexts:

* [Vertex context](#vertex-context)
* [Fragment context](#fragment-context)

The Fullscreen Master Stack has its own [Graph Settings](fullscreen-master-stack-reference.md) that determine which blocks you can use in the Shader Graph contexts.

This section contains information on the blocks that this Master Stack material type uses by default, and which blocks you can use to affect the Graph Settings.

### <a name="vertex-context"></a>Vertex context

The Vertex context represents the vertex stage of this shader. Unity executes any block you connect to this context in the vertex function of this shader. For more information, refer to [Master Stack](https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Master-Stack.html).

Vertex blocks are not compatible with the Fullscreen Master Stack.

### <a name="fragment-context"></a>Fragment context

The Fragment context represents the fragment (or pixel) stage of this shader. Unity executes any block you connect to this context in the fragment function of this shader. For more information, refer to [Master Stack](https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Master-Stack.html).

### Default

When you create a new Fullscreen Master Stack, the Fragment context contains the following blocks by default.

<table>
<thead>
<tr>
<th><strong>Property</strong></th>
<th><strong>Description</strong></th>
<th><strong>Setting Dependency</strong></th>
<th><strong>Default Value</strong></th>
</tr>
</thead>
<tbody>

[!include[](../snippets/shader-graph-blocks/base-color.md)]
[!include[](../snippets/shader-graph-blocks/alpha.md)]

</tbody>
</table>

### Relevant

The following blocks are also compatible with the Fullscreen master stack.

<table>
<thead>
<tr>
<th><strong>Property</strong></th>
<th><strong>Description</strong></th>
<th><strong>Setting Dependency</strong></th>
<th><strong>Default Value</strong></th>
</tr>
</thead>
<tbody>

[!include[](../snippets/shader-graph-blocks/eye-depth.md)]
[!include[](../snippets/shader-graph-blocks/linear01-depth.md)]
[!include[](../snippets/shader-graph-blocks/raw-depth.md)]

</tbody>
</table>

## Fullscreen Master Stack reference

For more information about the properties available in the Fullscreen Master Stack, refer to the [Master Stack Fullscreen reference for URP](fullscreen-master-stack-reference.md).