# Clamp Node

## Description

The Billboard node rotates the vertex position, normal, and tangent to align with all axes or x and z axes of the camera.
## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Position | Input      |    Vector3 | The input vertex postion |
| Normal | Input      |    Vector3 | The input vertex Normal |
| Tangent | Input      |    Vector3 | The input vertex Tangent |
| Billboard Position | Output      |    Vector3 | The billboard vertex position |
| Billboard Normal | Output      |    Vector3 | The billboard vertex normal |
| Billboard Tangent | Output      |    Vector3 | The billboard vertex tangent |
## Controls

The Billboard [!include[nodes-controls](./snippets/nodes-controls.md)]

<table>
<thead>
<tr>
<th><strong>Name</strong></th>
<th><strong>Mode</strong></th>
<th colspan="2"><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td rowspan="3"><strong>Type</strong></td>
<td rowspan="3">Dropdown</td>
<td colspan="2">Select whether to align all three axes or only x and z axes.</td>
</tr>
<tr>
<td><strong>Spherical</strong></td>
<td>Aglins all three axes to the camera.</td>
</tr>
<tr>
<td><strong>Cylindrical</strong></td>
<td>Aligns x and z axes to camera.</td>
</tr>
</tbody>
</table>

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)], based on the selected Mode on the Billboard node:

### Spherical

```
Scale = mul(float3( 1, 1, 1 ), UNITY_MATRIX_M);
rotationMatrix = UNITY_MATRIX_I_V;
Scaled_Pos= float4( Position * Scale, 0);
BillboardPosition = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + float3(0, .5, 0) + mul(rotationMatrix, Scaled_Pos).xyz);
BillboardNormal = TransformWorldToObject(mul(rotationMatrix , float4(Normal, 0)).xyz);
BillboardTangent = TransformWorldToObject(mul(rotationMatrix , float4(Tangent, 0)).xyz);
```

### Cylindrical

```
Scale = mul(float3( 1, 1, 1 ), UNITY_MATRIX_M);
rotationMatrix = UNITY_MATRIX_I_V;
rotationMatrix[1] = float4(0, 1, 0, 0);
Scaled_Pos= float4( Position * Scale, 0);
BillboardPosition = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + float3(0, .5, 0) + mul(rotationMatrix, Scaled_Pos).xyz);
BillboardNormal = TransformWorldToObject(mul(rotationMatrix , float4(Normal, 0)).xyz);
BillboardTangent = TransformWorldToObject(mul(rotationMatrix , float4(Tangent, 0)).xyz);
```
