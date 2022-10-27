# Sample Texture 2D Array node

The Sample Texture 2D Array node samples a **Texture 2D Array** asset and returns a **Vector 4** color value. You can specify the **UV** coordinates for a texture sample and use a [Sampler State node](Sampler-State-Node.md) to define a specific Sampler State. The node's **Index** input port specifies which index of a Texture 2D Array to sample.

For more information about Texture 2D Arrays, see [Texture Arrays](https://docs.unity3d.com/Manual/class-Texture2DArray.html) in the Unity User manual.

[!include[nodes-sample-errors](./snippets/sample-nodes/nodes-sample-errors.md)]

![An image that displays the Graph window with a Sample Texture 2D Array node.](images/sg-sample-texture-2d-array-node.png)

## Create Node menu category

The Sample Texture 2D Array node is under the **Input** &gt; **Texture** category in the Create Node menu.

## Compatibility

The Sample Texture 3D [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]

[!include[nodes-sample-fragment-lod](./snippets/sample-nodes/nodes-sample-fragment-lod.md)]

## Inputs

The Sample Texture 3D [!include[nodes-inputs](./snippets/nodes-inputs.md)]

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
<td><strong>Texture Array</strong></td>
<td>Texture 2D Array</td>
<td>None</td>
<td>The Texture 2D Array asset to sample.</td>
</tr>
<tr>
<td><strong>Index</strong></td>
<td>Float</td>
<td>None</td>
<td>The index of the specific Texture in the Texture array to sample. The index value is the Texture's location in the Texture array. The index values in an array always start at 0. An array with four textures would have locations 0, 1, 2, and 3.</td>
</tr>
[!include[nodes-sample-uv-table](./snippets/sample-nodes/nodes-sample-uv-table.md)]
[!include[nodes-sample-ss-table](./snippets/sample-nodes/nodes-sample-ss-table.md)]
[!include[nodes-sample-lod-table](./snippets/sample-nodes/nodes-sample-lod-table.md)]
[!include[nodes-sample-mip-bias-table](./snippets/sample-nodes/nodes-sample-mip-bias-table.md)]
[!include[nodes-sample-ddx-table](./snippets/sample-nodes/nodes-sample-ddx-table.md)]
[!include[nodes-sample-ddy-table](./snippets/sample-nodes/nodes-sample-ddy-table.md)]
</tbody>
</table>

## Additional node settings

The Sample Texture 3D [!include[nodes-additional-settings](./snippets/nodes-additional-settings.md)]

[!include[nodes-sample-mip-bias-sample-mode-table](./snippets/sample-nodes/nodes-sample-mip-bias-sample-mode-table.md)]

## Outputs

The Sample Texture 3D [!include[nodes-outputs](./snippets/nodes-outputs.md)]

[!include[nodes-sample-rgba-output-table](./snippets/sample-nodes/nodes-sample-rgba-output-table.md)]

## Example graph usage

In the following example, the Sample Texture 2D Array node samples a Texture array that has 4 different cloth normal maps. Change the number given to the **Index** port as an input, and the Sample Texture 2D Array node can sample a specific normal map from the array. The **Index** value changes the output the node sends to the Normal Unpack node, and the Normal (Tangent Space) Block node in the Master Stack.

![An image of the Graph window, that displays a Sample Texture 2D Array node. The node has a Sampler State node attached as an input and sends its RGBA output to the Normal Unpack node. The Normal Unpack node's Out output port connects to the Normal (Tangent Space) Block node in the Master Stack. The Index is set to 2, which makes the sphere in the Main Preview window render with a leather-like Texture.](images/sg-sample-texture-2d-array-node-example.png)

![An image of the Graph window, that displays a Sample Texture 2D Array node. The node has a Sampler State node attached as an input and sends its RGBA output to the Normal Unpack node. The Normal Unpack node's Out output port connects to the Normal (Tangent Space) Block node in the Master Stack. The Index is set to 0, which makes the sphere in the Main Preview window render with a ridged fabric Texture.](images/sg-sample-texture-2d-array-node-example-2.png)


## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]:

```
float4 _SampleTexture2DArray_RGBA = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, UV, Index);
float _SampleTexture2DArray_R = _SampleTexture2DArray_RGBA.r;
float _SampleTexture2DArray_G = _SampleTexture2DArray_RGBA.g;
float _SampleTexture2DArray_B = _SampleTexture2DArray_RGBA.b;
float _SampleTexture2DArray_A = _SampleTexture2DArray_RGBA.a;
```

## Related nodes

[!include[nodes-related](./snippets/nodes-related.md)] Sample Texture 3D node:

- [Sample Texture 2D node](Sample-Texture-2D-Node.md)
- [Sample Texture 3D node](Sample-Texture-3D-Node.md)
- [Sampler State node](Sampler-State-Node.md)
