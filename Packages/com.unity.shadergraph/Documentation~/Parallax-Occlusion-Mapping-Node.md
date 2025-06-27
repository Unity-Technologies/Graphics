# Parallax Occlusion Mapping Node

## Description

You can use the Parallax Occlusion Mapping (POM) node to create a parallax effect that displaces a material's UVs and depth to create the illusion of depth inside that material.

If you receive a texture sampling error while using this node in a graph that includes Custom Function nodes or Subgraphs, try upgrading to Shader Graph version 10.3 or later. This may resolve the errors.

When you assign the same Texture2D to a POM node and a Sample Texture 2D node, you need to avoid transforming the UV coordinates twice. To prevent this, connect the Split Texture Transform node’s **Texture Only** port to the Sample Texture 2D Node’s **UV** port.

## Ports

| Name | **Direction** | Type | Description |
| --- | --- | --- | --- |
| **Heightmap** | Input | Texture2D | The Texture that specifies the depth of the displacement. |
| **Heightmap Sampler** | Input | Sampler State | The Sampler to sample **Heightmap** with. |
| **Amplitude** | Input | Float | A multiplier to apply to the height of the **Heightmap** (in centimeters). |
| **Steps** | Input | Float | The number of steps that the linear search of the algorithm performs. |
| **UVs** | Input | Vector2 | The UVs that the sampler uses to sample the Texture. |
| **Tiling** | Input | Vector2 | The tiling to apply to the input UVs. |
| **Offset**| Input | Vector2 | The offset to apply to the input UVs. |
| **Primitive Size** | Input | Vector2 | Size of the UV space in object space. For example, a Unity built-in Plane mesh has a primitive size of (10,10). |
| **LOD** | Input | Float | The level of detail to use to sample the **Heightmap**. This value should always be positive. |
| **LOD Threshold** | Input | Float | The **Heightmap** mip level where the Parallax Occlusion Mapping effect begins to fade out. This is equivalent to the **Fading Mip Level Start** property in the High Definition Render Pipeline's (HDRP) [Lit Material](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Lit-Shader.html). |
| **Pixel Depth Offset** | Output |Float | The offset to apply to the depth buffer to produce the illusion of depth. Connect this output to the **Depth Offset** on the Master Node to enable effects that rely on the depth buffer, such as shadows and screen space ambient occlusion. |
| **Parallax UVs** | Output| Vector2 | UVs that you have added the parallax offset to. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 ParallaxOcclusionMapping_ViewDir = IN.TangentSpaceViewDirection * GetDisplacementObjectScale().xzy;
float ParallaxOcclusionMapping_NdotV = ParallaxOcclusionMapping_ViewDir.z;
float ParallaxOcclusionMapping_MaxHeight = Amplitude * 0.01;
ParallaxOcclusionMapping_MaxHeight *= 2.0 / ( abs(Tiling.x) + abs(Tiling.y) );

float2 ParallaxOcclusionMapping_UVSpaceScale = ParallaxOcclusionMapping_MaxHeight * Tiling / PrimitiveSize;

// Transform the view vector into the UV space.
float3 ParallaxOcclusionMapping_ViewDirUV    = normalize(float3(ParallaxOcclusionMapping_ViewDir.xy * ParallaxOcclusionMapping_UVSpaceScale, ParallaxOcclusionMapping_ViewDir.z)); // TODO: skip normalize

PerPixelHeightDisplacementParam ParallaxOcclusionMapping_POM;
ParallaxOcclusionMapping_POM.uv = UVs.xy;

float ParallaxOcclusionMapping_OutHeight;
float2 _ParallaxOcclusionMapping_ParallaxUVs = UVs.xy + ParallaxOcclusionMapping(Lod, Lod_Threshold, Steps, ParallaxOcclusionMapping_ViewDirUV, ParallaxOcclusionMapping_POM, ParallaxOcclusionMapping_OutHeight);

float _ParallaxOcclusionMapping_PixelDepthOffset = (ParallaxOcclusionMapping_MaxHeight - ParallaxOcclusionMapping_OutHeight * ParallaxOcclusionMapping_MaxHeight) / max(ParallaxOcclusionMapping_NdotV, 0.0001);
```
