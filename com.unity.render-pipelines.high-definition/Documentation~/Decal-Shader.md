# Decal Shader

![](Images/HDRPFeatures-DecalShader.png)

## Properties

### Surface Options

These properties allow you to set the affected attributes of the Material the decal is project onto when HDRP renders it into the Scene.

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Affect BaseColor**            | Enable the checkbox to make this decal use the **baseColor** properties. Otherwise the decal has no baseColor effect. Regardless of whether you enable or disable this property, HDRP still uses the alpha channel of the base color as an opacity for the other properties. |
| **Affect Normal**               | Enable the checkbox to make the decal use the **normal** property. Otherwise, the decal does not modify the normals of the receiving Material. |
| **Affect Metal**                | Enable the checkbox to make the decal use the metallic property of its **Mask Map**. Otherwise the decal has no metallic effect. Uses the red channel of the **Mask Map**.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.md#Decals). |
| **Affect Ambient Occlusion**    | Enable the checkbox to make the decal use the ambient occlusion property of its **Mask Map**. Otherwise the decal has no ambient occlusion effect. Uses the green channel of the **Mask Map**.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.md#Decals). |
| **Affect Smoothness**           | Enable the checkbox to make the decal use the smoothness property of its **Mask Map**. Otherwise the decal has no smoothness effect. Uses the alpha channel of the **Mask Map**.<br /> |
| **Affect Emissive**             | Enable the checkbox to make this decal emissive. When enabled, this Material appears self-illuminated and acts as a visible source of light. This property does not work with transparent receiving Materials. |


### Surface Inputs

These properties allow you to set the inputs that affect the behavior of the decal when HDRP renders it into the Scene.

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Base Map / Opacity**          | Allows you to specify a Texture for the decal as well as modify the decal’s base color and opacity. |
| **Normal Map**                  | A map that modifies the normal property of the Material the decal projects onto. If no normal is provided, a default normal poting up in tangent space is used. |
| **Normal Opacity channel**      | Use this drop-down to select the source of normal map opacity. You can select either **Base Color Map Alpha**, **Mask Map Blue** or **Mask Opacity**:<br />&#8226; **Base Color Map Alpha**: Uses the alpha channel of the **Base Map**’s color picker as opacity.<br />&#8226; **Mask Map Blue**: Uses the blue channel of the **Mask Map** as opacity.<br />&#8226; **Opacity Mask**: Uses the Mask Opacity. |
| **Mask Map**                    | Assign a [channel-packed Texture](Glossary.md#ChannelPacking) with the following Material maps in its RGBA channels.<br />&#8226; **Red**: Stores the metallic map. <br />&#8226; **Green**: Stores the ambient occlusion map.<br />&#8226; **Blue**: Stores the opacity mask map.<br />&#8226; **Alpha**: Stores the smoothness map.<br />For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.md#MaskMap). |
| **Metallic**                    | Use the slider to set the strength of the metallic effect of the decal. Choose a value from 0 and 1 where 0 means no effect and 1 means full effect.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.md#Decals). |
| **Ambient Occlusion**           | Use the slider to set the strength of the ambient occlusion effect of the decal.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.md#Decals). |
| **Smoothness**                  | Use the slider to set the strength of the smoothness of the decal. |
| **Metallic Remapping**          | Remaps the metallic values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, it condenses the original range down to the new range uniformly.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.md#Decals). |
| **AO Remapping**                | Remaps the ambient occlusion values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, it condenses the original range down to the new range uniformly.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.md#Decals). |
| **Smoothness Remapping**        | Remaps the smoothness values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, it condenses the original range down to the new range uniformly. |
| **Mask Opacity channel**        | Use this drop-down to select the source of the **Mask Map** opacity. You can select either **Base Color Map Alpha**, **Mask Map Blue** or **Mask Opacity**:<br />&#8226; **Base Color Map Alpha**: Uses the alpha channel of the **Base Map**’s color picker as opacity.<br />&#8226; **Mask Map Blue**: Uses the blue channel of the **Mask Map** as opacity.<br />&#8226; **Opacity Mask**: Uses the Mask Opacity. |
| **Scale Mask Map Blue Channel** | Use the slider to set the multiplier for the opacity (blue channel of the **Mask Map**). A value of 0 means no effect and a value of 1 means full effect. |
| **Mask opacity**                | Use the slider to set the opacity value to use for Mettalic, Ambient Occlusion and Smoothness if **Mask Opacity channel** is setup to Mask Opacity. A value of 0 means no effect and a value of 1 means full effect. |
| **Global Opacity**              | Use the slider to set the opacity of the decal. The lower the value, the more transparent the decal. The opacity combine with all the other opacity control. |
| **Use Emission Intensity**      | Enable the checkbox to use a separate LDR color and intensity value to set the emission color for this Material. Disable this checkbox to only use an HDR color to handle the color and emission color intensity. |
| **Emission Map**                | Assign a Texture that this Material uses for emission. You can also use the color picker to select a color that HDRP multiplies by the Texture. If you do not set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **Emission Intensity**          | Set the overall strength of the emission effect for this Material. Use the drop-down to select one of the following [physical light units](Physical-Light-Units.md) to use for intensity:<br />&#8226; [Luminance](Physical-Light-Units.md#Nits)<br />&#8226; [EV<sub>100</sub>](Physical-Light-Units.md#EV)<br />This property only appears when you enable the **Use Emission Intensity** checkbox. |

### Sorting Inputs

These properties allow you to change the rendering behavior of the decal.

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Draw Order**            | Controls the order in which HDRP draws decals in the Scene. HDRP draws decals with lower values first, so it draws decals with a higher draw order value on top of those with lower values. This feature works for decals projected on opaque and transparent surfaces.<br />**Note**: This property only applies to decals the [Decal Projector](Decal-Projector.md) creates and has no effect on Mesh decals. Additionally, if you have multiple Decal Materials with the same **Draw Order**, the order HDRP renders them in depends on the order you create the Materials. HDRP renders Decal Materials you create first before those you create later with the same **Draw Order**. |
| **Mesh Decal Depth Bias** | A depth bias that HDRP applies to the decal’s Mesh to stop it from overlapping with other Meshes. A negative value draws the decal in front of any overlapping Mesh, while a positive value offsets the decal and draw it behind. This property only affects decal Materials directly attached to GameObjects with a Mesh Renderer, so Decal Projectors do not use this property. |

### HDRP Asset properties

You can edit global settings that apply to all decals in your Scene in your Unity Project’s HDRP Asset. For information on these properties, see the [**Decals** section of the HDRP Asset documentation](HDRP-Asset.md#Decals).
