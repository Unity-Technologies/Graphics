# Transform Node

## Description

Returns the result of transforming the value of input **In** from one coordinate space to another. The spaces to transform from and to are defined the values of the dropdowns on the node.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Vector 3 | Input value |
| Out | Output      |   Vector 3 | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| From      | Dropdown | Object, View, World, Tangent | Selects the space to convert from |
| To      | Dropdown | Object, View, World, Tangent | Selects the space to convert to |

## Generated Code Example

The following example code represents one possible outcome of this node per **Base** mode.

**World > World**

```
float3 _Transform_Out = In;
```

**World > Object**

```
float3 _Transform_Out = TransformWorldToObject(In);
```

**World > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(In, tangentTransform_World);
```

**World > View**

```
float3 _Transform_Out = TransformWorldToView(In);
```

**Object > World**

```
float3 _Transform_Out = TransformObjectToWorld(In);
```

**Object > Object**

```
float3 _Transform_Out = In;
```

**Object > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(TransformObjectToWorld(In), tangentTransform_World);
```

**Object > View**

```
float3 _Transform_Out = TransformWorldToView(TransformObjectToWorld(In));
```

**Tangent > World**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = mul(In, transposeTangent).xyz;
```

**Tangent > Object**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = TransformWorldToObject(mul(In, transposeTangent).xyz);
```

**Tangent > Tangent**

```
float3 _Transform_Out = In;
```

**Tangent > View**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = TransformWorldToView(mul(In, transposeTangent).xyz);
```

**View > World**

```
float3 _Transform_Out = mul(UNITY_MATRIX_I_V, float4(In, 1)).xyz;
```

**View > Object**

```
float3 _Transform_Out = TransformWorldToObject(mul(UNITY_MATRIX_I_V, float4(In, 1) ).xyz);
```

**View > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(mul(UNITY_MATRIX_I_V, float4(In, 1) ).xyz, tangentTransform_World);
```

**View > View**

```
float3 _Transform_Out = In;
```