# Normal From Texture Node

## Description

Converts a height map defined by input **Texture** into a normal map. UV values and sampler state can be defined by inputs **UV** and **Sampler** respectively. If nothing is connected to these ports they will use default values from the inputs. See [Port Bindings](Port-Bindings.md) for more information.

The strength of the created normal map can be defined by inputs **Offset** and **Strength**, where **Offset** defines the maximum distance of a normal detail and **Strength** acts as a multiplier to the result.

If you experience texture sampling errors while using this node in a graph which includes Custom Function Nodes or Sub Graphs, you can resolve them by upgrading to version 10.3 or later. 

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture      | Input | Texture | None | Height map |
| UV      | Input | Vector 2 | UV | Texture coordinates |
| Sampler      | Input | Sampler State | None | Sampler for **Texture** |
| Offset      | Input | Float    | None | Amount to offset samples |
| Strength      | Input | Float    | None | Strength multiplier |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_NormalFromTexture_float(Texture texture, SamplerState Sampler, float2 UV, float Offset, float Strength, out float3 Out)
{
    Offset = pow(Offset, 3) * 0.1;
    float2 offsetU = float2(UV.x + Offset, UV.y);
    float2 offsetV = float2(UV.x, UV.y + Offset);
    float normalSample = Texture.Sample(Sampler, UV);
    float uSample = Texture.Sample(Sampler, offsetU);
    float vSample = Texture.Sample(Sampler, offsetV);
    float3 va = float3(1, 0, (uSample - normalSample) * Strength);
    float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
    Out = normalize(cross(va, vb));
}
```
