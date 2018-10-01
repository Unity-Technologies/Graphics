## Description

Creates a normal map from a height value defined by input **Vector 1**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Vector 1 | Height input value |
| Out | Output      |    Vector 3 | Output value |

## Shader Function

**World Space**
```
float3 worldDirivativeX = ddx(Position * 100);
float3 worldDirivativeY = ddy(Position * 100);
float3 crossX = cross(TangentMatrix[2].xyz, worldDirivativeX);
float3 crossY = cross(TangentMatrix[2].xyz, worldDirivativeY);
float3 d = abs(dot(crossY, worldDirivativeX));
float3 inToNormal = ((((In + ddx(In)) - In) * crossY) + (((In + ddy(In)) - In) * crossX)) * sign(d);
inToNormal.y *= -1.0;
Out = normalize((d * TangentMatrix[2].xyz) - inToNormal);
```

**Tangent Space**
```
Out = TransformWorldToTangent(Out, TangentMatrix);
```