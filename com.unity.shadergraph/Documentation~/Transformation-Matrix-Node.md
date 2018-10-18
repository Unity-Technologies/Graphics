# Transformation Matrix Node

## Description

Defines a constant **Matrix 4x4** value for a common **Transformation Matrix** in the shader. The **Transformation Matrix** can be selected from the dropdown parameter.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Matrix 4 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|  | Dropdown | Model, InverseModel, View, InverseView, Projection, InverseProjection, ViewProjection, InverseViewProjection | Sets output value |

## Generated Code Example

The following example code represents one possible outcome of this node per mode.

**Model**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_M;
```

**InverseModel**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_M;
```

**View**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_V;
```

**InverseView**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_V;
```

**Projection**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_P;
```

**InverseProjection**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_P;
```

**ViewProjection**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_VP;
```

**InverseViewProjection**
```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_VP;
```