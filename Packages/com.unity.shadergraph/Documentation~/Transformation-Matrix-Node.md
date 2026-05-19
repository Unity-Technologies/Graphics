# Transformation Matrix Node

## Description

Defines a constant **Matrix 4x4** value for a common **Transformation Matrix** in the shader. The **Transformation Matrix** can be selected from the dropdown parameter.

Two output value options for this node, **Inverse Projection** and **Inverse View Projection**, are not compatible with the Built-In Render Pipeline target. When you choose either of these options and target the Built-In Render Pipeline, this node produces an entirely black result.

[!include[birp-deprecation-message](snippets/birp-deprecation-message.md)]

## Ports

| Name | Direction | Type | Binding | Description |
|:--- |:---|:---|:---|:---|
| Out | Output | Matrix 4 | None | Output value |

## Controls

| Control | Description |
|:--- |:---|
| (Dropdown) | Sets the output value. The options are: <ul><li>**Model**</li><li>**InverseModel**</li><li>**View**</li><li>**InverseView**</li><li>**Projection**</li><li>**InverseProjection**</li><li>**ViewProjection**</li><li>**InverseViewProjection**</li></ul> |

## Transformations

All these matrices are `float4x4` type, and are column major.

| **Name**                  | **Value** |
|:---                       |:---       |
| Model                 | Current model matrix. Transforms from Object to World space.                       |
| InverseModel          | Inverse of the current model matrix. Transforms from World to Object space.        |
| View                  | Current view matrix. Transforms from World to View space.                          |
| InverseView           | Inverse of the current view matrix. Transforms from View to World space.           |
| Projection            | Current projection matrix. Transforms from View to Clip space.                     |
| InverseProjection     | Inverse of the current projection matrix. Transforms from Clip to View space.      |
| ViewProjection        | Current view \* projection matrix. Transforms from World to Clip space.            |
| InverseViewProjection | Inverse of current view \* projection matrix. Transforms from Clip to World space. |

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
