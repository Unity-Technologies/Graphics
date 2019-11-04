# Transform Node

## Description

Returns the result of transforming the input value (**In**) from one coordinate space to another. Select drop-down options on the node to define which spaces to transform from and to.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Vector 3 | Input value |
| Out | Output      |   Vector 3 | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| From      | Dropdown | Object, View, World, Tangent, Absolute World | Selects the space to convert from |
| To      | Dropdown | Object, View, World, Tangent, Absolute World | Selects the space to convert to |

## World and Absolute World
Use the **World** and **Absolute World** space options to transform the coordinate space of [position](Position-Node.md) values. The **World** space option uses the Scriptable Render Pipeline default world space to convert position values. The **Absolute World** space option uses absolute world space to convert position values in all Scriptable Render Pipelines.

If you use the **Transform Node** to convert coordinate spaces that are not for position values, Unity recommends that you use the **World** space option. Using **Absolute World** on values that do not represent position might result in unexpected behavior.

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
float3 _Transform_Out = TransformWorldToView(In)
```
**World > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(In);
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
**Object > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(TransformObjectToWorld(In));
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
**Tangent > Absolute World**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = GetAbsolutePositionWS(mul(In, transposeTangent)).xyz;
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
**View > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(mul(UNITY_MATRIX_I_V, float4(In, 1))).xyz;
```
**Absolute World > World**

```
float3 _Transform_Out = GetCameraRelativePositionWS(In);
```

**Absolute World > Object**

```
float3 _Transform_Out = TransformWorldToObject(In);
```

**Absolute World > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(In, tangentTransform_World);
```

**Absolute World > View**

```
float3 _Transform_Out = TransformWorldToView(In)
```
**Absolute World > Absolute World**

```
float3 _Transform_Out = In;
```