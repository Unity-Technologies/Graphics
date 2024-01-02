# Vertex Context

This Context represents the vertex stage of a shader. Any Node you connect to a Block in this Context becomes part of the final shader's vertex function.

## Compatible Blocks

This section lists the Blocks that are compatible with Vertex Contexts in the High Definition Render Pipeline (HDRP). Each entry includes:

- The Block's name.
- A description of what the Block does.
- Settings in the [Graph Settings menu](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Graph-Settings-Menu.html) that the Block is relevant to. If you enable these settings, Shader Graph adds the Block to the Context; if you disable the setting, Shader Graph removes the Block from the Context. If you add the Block and do not enable the setting, Shader Graph ignores the Block and its connected Nodes when it builds the final shader.
- The default value that Shader Graph uses if you enable the Block's **Setting Dependency** then remove the Block from the Context.

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
