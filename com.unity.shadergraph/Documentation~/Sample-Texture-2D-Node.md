# Sample Texture 2D node

The Sample Texture 2D node samples a **Texture 2D** asset and returns a **Vector 4** color value. You can specify the **UV** coordinates for your Texture sample and use a [Sampler State node](Sampler-State-Node.md) to define a specific Sampler State.

You can also use the Sample Texture 2D node to sample a normal map. For more information, see the [Controls](#controls) section, or [Normal map (Bump mapping)](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html) in the Unity User manual.

[!include[nodes-sample-errors](./snippets/sample-nodes/nodes-sample-errors.md)]

![An image that displays the Graph window with a Sample Texture 2D node.](images/sg-sample-texture-2d-node.png)

## Create Node menu category

The Sample Texture 2D node is under the **Input** &gt; **Texture** category in the Create Node menu.

## Compatibility

[!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]

[!include[nodes-sample-fragment-lod](./snippets/sample-nodes/nodes-sample-fragment-lod.md)]

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
<td>Texture 2D</td>
<td>None</td>
<td>The Texture 2D asset to sample.</td>
</tr>
[!include[nodes-sample-uv-table](./snippets/sample-nodes/nodes-sample-uv-table.md)]
[!include[nodes-sample-ss-table](./snippets/sample-nodes/nodes-sample-ss-table.md)]
[!include[nodes-sample-lod-table](./snippets/sample-nodes/nodes-sample-lod-table.md)]
[!include[nodes-sample-mip-bias-table](./snippets/sample-nodes/nodes-sample-mip-bias-table.md)]
[!include[nodes-sample-ddx-table](./snippets/sample-nodes/nodes-sample-ddx-table.md)]
[!include[nodes-sample-ddy-table](./snippets/sample-nodes/nodes-sample-ddy-table.md)]
</tbody>
</table>

## Controls

[!include[nodes-controls](./snippets/nodes-controls.md)]

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
<td><strong>Type</strong></td>
<td>Dropdown</td>
<td>Default, Normal</td>
<td>Select whether the Texture is a Texture asset or a normal map. </td>
</tr>
<tr>
<td><strong>Space</strong></td>
<td>Dropdown</td>
<td>Tangent, Object</td>
<td>If <strong>Type</strong> is <strong>Normal</strong> and you're using your Texture as a normal map, choose the Space for your normal map: <br/><br/>
<ul>
<li><strong>Tangent</strong>: Use a Tangent normal map whenever the mesh for your geometry needs to deform or change, like when you're animating a character. When you select <strong>Tangent</strong> Space, the normal map's normals are relative to the existing vertex normals of any geometry rendered with your Shader Graph. Your Shader Graph only adjusts the vertex normals instead of overriding them. </li>
<li><strong>Object</strong>: Use an Object normal map whenever the mesh for your geometry is static and doesn't need to deform. When you select <strong>Object</strong> Space, the normal map's normals are explicit and override the normals of any geometry rendered with your Shader Graph. Because a static mesh's normals never change, an <strong>Object</strong> normal map also maintains consistent lighting across different levels of detail (LODs). </li>
</ul> <br/> For more information about normal maps, see <a href="https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html">Normal map (Bump mapping)</a> in the Unity User manual.</td>
</tr>
</tbody>
</table>

## Additional node settings

[!include[nodes-additional-settings](./snippets/nodes-additional-settings.md)]

[!include[nodes-sample-mip-bias-sample-mode-table](./snippets/sample-nodes/nodes-sample-mip-bias-sample-mode-table.md)]

## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)]

[!include[nodes-sample-rgba-output-table](./snippets/sample-nodes/nodes-sample-rgba-output-table.md)]

## Example graph usage

In the following example, the Sample Texture 2D node uses a [Subgraph node](Sub-graph-Node.md) that generates UV coordinates in latitude and longitude format to render a Texture formatted using latitude and longitude coordinates. If the Sample Texture 2D node uses the **Standard** Mip Sampling Mode, the Texture displays with a seam running down the side of the sphere where the left and right sides of the Texture meet.

The UV coordinates for sampling the Texture jump from `0` to `1` at this point on the model, which causes a problem with the mip level calculation in the sample. The error in the mip level calculation causes the seam. The Texture requires a different mip sampling mode to remove the seam:

![An image of the Graph window, that displays a UV Lat Long Subgraph node connected to the UV input port on a Sample Texture 2D node. The Sample Texture 2D is providing its RGBA output to the Base Color Block node in the Master Stack. The Main Preview of the sampled Texture has a noticeable seam running down the middle of the sphere.](images/sg-sample-texture-2d-node-example.png)

By setting the Mip Sampling Mode to **Gradient**, the Sample Texture 2D node can use the standard set of UVs for the model in the mip level calculation, instead of the latitude and longitude UVs needed for sampling the Texture. The new UV coordinates passed into the **DDX** and **DDY** input ports result in a continuous mip level, and remove the seam:

![An image of the Graph window, that displays the same Sample Texture 2D node as the previous image. This time, the Mip Sampling Mode in the Graph Inspector has been set to Gradient. The new DDX and DDY input ports on the Sample Texture 2D node receive input from a DDX and DDY node, taking input from a UV node. The seam on the Main Preview of the Texture has disappeared.](images/sg-sample-texture-2d-node-example-2.png)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)], depending on the selected [**Type**](#controls) on the node:

### Default

```
float4 _SampleTexture2D_RGBA = SAMPLE_TEXTURE2D(Texture, Sampler, UV);
float _SampleTexture2D_R = _SampleTexture2D_RGBA.r;
float _SampleTexture2D_G = _SampleTexture2D_RGBA.g;
float _SampleTexture2D_B = _SampleTexture2D_RGBA.b;
float _SampleTexture2D_A = _SampleTexture2D_RGBA.a;
```

### Normal

```
float4 _SampleTexture2D_RGBA = SAMPLE_TEXTURE2D(Texture, Sampler, UV);
_SampleTexture2D_RGBA.rgb = UnpackNormalRGorAG(_SampleTexture2D_RGBA);
float _SampleTexture2D_R = _SampleTexture2D_RGBA.r;
float _SampleTexture2D_G = _SampleTexture2D_RGBA.g;
float _SampleTexture2D_B = _SampleTexture2D_RGBA.b;
float _SampleTexture2D_A = _SampleTexture2D_RGBA.a;
```

## Related nodes

[!include[nodes-related](./snippets/nodes-related.md)]


- [Sample Texture 2D Array node](Sample-Texture-2D-Array-Node.md)
- [Sample Texture 3D node](Sample-Texture-3D-Node.md)
- [Sampler State node](Sampler-State-Node.md)
