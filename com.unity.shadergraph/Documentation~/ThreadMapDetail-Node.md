# ThreadMapDetail Node

The ThreadMapDetail node adds tilable Thread map detail information to a fabric material. The node outputs a Thread map that you can apply to a fabric material.

A Thread map is a Texture with 4 channels. Like a Detail map, a Thread map contains information about ambient occlusion, the normal x-axis and normal y-axis, and smoothness.

For more information on Detail maps, see [Secondary Maps (Detail Maps) & Detail Mask](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterDetail.html) in the Unity User Manual.

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
<td>Use the port's default input to enable or disable the ThreadMapDetail node. You can also connect a node that outputs a Boolean to choose when to enable or disable the thread map detail.</td>
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
<td>The coordinates of the UVs the ThreadMapDetail node should use to map the Texture. </td>
</tr>
<tr>
<td>Normals</td>
<td>Input</td>
<td>Vector 3</td>
<td>None</td>
<td></td>
</tr>
<tr>
<td>Smoothness</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td></td>
</tr>
<tr>
<td>Alpha</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td></td>
</tr>
<tr>
<td>Ambient Occlusion</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td></td>
</tr>
<tr>
<td>Thread AO Strength</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td>(Ambient Occlusion)</td>
</tr>
<tr>
<td>Thread Normal Strength</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td></td>
</tr>
<tr>
<td>Thread Smoothness Strength</td>
<td>Input</td>
<td>Float</td>
<td>None</td>
<td></td>
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
<td>The final alpha output of the thread map. Shader Graph calculates this alpha value by multiplying the input <strong>Alpha</strong> value by the <strong>Thread AO Strength</strong>.</td>
</tr>
</tbody>
</table>


## Generated code example
