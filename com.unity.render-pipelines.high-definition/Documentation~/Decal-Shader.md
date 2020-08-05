# Decal Shader

The High Definition Render Pipeline (HDRP) includes the [Decal Projector](Decal-Projector.html) component, which you can use to project specific Materials into your Scene to create realistic-looking decals. These Materials must use the **HDRP/Decal** Shader. Use this Shader to create decal Materials that you can place, or project, into your Scene.

![](Images/HDRPFeatures-DecalShader.png)

## Properties

### Surface Inputs

These properties allow you to set the inputs that affect the behavior of the decal when HDRP renders it into the Scene.

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Base Map**                    | Allows you to specify a Texture for the decal as well as modify the decal’s base color. |
| **Affect BaseColor**            | Enable the checkbox to make this decal use the alpha channel of the base color as an opacity for its other properties. This does not modify the base color of the Material the decal projects on. |
| **Normal Map**                  | A map that modifies the normal property of the Material the decal projects onto. |
| **Normal Opacity channel**      | Use this drop-down to select the source of normal map opacity. You can select either **Base Color Map Alpha** or **Mask Map Blue**:<br />&#8226; **Base Color Map Alpha**: Uses the alpha channel of the **Base Map**’s color picker as opacity.<br />&#8226; **Mask Map Blue**: Uses the blue channel of the **Mask Map** as opacity. |
| **Mask Map**                    | Assign a [channel-packed Texture](Glossary.html#ChannelPacking) with the following Material maps in its RGBA channels.<br />&#8226; **Red**: Stores the metallic map. <br />&#8226; **Green**: Stores the ambient occlusion map.<br />&#8226; **Blue**: Stores the detail mask map.<br />&#8226; **Alpha**: Stores the smoothness map.<br />For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.html#MaskMap). |
| **Smoothness Remapping**        | Remaps the smoothness values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, it condenses the original range down to the new range uniformly. |
| **Metallic Scale**              | Use the slider to set the strength of the metallic effect of the decal. This targets the values of the Metallic map in the Red channel of the **Mask Map**. Choose a value from 0 and 1 where 0 means no effect and 1 means full effect.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.html#Decals). |
| **AO Remapping**                | Remaps the ambient occlusion values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, it condenses the original range down to the new range uniformly.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.html#Decals). |
| **Mask Opacity channel**        | Use this drop-down to select the source of the **Mask Map** opacity. You can select either **Base Color Alpha** or **Mask Map Blue**.<br />&#8226; **Base Color Map Alpha** uses the alpha channel of the **Base Map**’s color picker as opacity.<br />&#8226; **Mask Map Map Blue** uses the blue channel of the **Mask Map** as opacity. |
| **Affect Metal**                | Enable the checkbox to make the decal use the metallic property of its **Mask Map**. Otherwise the decal has no metallic effect. Uses the red channel of the **Mask Map**.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.html#Decals). |
| **Affect AO**                   | Enable the checkbox to make the decal use the ambient occlusion property of its **Mask Map**. Otherwise the decal has no ambient occlusion effect. Uses the green channel of the **Mask Map**.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.html#Decals). |
| **Affect Smoothness**           | Enable the checkbox to make the decal use the smoothness property of its **Mask Map**. Otherwise the decal has no smoothness effect. Uses the alpha channel of the **Mask Map**.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.html#Decals). |
| **Scale Mask Map Blue Channel** | Use the slider to set the multiplier for the opacity (blue channel of the **Mask Map**). A value of 0 means no effect and a value of 1 means full effect. |
| **Global Opacity**              | Use the slider to set the opacity of the decal. The lower the value, the more transparent the decal. |
| **Emissive**                    | Enable the checkbox to make this decal emissive. When enabled, this Material appears self-illuminated and acts as a visible source of light. |
| **Use Emission Intensity**      | Enable the checkbox to use a separate LDR color and intensity value to set the emission color for this Material. Disable this checkbox to only use an HDR color to handle the color and emission color intensity. |
| **Emission Map**                | Assign a Texture that this Material uses for emission. You can also use the color picker to select a color that HDRP multiplies by the Texture. If you do not set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **Emission Intensity**          | Set the overall strength of the emission effect for this Material. Use the drop-down to select one of the following [physical light units](Physical-Light-Units.html) to use for intensity:<br />&#8226; [Luminance](Physical-Light-Units.html#Luminance)<br />&#8226; [EV<sub>100</sub>](Physical-Light-Units.html#EV)<br />This property only appears when you enable the **Use Emission Intensity** checkbox. |

**Note:** To alter the **Affect Metal**, **Affect AO**, and **Affect Smoothness** properties, you must enable the **Metal and Ambient Occlusion properties** in your Unity Project’s [HDRP Asset](HDRP-Asset.html). Navigate to **HDRP Asset** > **Decals** and tick the **Metal and AO properties** checkbox. Otherwise, the decal only affects smoothness.

### Sorting Inputs

These properties allow you to change the rendering behavior of the decal.

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Draw Order**            | Controls the order in which HDRP draws decals in the Scene. HDRP draws decals with lower values first, so it draws decals with a higher draw order value on top of those with lower values. This feature works for decals projected on opaque and transparent surfaces.<br />**Note**: This property only applies to decals the [Decal Projector](Decal-Projector.md) creates and has no effect on Mesh decals. Additionally, if you have multiple Decal Materials with the same **Draw Order**, the order HDRP renders them in depends on the order you create the Materials. HDRP renders Decal Materials you create first before those you create later with the same **Draw Order**. |
| **Mesh Decal Depth Bias** | A depth bias that HDRP applies to the decal’s Mesh to stop it from overlapping with other Meshes. A negative value draws the decal in front of any overlapping Mesh, while a positive value offsets the decal and draw it behind. This property only affects decal Materials directly attached to GameObjects with a Mesh Renderer, so Decal Projectors do not use this property. |

### HDRP Asset properties

You can edit global settings that apply to all decals in your Scene in your Unity Project’s HDRP Asset. For information on these properties, see the [**Decals** section of the HDRP Asset documentation](HDRP-Asset.html#Decals).