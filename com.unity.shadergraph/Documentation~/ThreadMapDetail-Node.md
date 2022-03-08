# ThreadMapDetail node

The ThreadMapDetail node adds tileable thread map detail information to a fabric material. The node outputs a thread map that you can apply to a fabric material.

![An image showing the ThreadMapDetail node in the Shader Graph window](images/sg-threadmapdetail-node.png)

[!include[nodes-subgraph-node](./snippets/nodes-subgraph-node.md)]

A thread map is a Texture with 4 channels. Like a detail map, a thread map contains information about ambient occlusion, the normal x-axis and normal y-axis, and smoothness.

For more information on Detail maps, see [Secondary Maps (Detail Maps) & Detail Mask](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterDetail.html) in the Unity User Manual.

## Create Node menu category

The ThreadMapDetail node is under the **Utility** &gt; **High Definition Render Pipeline** &gt; **Fabric** category in the Create Node menu.

## Compatibility

[!include[nodes-compatibility-hdrp](./snippets/nodes-compatibility-hdrp.md)]

[!include[hdrp-latest-link](./snippets/hdrp-latest-link.md)]

[!include[nodes-all-contexts](./snippets/nodes-all-contexts.md)]

## Inputs

[!include[nodes-inputs](./snippets/nodes-inputs.md)]

<table>
    <thead>
        <tr>
            <th><strong>Name</strong></th>
            <th><strong>Type</strong></th>
            <th><strong>Binding</strong></th>
            <th><strong>Description</strong></th>
         </tr>
    </thead>
    <tbody>
        <tr>
            <td><strong>Use Thread Map</strong></td>
            <td>Boolean</td>
            <td>None</td>
            <td>Use the port's default input to enable or disable the ThreadMapDetail node. You can also connect a node that outputs a Boolean to choose when to enable or disable the thread map.</td>
        </tr>
        <tr>
            <td><strong>ThreadMap</strong></td>
            <td>Texture 2D</td>
            <td>None</td>
            <td>The texture that contains the detailed information of a fabric's thread pattern. The texture should contain 4 channels:
                <ul>
                    <li>R - The ambient occlusion</li>
                    <li>G - The normal Y-axis</li>
                    <li>B - The smoothness</li>
                    <li>A - The normal X-axis</li>
                </ul>
            </td>
        </tr>
        <tr>
            <td><strong>UV</strong></td>
            <td>Vector 2</td>
            <td>UV</td>
            <td>The UV coordinates the ThreadMapDetail node should use to map the ThreadMap texture on the geometry.</td>
        </tr>
        <tr>
            <td><strong>Normals</strong></td>
            <td>Vector 3</td>
            <td>None</td>
            <td>The base normal map that you want your Shader Graph to apply to the geometry before it applies the thread map.</td>
    </tr>
    <tr>
        <td><strong>Smoothness</strong></td>
        <td>Float</td>
        <td>None</td>
        <td>The base smoothness value that you want your Shader Graph to apply to the geometry before it applies the thread map.</td>
    </tr>
    <tr>
<td><strong>Alpha</strong></td>
<td>Float</td>
<td>None</td>
<td>The base alpha value that you want your Shader Graph to apply to the geometry before it applies the thread map.</td>
</tr>
<tr>
<td><strong>Ambient Occlusion</strong></td>
<td>Float</td>
<td>None</td>
<td>The base ambient occlusion value that you want your Shader Graph to apply to the geometry before it applies the thread map.</td>
</tr>
<tr>
<td><strong>Thread AO Strength</strong></td>
<td>Float</td>
<td>None</td>
<td>Specify a value of <code>0</code> or <code>1</code> to determine how the <strong>ThreadMap</strong>'s ambient occlusion should impact the final shader result:
<ul>
<li>If you provide a value of <code>0</code>, the <strong>ThreadMap</strong>'s ambient occlusion has no effect on the final output of the shader.</li>
<li>If you provide a value of <code>1</code>, Shader Graph multiplies your base <strong>Ambient Occlusion</strong> value by the ambient occlusion value specified in your <strong>ThreadMap</strong> to determine the final output of the shader.</li></ul></td>
</tr>
<tr>
<td><strong>Thread Normal Strength</strong></td>
<td>Float</td>
<td>None</td>
<td>Specify a value of <code>0</code> or <code>1</code> to determine how the <strong>ThreadMap</strong>'s normal should impact the final shader result:
<ul>
<li>If you provide a value of <code>0</code>, the <strong>ThreadMap</strong>'s normal has no effect on the final output of the shader.</li>
<li>If you provide a value of <code>1</code>, Shader Graph blends the values from your base <strong>Normals</strong> with the normal specified in your <strong>ThreadMap</strong> to determine the final output of the shader.</li></ul></td>
</tr>
<tr>
<td><strong>Thread Smoothness Strength</strong></td>
<td>Float</td>
<td>None</td>
<td>Specify a value of <code>0</code> or <code>1</code> to determine how the <strong>ThreadMap</strong>'s smoothness should impact the final shader result:
<ul>
<li>If you provide a value of <code>0</code>, the <strong>ThreadMap</strong>'s smoothness value has no effect on the final output of the shader.</li>
<li>If you provide a value of <code>1</code>, Shader Graph adds the smoothness value specified in your <strong>ThreadMap</strong> to your base <strong>Smoothness</strong> value to determine the final output of the shader. For this calculation, Shader Graph remaps the value of your <strong>ThreadMap</strong>'s smoothness from (0,1) to (-1, 1).</li></ul></td>
</tr>
</tbody>
</table>

## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)]

<table>
<thead>
<tr>
<th><strong>Name</strong></th>
<th><strong>Type</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Normal</strong></td>
<td>Vector 3</td>
<td>The final normal output of the thread map.</td>
</tr>
<tr>
<td><strong>Smoothness</strong></td>
<td>Float</td>
<td>The final smoothness output of the thread map.</td>
</tr>
<tr>
<td><strong>Ambient Occlusion</strong></td>
<td>Float</td>
<td>The final ambient occlusion output of the thread map.</td>
</tr>
<tr>
<td><strong>Alpha</strong></td>
<td>Float</td>
<td>The final alpha output of the thread map. Shader Graph calculates this alpha value by multiplying the input <strong>Alpha</strong> value by the <strong>Thread AO Strength</strong> value.</td>
</tr>
</tbody>
</table>


## Example graph usage

For an example use of the ThreadMapDetail node, see either of the HDRP's Fabric shaders.

To view these Shader Graphs:

1. Create a new material and assign it the **HDRP** &gt; **Fabric** &gt; **Silk** or **HDRP** &gt; **Fabric** &gt; **CottonWool** shader, as described in the Unity User Manual section [Creating a material asset, and assigning a shader to it](https://docs.unity3d.com/Documentation/Manual/materials-introduction.html).

2. Next to the **Shader** dropdown, select **Edit**.

Your chosen Fabric's Shader Graph opens. You can view the ThreadMapDetail node, its Subgraph, and the other nodes that create HDRP's Fabric shaders.
