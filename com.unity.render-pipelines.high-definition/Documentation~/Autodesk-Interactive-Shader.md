# Autodesk Interactive shader

The Autodesk Interactive shader replicates the Interactive PBS shader available in Autodesk® 3DsMax and Autodesk® Maya for the High Definition Render Pipeline (HDRP). When Unity imports an FBX exported from one of these softwares, if the FBX includes materials with Interactive PBS shaders, Unity imports these materials as Autodesk Interactive materials. The material properties and textures inputs are identical between these two materials. The materials themselves also look and respond to light similarly. Note that there are slight differences between what you see in Autodesk® Maya or Autodesk® 3DsMax and what you see in Unity.

![img](https://lh3.googleusercontent.com/T02eER1w_cblTlw4VQu8bKaCqo_QZmPLaWP_73OEqgh8LroKr8uVd6uUornm64R0ANPU8x11176MBTcd_WEJqpm_29uUkCUgSAH8z8GzNYMcLdDx87dIgFXji_fu0bqaf7jDkrUO)

Interactive PBS materials seen in **Autodesk® Maya** viewport.

![Interactive PBS materials imported in Unity](https://lh3.googleusercontent.com/ZpWu62eJNvhppjuw-t6J_beVHjZrCYGZVW07hvrz6Kyt2x1YdTrqX763Xy2f_Q1azWcRMGJZkwkHxmFlfxVXVpQEs2PTTYJDhB_opedVCj0fyM1VK-dBZLhqbglxxIsZUSN8LEwt)

The same materials imported from FBX seen in HDRP.

Autodesk® Maya or Autodesk® 3DsMax also include two variants of this shader, which are also available in HDRP:

- [Autodesk Interactive Masked](Autodesk-Interactive-Shader-Masked.md)
- [Autodesk Interactive Transparent](Autodesk-Interactive-Shader-Transparent.md).

Note that this shader is implemented as a [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html).

## **Creating an Autodesk Interactive material**

When Unity imports an FBX with a compatible Autodesk shader, it automatically creates an Autodesk Interactive material. If you want to manually create an Autodesk Interactive material:

1. Create a new material (menu: **Assets > Create > Material**).
2. In the Inspector for the Material, click the **Shader** drop-down then click **HDRP > Autodesk Interactive > AutodeskInteractive**.

### **Properties**

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
| **UseAoMap**                           | Toggles whether the material uses the **AoMap** property.    |
| **AoMap**                              | The texture that defines the occlusion map to simulate shadowing of ambient light. |
|**UVOffset**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UVOffset.md)]|
|**UVScale**|[!include[](Snippets/ShaderProperties/Autodesk-Interactive-UVScale.md)]|
| **Enable GPU Instancing**              | Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material in one batch when possible. This makes rendering faster. HDRP cannot render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you cannot [static-batch](https://docs.unity3d.com/Manual/DrawCallBatching.html) GameObjects that have an animation based on the object pivot, but the GPU can instance them. |
| **Double Sided Global Illumination**   | When enabled, the lightmapper accounts for both sides of the geometry when calculating Global Illumination. Backfaces are not rendered or added to lightmaps, but get treated as valid when seen from other objects. When using the Porgressive Lightmapper backfaces bounce light using the same emission and albedo as frontfaces. |
| **Emission**                           | Toggles whether emission affects global illumination.        |
| **- Global Illumination**              | The mode HDRP uses to determine how color emission interacts with global illumination.<br />&#8226; **Realtime**: Select this option to make emission affect the result of real-time global illumination.<br />&#8226; **Baked**: Select this option to make emission only affect global illumination during the baking process.<br />&#8226; **None**: Select this option to make emission not affect global illumination. |
| **Motion Vector For Vertex Animation** | Enable the checkbox to make HDRP write motion vectors for GameObjects that use vertex animation. This removes the ghosting that vertex animation can cause. |

##  