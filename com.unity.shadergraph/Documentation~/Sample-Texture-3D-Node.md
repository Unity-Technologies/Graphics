# Sample Texture 3D Node

The Sample Texture 3D node samples a **Texture 3D** asset and returns a **Vector 4** color value. You can specify the **UV** coordinates for your texture sample and use a [Sampler State Node](Sampler-State-Node.md) to define a specific Sampler State.

For more information about Texture 3D assets, see [3D textures](https://docs.unity3d.com/Manual/class-Texture3D.html) in the Unity User manual.

[!include[nodes-sample-errors](./snippets/sample-nodes/nodes-sample-errors.md)]

![An image showing the Graph window with a Sample Texture 3D node.](images/sg-sample-texture-3d-node.png)

## Create Node menu category

The Sample Texture 2D node is under the **Input** &gt; **Texture** category in the Create Node menu.

## Compatibility

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]
    [!include[nodes-fragment-only](./snippets/nodes-fragment-only.md)]
</ul>

If you need to sample a texture for use in the Vertex Context of your Shader Graph, set the [Mip Sampling Mode](#additional-node-settings) to **LOD**.

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
<td><strong>Texture</strong></td>
<td>Texture 3D</td>
<td>None</td>
<td>The 3D texture asset that the node should sample.</td>
</tr>
<tr>
<td><strong>UV</strong></td>
<td>Vector 3</td>
<td>None</td>
<td>The three-dimensional UV coordinates that the node should use to sample the texture.</td>
</tr>
[!include[nodes-sample-ss-table](./snippets/sample-nodes/nodes-sample-ss-table.md)]
[!include[nodes-sample-lod-table](./snippets/sample-nodes/nodes-sample-lod-table.md)]
</tbody>
</table>


## Additional node settings

[!include[nodes-additional-settings](./snippets/nodes-additional-settings.md)]

<table>
<thead>
<tr>
<th><strong>Name</strong></th>
<th><strong>Type</strong></th>
<th><strong>Options</strong></th>
<th><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Mip Sampling Mode</strong></td>
<td>Dropdown</td>
<td>Standard, LOD</td>
<td>Choose the sampling mode that the Sample Texture 3D node should use for calculating the mip level of the texture:
<br/>
<ul>
<li><strong>Standard</strong>: The mip is calculated and selected automatically for the texture.</li>
<li><strong>LOD</strong>: Set an explicit mip for the texture. The texture will always use this mip, regardless of the DDX or DDY calculations between pixels. Setting the Mip Sampling Mode to <strong>LOD</strong> also allows you to connect the node to a Block node in the Vertex Context. For more information on Block nodes and Contexts, see <a href="Master-Stack.md">Master Stack</a>.</li>
</ul>
</td>
</tr>
</tbody>
</table>

## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)]

[!include[nodes-sample-rgba-output-table](./snippets/sample-nodes/nodes-sample-rgba-output-table.md)]

## Example graph usage

In the following example, the Sample Texture 3D node samples a 3D fractal noise texture asset. It takes its input UV coordinates from a Position node, set to **Object** Space.

The Sample Texture 3D node needs a Vector 3 for its UV coordinate input, rather than a Vector 2, because the texture asset exists as a volume in imaginary 3D space. The node uses the default Sampler State, since there is no Sampler State node connected.

Since this specific Texture 3D asset stores its texture data in the Alpha channel, the Sample Texture 3D node uses its A output port as an input for the Base Color Block node in the Fragment Context of the Master Stack:

![An image of the Graph window, showing a Position node connected to the UV input port on a Sample Texture 3D node. The Sample Texture 3D node's A output port is connected to the Base Color Block node in the Fragment Context of the Master Stack.](images/sg-sample-texture-3d-node-example.png)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float4 _SampleTexture3D_Out = SAMPLE_TEXTURE3D(Texture, Sampler, UV);
```

## Related nodes

[!include[nodes-related](./snippets/nodes-related.md)]

- [Sample Texture 2D Array node](Sample-Texture-2D-Array-Node.md)
- [Sample Texture 2D node](Sample-Texture-2D-Node.md)
- [Sampler State node](Sampler-State-Node.md)
