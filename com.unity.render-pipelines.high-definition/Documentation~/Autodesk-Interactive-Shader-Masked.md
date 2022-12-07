# Autodesk Interactive masked shader

The Autodesk Interactive masked shader replicates the Interactive PBS with the masked preset available in Autodesk® 3DsMax and Autodesk® Maya for the High Definition Render Pipeline (HDRP). It acts like a [cutout shader](https://docs.unity3d.com/Manual/shader-TransparentCutoutFamily.html). When Unity imports an FBX exported from one of these softwares, if the FBX includes materials with Interactive PBS shaders, Unity imports these materials as Autodesk Interactive materials. The material properties and textures inputs are identical between these two materials. The materials themselves also look and respond to light similarly.

**Note**: There are slight differences between what you see in Autodesk® Maya or Autodesk® 3DsMax and what you see in Unity.

Autodesk® Maya or Autodesk® 3DsMax also include two variants of this shader, which are also available in HDRP:

- [Autodesk Interactive](Autodesk-Interactive-Shader.md)
- [Autodesk Interactive Transparent](Autodesk-Interactive-Shader-Transparent.md)

**Note**: This shader is implemented as a [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html).

## Creating an Autodesk Interactive masked material

When Unity imports an FBX with a compatible Autodesk shader, it automatically creates an Autodesk Interactive material. If you want to manually create an Autodesk Interactive material:

1. Create a new material (menu: **Assets** > **Create** > **Material**).
2. In the Inspector for the Material, click the **Shader** drop-down then click **HDRP** > **Autodesk Interactive** > **AutodeskInteractiveMasked**.

### Properties

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/autodesk-interactive/use-color-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/base-color.md)]
[!include[](snippets/shader-properties/autodesk-interactive/color-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/use-normal-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/normal-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/use-metallic-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/metallic.md)]
[!include[](snippets/shader-properties/autodesk-interactive/metallic-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/use-roughness-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/roughness.md)]
[!include[](snippets/shader-properties/autodesk-interactive/roughness-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/use-emissive-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/emissive.md)]
[!include[](snippets/shader-properties/autodesk-interactive/emissive-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/use-opacity-map-masked.md)]
[!include[](snippets/shader-properties/autodesk-interactive/mask-map.md)]
[!include[](snippets/shader-properties/autodesk-interactive/mask-threshold.md)]
[!include[](snippets/shader-properties/autodesk-interactive/uv-offset.md)]
[!include[](snippets/shader-properties/autodesk-interactive/uv-scale.md)]
[!include[](snippets/shader-properties/general/enable-gpu-instancing.md)]
[!include[](snippets/shader-properties/general/double-sided-global-illumination.md)]
[!include[](snippets/shader-properties/general/emission.md)]
[!include[](snippets/shader-properties/general/emission-global-illumination.md)]
[!include[](snippets/shader-properties/general/motion-vector-for-vertex-animation.md)]
</table>
