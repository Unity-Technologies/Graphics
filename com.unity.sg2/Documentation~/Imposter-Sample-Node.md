# Imposter Sample Node

## Description

The Imposter Sample Node utilizes the three virtual UVs to sample the imposter texture and blends them based on the camera intersection point on the octahedron sphere, thereby achieving accurate sampling results.
## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Texture | Input      |    Texture2D | The texture asset to sample |
| Sampler | Input      |    Sampler State | The texture sampler to use for sampling the texture |
| UV0 | Input      |    Vector4 | The virtual UV for the base frame |
| UV1 | Input      |    Vector4 | The virtual UV for the second frame |
| UV2 | Input      |    Vector4 | The virtual UV for the third frame |
| Grid | Input      |    Vector4 | The current UV grid, which is used to find the corresponding sample frames |
| Frames | Input      |    Float | The amount of the imposter frames |
| Clip | Input      |    Float | The amount of clipping for a single frame |
| Parallax | Input      |    Boolean | Adds parallax shif if the port value is true, only applicable when sampling a normal map |
| RGBA | Output      |    Vector3 | A vector4 from the sampled texture |

## Controls

The Imposter Sample Node [!include[nodes-controls](./snippets/nodes-controls.md)]

<table>
<thead>
<tr>
<th><strong>Name</strong></th>
<th><strong>Sample Type</strong></th>
<th colspan="2"><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td rowspan="3"><strong>Type</strong></td>
<td rowspan="3">Dropdown</td>
<td colspan="2">Select whether to sample three frames or one frame.</td>
</tr>
<tr>
<td><strong>Three Frames</strong></td>
<td>Blends between three frames to get the smoothest result.</td>
</tr>
<tr>
<td><strong>One Frame</strong></td>
<td>Calculates only one frame for better performance.</td>
</tr>
</tbody>
</table>

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)], based on the selected Mode on the Imposter Sample Node node:

### ThreeFrames

```
ImposterUV(inPos, inUV, imposterFrames, imposterOffset, imposterSize,isHemi, outPos, outUVGrid, outUV0, outUV1, outUV2);
```

### OneFrame

```
ImposterUV_oneFrames(inPos, inUV, imposterFrames, imposterOffset, imposterSize,isHemi, outPos, outUVGrid, outUV0, outUV1, outUV2);

```
