# Autodesk Interactive transparent shader

The Autodesk Interactive transparent shader replicates the Interactive PBS with the transparent preset available in Autodesk® 3DsMax and Autodesk® Maya for the High Definition Render Pipeline (HDRP). When Unity imports an FBX exported from one of these softwares, if the FBX includes materials with Interactive PBS shaders, Unity imports these materials as Autodesk Interactive materials. The material properties and textures inputs are identical between these two materials. The materials themselves also look and respond to light similarly. Note that there are slight differences between what you see in Autodesk® Maya or Autodesk® 3DsMax and what you see in Unity.

Autodesk® Maya or Autodesk® 3DsMax also include two variants of this shader, which are also available in HDRP:

- [Autodesk Interactive](Autodesk-Interactive-Shader.md)
- [Autodesk Interactive Masked](Autodesk-Interactive-Shader-Masked.md)

Note that this shader is implemented as a [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html).

## Creating an Autodesk Interactive transparent material

When Unity imports an FBX with a compatible Autodesk shader, it automatically creates an Autodesk Interactive material. If you want to manually create an Autodesk Interactive material:

1. Create a new material (menu: **Assets > Create > Material**).
2. In the Inspector for the Material, click the **Shader** drop-down then click **HDRP > Autodesk Interactive > AutodeskInteractiveTransparent**.

### Properties

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties-uxml/autodesk-interactive/use-color-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/base-color.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/color-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/use-normal-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/normal-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/use-metallic-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/metallic.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/metallic-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/use-roughness-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/roughness.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/roughness-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/use-emissive-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/emissive.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/emissive-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/use-opacity-map.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/opacity.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/uv-offset.md)]
[!include[](snippets/shader-properties-uxml/autodesk-interactive/uv-scale.md)]
[!include[](snippets/shader-properties-uxml/general/enable-gpu-instancing.md)]
[!include[](snippets/shader-properties-uxml/general/double-sided-global-illumination.md)]
[!include[](snippets/shader-properties-uxml/general/emission.md)]
[!include[](snippets/shader-properties-uxml/general/emission-global-illumination.md)]
[!include[](snippets/shader-properties-uxml/general/motion-vector-for-vertex-animation.md)]
</table>