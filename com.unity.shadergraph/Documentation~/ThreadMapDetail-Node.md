# ThreadMapDetail Node

The ThreadMapDetail node adds tilable thread map detail information to a fabric material. The node outputs a thread map that you can apply to a fabric material.

A thread map is a Texture with 4 channels. Like a detail map, a thread map contains information about ambient occlusion, the normal x-axis and normal y-axis, and smoothness.

For more information on Detail maps, see [Secondary Maps (Detail Maps) & Detail Mask](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterDetail.html) in the Unity User Manual.

> [!NOTE]
> The ThreadMapDetail node is a Subgraph node: it represents a Subgraph instead of directly representing shader code. Double-click the node in any Shader Graph to view the Subgraph.

## Ports

<table>
<thead>
<tr>
<th><strong>Name</strong></th>
<th><strong>Direction</strong></th>
<th><strong>Type</strong></th>
<th><strong>Binding</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td>Use Thread Map</td>
<td>Input</td>
<td>Boolean</td>
<td>None</td>
<td>Use the port's default input to enable or disable the ThreadMapDetail node. You can also connect a node that outputs a Boolean to choose when to enable or disable the thread map.</td>
</tr>
<tr>
<td>ThreadMap</td>
<td>Input</td>
<td>Texture 2D</td>
<td>None</td>
<td>The Texture that contains the detailed information of a fabric's thread pattern. The texture should contain 4 channels:
<ul><li>R - The ambient occlusion</li>
<li>G - The normal Y-axis</li>
<li>B - The smoothness</li>
<li>A - The normal X-axis</li></ul></td>
</tr>
<tr>
<td>UV</td>
<td>Input</td>
<td>Vector 2</td>
<td>UV</td>
<td>The UV coordinates the ThreadMapDetail node should use to map the Texture on the object.</td>
</tr>
<tr>
<td>Normals</td>
<td>Input</td>
<td>Vector 3</td>
<td>None</td>
<td>The base normal map that you want your shader to apply to an object before it applies the thread map.</td>
</tr>
<tr>
<td>Smoothness</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td>The base smoothness value that you want your shader to apply to an object before it applies the thread map.</td>
</tr>
<tr>
<td>Alpha</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td>The base alpha value that you want your shader to apply to an object before it applies the thread map.</td>
</tr>
<tr>
<td>Ambient Occlusion</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td>The base ambient occlusion value that you want your shader to apply to an object before it applies the thread map.</td>
</tr>
<tr>
<td>Thread AO Strength</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td>Specify a value of <code>0</code> or <code>1</code> to determine how the thread map's ambient occlusion should impact the final shader result:
<ul>
<li>If you provide a value of <code>0</code>, the thread map's ambient occlusion has no effect on the final output of the shader.</li>
<li>If you provide a value of <code>1</code>, Shader Graph multiplies your base ambient occlusion value by the ambient occlusion value specified in your thread map to determine the final output of the shader.</li></ul></td>
</tr>
<tr>
<td>Thread Normal Strength</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td>Specify a value of <code>0</code> or <code>1</code> to determine how the thread map's normal should impact the final shader result:
<ul>
<li>If you provide a value of <code>0</code>, the thread map's normal has no effect on the final output of the shader.</li>
<li>If you provide a value of <code>1</code>, Shader Graph blends your base normal map with the normal specified in your thread map to determine the final output of the shader.</li></ul></td>
</tr>
<tr>
<td>Thread Smoothness Strength</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td>Specify a value of <code>0</code> or <code>1</code> to determine how the thread map's smoothness should impact the final shader result:
<ul>
<li>If you provide a value of <code>0</code>, the thread map's smoothness value has no effect on the final output of the shader.</li>
<li>If you provide a value of <code>1</code>, Shader Graph adds the smoothness value specified in your thread map to your base smoothness value to determine the final output of the shader. For this calculation, Shader Graph remaps the value of your thread map's smoothness from (0,1) to (-1, 1).</li></ul></td>
</tr>
<tr>
<td>Normal</td>
<td>Output</td>
<td>Vector 3</td>
<td>None</td>
<td>The final normal output of the thread map.</td>
</tr>
<tr>
<td>Smoothness</td>
<td>Output</td>
<td>Float</td>
<td>None</td>
<td>The final smoothness output of the thread map.</td>
</tr>
<tr>
<td>Ambient Occlusion</td>
<td>Output</td>
<td>Float</td>
<td>None</td>
<td>The final ambient occlusion output of the thread map.</td>
</tr>
<tr>
<td>Alpha</td>
<td>Output</td>
<td>Float</td>
<td>None</td>
<td>The final alpha output of the thread map. Shader Graph calculates this alpha value by multiplying the input <strong>Alpha</strong> value by the <strong>Thread AO Strength</strong> value.</td>
</tr>
</tbody>
</table>
