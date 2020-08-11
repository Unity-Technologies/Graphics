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

| **Property** | **Description** |
| -------------------------------------- | ------------------------------------------------------------ |
|**UseColorMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UseColorMap.md)]|
|**BaseColor**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-BaseColor.md)]|
|**ColorMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-ColorMap.md)]|
|**UseNormalMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UseNormalMap.md)]|
|**NormalMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-NormalMap.md)]|
|**UseMetallicMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UseMetallicMap.md)]|
|**Metallic**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-Metallic.md)]|
|**MetallicMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-MetallicMap.md)]|
|**UseRoughnessMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UseRoughnessMap.md)]|
|**Roughness**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-Roughness.md)]|
|**RoughnessMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-RoughnessMap.md)]|
|**UseEmissiveMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UseEmissiveMap.md)]|
|**Emissive**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-Emissive.md)]|
|**EmissiveMap**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-EmissiveMap.md)]|
| **UseOpacityMap**                    | Toggles whether the material uses the alpha channel of the **ColorMap** texture or the **Opacity** property to set the opacity of its surface. Enable this checkbox to use the alpha channel of the **ColorMap** texture, disable this checkbox to use the **Opacity** property. |
| **Opacity** | The opacity of the surface.</br>If you enable **UseOpacityMap**, HDRP ignores this property. |
|**UVOffset**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UVOffset.md)]|
|**UVScale**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UVScale.md)]|
| **Enable GPU Instancing**              | [!include[](Snippets/ShaderProperties/Enable-GPU-Instancing.md)] |
| **Double Sided Global Illumination**   | [!include[](Snippets/ShaderProperties/Double-Sided-Global-Illumination.md)] |
| **Emission**                           | [!include[](Snippets/ShaderProperties/Emission.md)] |
| **- Global Illumination**              | [!include[](Snippets/ShaderProperties/Emission--Global-Illumination.md)] |
| **Motion Vector For Vertex Animation** | [!include[](Snippets/ShaderProperties/Motion-Vector-For-Vertex-Animation.md)] |