# Parallax Occlusion Mapping Node

## Description

The Parallax Occlusion Mapping (POM) Node allows you to create a parallax effect that displaces a Material's UVs and depth to create the illusion of depth inside a Material.

## Ports

| Name | **Direction** | Type | Description |
| --- | --- | --- | --- |
| **Heightmap** | Input | Texture2D | The Texture that specifies the depth of the displacement. |
| **Heightmap Sampler** | Input | Sampler State | The Sampler to sample **Heightmap** with. |
| **Amplitude** | Input | Float | A multiplier to apply to the height of the Heightmap (in centimeters). |
| **Steps** | Input | Float | The number of steps that the linear search of the algorithm performs. |
| **UVs** | Input | Vector2 | The UVs that the sampler uses to sample the Texture. |
| **Lod** | Input | Float | The level of detail to use to sample **Heightmap**. |
| **Lod Threshold** | Input | Float | The **Heightmap** mip level where the POM effect begins to fade out. This is equivalent to the **Fading Mip Level Start** property in the High Definition Render Pipeline's (HDRP) [Lit Material](Lit-Shader.md). |
| **Depth Offset** | Output |Float | The offset to apply to the depth buffer to produce the illusion of depth. To enable effects that rely on the depth buffer, such as shadows and screen space ambient occlusion, connect this output to the **Depth Offset** on the Master Node. |
| **Parallax UVs** | Output| Vector2 | The UVs after adding the parallax offset. |


## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 ParallaxOcclusionMapping_ViewDir = IN.TangentSpaceViewDirection * GetDisplacementObjectScale().xzy;
float ParallaxOcclusionMapping_NdotV = ParallaxOcclusionMapping_ViewDir.z;
float ParallaxOcclusionMapping_MaxHeight = Amplitude * 0.01;

// Transform the view vector into the UV space.
float3 ParallaxOcclusionMapping_ViewDirUV    = normalize(float3(ParallaxOcclusionMapping_ViewDir.xy * ParallaxOcclusionMapping_MaxHeight, ParallaxOcclusionMapping_ViewDir.z)); // TODO: skip normalize

PerPixelHeightDisplacementParam ParallaxOcclusionMapping_POM;
ParallaxOcclusionMapping_POM.uv = UVs.xy;

float ParallaxOcclusionMapping_OutHeight;
float2 _ParallaxOcclusionMapping_ParallaxUVs = UVs.xy + ParallaxOcclusionMapping(Lod, Lod_Threshold, Steps, ParallaxOcclusionMapping_ViewDirUV, ParallaxOcclusionMapping_POM, ParallaxOcclusionMapping_OutHeight);

float _ParallaxOcclusionMapping_PixelDepthOffset = (ParallaxOcclusionMapping_MaxHeight - ParallaxOcclusionMapping_OutHeight * ParallaxOcclusionMapping_MaxHeight) / max(ParallaxOcclusionMapping_NdotV, 0.0001);
```
