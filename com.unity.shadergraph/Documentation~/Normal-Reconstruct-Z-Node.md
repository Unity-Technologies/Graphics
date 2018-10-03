## Description

Derives the correct Z value for generated normal maps using a given **X** and **Y** value in a **Vector 2**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Vector 2 | Normal X and Y value |
| Out | Output      |    Vector 3 | Output value |

## Shader Function

```
float reconstructZ = sqrt(1 - ( In.x * In.x + In.y * In.y));
float3 normalVector = float3(In.x, In.y, reconstructZ);
Out = normalize(normalVector);
```