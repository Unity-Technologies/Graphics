# Object Node

## Description

Provides access to various parameters of the currently rendering **Object**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position      | Output | Vector 3 | None | Object position in world space |
| Scale       | Output | Vector 3 | None | Object scale in world space |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 _Object_Position = unity_ObjectToWorld._m03_m13_m23;
float3 _Object_Scale = float3(length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
                      length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
                      length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)));
```