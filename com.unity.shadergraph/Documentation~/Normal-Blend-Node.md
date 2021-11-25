# Normal Blend Node

## Description

Blends two normal maps defined by inputs **A** and **B** together, normalizing the result to create a valid normal map.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| A      | Input | Vector 3 | None | First input value |
| B      | Input | Vector 3 | None | Second input value |
| Out | Output      |    Vector 3 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | Default, Reoriented | Selects the the method used for blending. |

## Generated Code Example

The following example code represents one possible outcome of this node per **Mode**.

**Default**

```
void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = normalize(float3(A.rg + B.rg, A.b * B.b));
}
```

**Reoriented**

```
void Unity_NormalBlend_Reoriented_float(float3 A, float3 B, out float3 Out)
{
    float3 t = A.xyz + float3(0.0, 0.0, 1.0);
    float3 u = B.xyz * float3(-1.0, -1.0, 1.0);
    Out = (t / t.z) * dot(t, u) - u;
}
```
