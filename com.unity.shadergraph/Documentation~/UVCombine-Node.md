# UVCombine node

The UVCombine node lets you select which UV channel you want to use for mapping your shader to geometry in your application. You can also choose to apply tiling and offset to your UV coordinates.

![An image showing the UVCombine node in the Shader Graph window](images/sg-uvcombine-node.png)

[!include[nodes-subgraph-node](./snippets/nodes-subgraph-node.md)]

## Create Node menu category

The UVCombine node is under the **Utility** &gt; **High Definition Render Pipeline** category in the Create Node menu.

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
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>UV Channel Mask</strong></td>
<td>Vector 4</td>
<td>Select which UV channel you want to use for your UV coordinates by entering a <code>1</code> in the corresponding default input on the port:
<ul>
<li><strong>X</strong>: UV channel 0</li>
<li><strong>Y</strong>: UV channel 1</li>
<li><strong>Z</strong>: UV channel 2</li>
<li><strong>W</strong>: UV channel 3</li>
</ul>
Set all other default inputs to <code>0</code>. You can also connect a node that outputs a Vector 4.</td>
</tr>
<tr>
<td><strong>UV Tile and Offset</strong></td>
<td>Vector 4</td>
<td>Use the port's default input to specify the amount of offset or tiling that you want to apply to your shader's UV coordinates:
<ul>
<li>Use <strong>X</strong> and <strong>Y</strong> to specify the tiling.</li>
<li>Use <strong>W</strong> and <strong>Z</strong> to specify the offset.</li>
</ul>
You can also connect a node that outputs a Vector 4.</td>
</tr>
</tbody>
</table>


## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

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
<td><strong>UV</strong></td>
<td>Vector 2</td>
<td>UV</td>
<td>The final UV output, after selecting a UV channel and, if specified, any tiling or offset.</td>
</tr>
</tbody>
</table>

## Example graph usage

For an example use of the UVCombine node, see either of the HDRP's Fabric shaders.

To view these Shader Graphs:

1. Create a new material and assign it the **HDRP** &gt; **Fabric** &gt; **Silk** or **HDRP** &gt; **Fabric** &gt; **CottonWool** shader, as described in the Unity User Manual section [Creating a material asset, and assigning a shader to it](https://docs.unity3d.com/Documentation/Manual/materials-introduction.html).

2. Next to the **Shader** dropdown, select **Edit**.

Your chosen Fabric's Shader Graph opens. You can view the UVCombine node, its Subgraph, and the other nodes that create HDRP's Fabric shaders.
