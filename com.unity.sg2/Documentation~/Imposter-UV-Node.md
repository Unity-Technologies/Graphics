# Imposter UV Node

## Description

The Imposter UV Node calculates the billboard position and the UV coordinates needed by the Imposter Sample node.
## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In Position | Input      |    Vector3 | The postion in Object space |
| UV | Input      |    Vector4 | The UV coordinates of the mesh |
| Frames | Input      |    Float | The number of the imposter frames in each axis|
| Offset | Input      |    Float | The offset value from the pivot |
| Size | Input      |    Float | The size of the imposter |
| HemiSphere | Input      |    Boolean | If it's true, calculates the imposter grid and UVs base on hemisphere type.ase on hemisphere type. Useful if the object is only seen from above |
| Out Positon | Output      |    Vector3 | The output billboard position |
| UV0 | Output      |    Vector4 | The virtual UV for the base frame |
| UV1 | Output      |    Vector4 | The virtual UV for the second frame |
| UV2 | Output      |    Vector4 | The virtual UV for the third frame |
| Grid | Output      |    Vector4 | The current UV grid, which is used to find the corresponding sample frames |


## Controls

The Imposter UV Node [!include[nodes-controls](./snippets/nodes-controls.md)]

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
ImposterUV(Pos, inUV, Frames, Offset, Size, HemiSphere, OutPos, Grid, UV0, UV1, UV2)```

### OneFrame

```
ImposterUV_oneFrame(Pos, inUV, Frames, Offset, Size, HemiSphere, OutPos, Grid, UV0);
```
