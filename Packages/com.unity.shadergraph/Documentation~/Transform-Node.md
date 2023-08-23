# Transform Node

## Description

Returns the result of transforming the input value (**In**) from one coordinate space to another. Select dropdown options on the node to define which spaces to transform from and to.

## Ports

| Name | Direction | Type     | Description  |
|:-----|:----------|:---------|:-------------|
| In   | Input     | Vector 3 | Input value  |
| Out  | Output    | Vector 3 | Output value |

## Controls

| Name | Type     | Options                                              | Description |
|:-----|:---------|:-----------------------------------------------------|:------------|
| From | Dropdown | Object, View, World, Tangent, Absolute World, Screen | Select the space to convert from.  |
| To   | Dropdown | Object, View, World, Tangent, Absolute World, Screen | Select the space to convert to.    |
| Type | Dropdown | Position, Direction, Normal                          | Select how you want to handle the conversion. |

## Node Settings Controls

The following control appears on the Node Settings tab of the Graph Inspector when you select the **Direction** or **Normal** conversion types for the Transform Node. The Normalize Output setting helps to improve performance as you can disable it if the output is already normalized, or if you don't need the output to remain normalized.

| Name             | Type     | Description |
|:-----------------|:---------|:------------|
| Normalize Output | Checkbox | Reduces the length of the output vector to 1. |

## World and Absolute World

Use the **World** and **Absolute World** space options to transform the coordinate space of [position](Position-Node.md) values. The **World** space option uses the Scriptable Render Pipeline default world space to convert position values. The **Absolute World** space option uses absolute world space to convert position values in all Scriptable Render Pipelines.

If you use the **Transform Node** to convert coordinate spaces that aren't for position values, Unity recommends that you use the **World** space option. Using **Absolute World** on values that don't represent position might result in unexpected behavior.

## Conversion type

Select the **Position** type to apply translation to the transformation. Select **Direction** if the input doesn't describe a surface normal (the direction a surface faces). Select **Normal** if the input describes a surface normal (the direction the surface faces). 


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
**World > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(In);
```

**World > Screen**

```
float4 hclipPosition = TransformWorldToHClipDir(In);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);
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
**Object > Screen**

```
float4 hclipPosition = TransformObjectToHClip(In);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);
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

**Tangent > Screen**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float4 hclipPosition = TransformWorldToHClipDir(mul(In, transposeTangent).xyz);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);
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

**View > Screen**

```
float4 hclipPosition = TransformWViewToHClip(In);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);
```

**Absolute World > World**

```
float3 _Transform_Out = GetCameraRelativePositionWS(In);
```

**Absolute World > Object**

```
float3 _Transform_Out = TransformWorldToObject(In);
```

**Absolute World > Object (in the High Definition Render Pipeline)**  

```
float3 _Transform_Out = TransformWorldToObject(GetCameraRelativePositionWS(In));
```

**Absolute World > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(In, tangentTransform_World);
```

**Absolute World > View**

```
float3 _Transform_Out = GetCameraRelativePositionWS(In)
```
**Absolute World > Absolute World**

```
float3 _Transform_Out = In;
```

**Absolute World > Screen**

```
float4 hclipPosition = TransformWorldToHClip(GetCameraRelativePositionWS(In));
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);
```
**Screen > World**

```
float3 _Transform_Out = ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP);
```

**Screen > Object**
```
float3 worldPos = ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP);
float3 _Transform_Out = TransformWorldToObject(worldPos);
```

**Screen > Tangent**
```
float3 worldPos = ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP);
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(worldPos, tangentTransform_World);
```

**Screen > View**
```
float4 positionCS  = ComputeClipSpacePosition(In.xy, In.z);
float4 result = mul(UNITY_MATRIX_I_V, positionCS);
float3 _Transform_Out = result.xyz / result.w;
```

**Screen > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP));
```
