# UVCombine node

The UVCombine node lets you select which UV channel you want to use for mapping your shader to an object in your application. You can also choose to apply tiling and offset to your UV coordinates.

![An image showing the UVCombine node in the Shader Graph window](images/sg-uv-combine-node.png)

> [!NOTE]
> The UVCombine node is a Subgraph node: it represents a Subgraph instead of directly representing shader code. Double-click the node in any Shader Graph to view the Subgraph.

## Create Node menu location

The UVCombine node is under the **Utility** &gt; **High Definition Render Pipeline** category in the Create Node menu.

## Compatibility

The UVCombine node is a High Definition Render Pipeline (HDRP) node. It's designed to work with Unity's HDRP package. You can use the node in your Shader Graphs if you have the HDRP installed in your project.

For more information on the HDRP, see [Unity's HDRP package documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest).

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
<td>UV Channel Mask</td>
<td>Input</td>
<td>Vector 4</td>
<td>None</td>
<td>Select which UV channel you want to use by entering a <code>1</code> in the corresponding default input on the port:
<ul>
<li>X: UV channel 0</li>
<li>Y: UV channel 1</li>
<li>Z: UV channel 2</li>
<li>W: UV channel 3</li>
</ul></td>
</tr>
<tr>
<td>UV Tile and Offset</td>
<td>Input</td>
<td>Vector 4</td>
<td>None</td>
<td>Use the port's default input to specify the amount of offset or tiling that you want to apply to your shader's UV coordinates. You can also connect a node that outputs a Vector 4. Use <code>X</code> and <code>Y</code> to specify the tiling, and <code>W</code> and <code>Z</code> to specify the offset.</td>
</tr>
<tr>
<td>UV</td>
<td>Output</td>
<td>Vector 2</td>
<td>UV</td>
<td>The final UV output, after selecting a channel and, if specified, any tiling or offset.</td>
</tr>
</tbody>
</table>

## Example shader usage

For an example of how to use the UVCombine node, see either of the HDRP's Fabric shaders. To view these Shader Graphs:

1. Create a new material and assign it the **HDRP** &gt; **Fabric** &gt; **Silk** or **HDRP** &gt; **Fabric** &gt; **CottonWool** shader, as described in the Unity User Manual section [Creating a material asset, and assigning a shader to it](https://docs.unity3d.com/Documentation/Manual/materials-introduction.html).

2. Next to the **Shader** dropdown, select **Edit**.

    Your chosen Fabric's Shader Graph opens.
