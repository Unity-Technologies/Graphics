# Normal Blend Node

## Description

Blends two normal maps defined by inputs **A** and **B**, normalizing the result to produce a valid normal map representing the combined surface detail.

## Ports

| Name | Direction | Type     | Binding | Description             |
|:-----|:----------|:---------|:--------|:------------------------|
| A    | Input     | Vector 3 | None    | First input normal map  |
| B    | Input     | Vector 3 | None    | Second input normal map |
| Out  | Output    | Vector 3 | None    | Blended normal map      |

## Controls

| Name | Type     | Options    | Description                                                                                                                                                        |
|:-----|:---------|:-----------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Mode | Dropdown | Default    | Blends the two normal maps by adding their x and y components and multiplying their z components, then normalizes the result to ensure a valid normal direction. |
| Mode | Dropdown | Reoriented | Blends the two normal maps using a mathematically correct method, ensuring the resulting normal map represents a realistic combined surface.                       |

## Generated Code Example

The following example code demonstrates how the node blends normals in each **Mode**:

**Default**

Adds the x and y components (R and G channels) of the input normals, multiplies the z components (B channel), then normalizes the result.

```
void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = normalize(float3(A.rg + B.rg, A.b * B.b));
}
```


**Reoriented**

Blends the input normals using a reoriented method that makes the resulting surface normal look realistic.

```
void Unity_NormalBlend_Reoriented_float(float3 A, float3 B, out float3 Out)
{
    float3 t = A.xyz + float3(0.0, 0.0, 1.0);
    float3 u = B.xyz * float3(-1.0, -1.0, 1.0);
    Out = (t / t.z) * dot(t, u) - u;
}
```
