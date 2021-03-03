# Parallax Mapping Node

## Description

The Parallax Mapping node lets you create a parallax effect that displaces a Material's UVs to create the illusion of depth inside a Material. This implementation uses the single step process that does not account for occlusion. For information on how the effect looks, see the [Height Map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterHeightMap.html) page.

## Ports

| Name | **Direction** | Type | Description |
| --- | --- | --- | --- |
| **Heightmap** | Input | Texture2D | The Texture that specifies the depth of the displacement. |
| **Heightmap Sampler** | Input | Sampler State | The Sampler to sample **Heightmap** with. |
| **Amplitude** | Input | Float | A multiplier to apply to the height of the Heightmap (in centimeters). |
| **UVs** | Input | Vector2 | The UVs that the sampler uses to sample the Texture. |
| **Parallax UVs** | Output| Vector2 | The UVs after adding the parallax offset. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2 _ParallaxMapping_ParallaxUVs = UVs.xy + ParallaxMapping(Heightmap, Heightmap_Sampler, IN.TangentSpaceViewDirection, Amplitude * 0.01, UVs.xy);
```
