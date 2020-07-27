# Decal Master Node

Decals are Materials that use the [HDRP Decal Shader](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@7.1/manual/Decal-Shader.html). You can use the Decal Shader Graph to author Decals that you can [project](Decal-Projector.html) or place into your Scene. The Decal Master Node is similar to the standard Decal Shader, except that you cannot use this version to author decals projected on transparent material.

## Creating and editing a Decal Material

When you select a Shader Graph Master Node in the Project view, you cannot edit any properties in the Inspector. Decal Materials use a Shader Graph Master Node, so you need to use a specific process to create and edit a Material that uses it. For information on how to do this, see [Creating and Editing HDRP Shader Graphs](Customizing-HDRP-materials-with-Shader-Graph.html). 

When you apply the node to a Material, the **Surface Options** and **Exposed Properties** become available to edit in the Material’s Inspector.

## Properties

There are properties on the Master Node, and properties on each Material. Master Node properties are inside the Shader Graph itself, in two sections:

- [**Master Node input ports**](#InputPorts): This section contains Shader Graph input ports on the Master Node itself. You can connect these to the output of other nodes or, in some cases, add your own values to them.
- [**Master Node settings menu**](#SettingsMenu): This section contains Settings you can use to customize your Master Node and expose more input ports.

 [Material properties](#MaterialProperties) are in the Inspector window for Materials that use this Shader.

<a name="InputPorts"></a>

### Decal Master Node input ports

![](Images/MasterNodeDecal1.png))

The following table describes the input ports on a Decal Master Node, including the property type and Shader stage used for each port. For more information on Shader stages, see [Shader Stage](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Shader-Stage.html).

| Property              | Type     | Stage    | Description                                                  |
| --------------------- | -------- | -------- | ------------------------------------------------------------ |
| **Position**          | Vector 3 | Vertex   | Set the object space position of the Material per vertex.    |
| **BaseColor**         | Vector 3 | Fragment | Set the color of the Material. To assign an image, connect a sampled Texture2D to this Master Node. |
| **BaseColor Opacity** | Vector 1 | Fragment | Set the Material's opacity. 0 is fully transparent, and 1 is fully opaque. |
| **Normal**            | Vector 3 | Fragment | Set the Material's normal value. The normals you assign should be in Tangent Space. |
| **Normal Opacity**    | Vector 1 | Fragment | Set the blend factor for the Material’s normals. A decal modifies the normals of the Material the decal projects onto. A value of 0 means that the decal does not affect the normals of the surface it projects onto. A value of 1 means that the decal fully overrides the normals of the surface. |
| **Metallic**          | Vector 1 | Fragment | Define how metallic the Material's appearance is (that is, how shiny it looks, and how much its appearance is based on the colours of the environment around it). 0 is completely non-metallic, and 1 is the maximum level of metallic appearance that Unity can achieve via this setting. |
| **Ambient Occlusion** | Vector 1 | Fragment | A multiplier for the intensity of diffuse global illumination. Set this to 0 to remove all global illumination. |
| **Smoothness**        | Vector 1 | Fragment | Set the appearance of the primary specular highlight. Every light ray that hits a smooth surface bounces off at predictable and consistent angles. For a perfectly smooth surface that reflects light like a mirror, set this to a value of 1. For a rougher surface, set this to a lower value. |
| **MAOS Opacity**      | Vector 1 | Fragment | Set the opacity of the **Metallic**, **Ambient Occlusion** and **Smoothness** values. |
| **Emission**          | Vector 3 | Fragment | Set the Material's emission color value. The RGB values you assign should be between 0-255. The Intensity value should be within the range -10 and 10. <br/>**Emission** only works on an Opaque Decal Shader. |

<a name="SettingsMenu"></a>

### Master Node settings menu

To view these properties, click the **Cog** in the top right of the Master Node.

![](Images/MasterNodeDecal2.png))

| Property                      | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Precision**                 | Select the precision of the calculations that the Shader processes. Lower precision calculations are faster but can cause issues, such as incorrect intensity for specular highlights.<br/>&#8226; **Inherit**: Uses global precision settings. This is the highest precision setting, so using it does not result in any precision issues, but Shader calculations are slower than other values.<br/>&#8226; **Float**: Uses single-precision floating-point instructions. This makes each calculation less resource-intensive, which speeds up calculations.<br/>&#8226; **Half**: Uses half-precision floating-point instructions. This is the fastest precision level, which means that calculations that use it are the least resource-intensive to process. This precision setting is the most likely one to result in issues, such as quantization (banding) artifacts and intensity clipping. **Half** precision is currently experimental for the Decal Shader. |
| **Affects BaseColor**         | Enable or disable the effect of the **BaseColor** property.  |
| **Affects Normal**            | Enable or disable the effect of the **Normal** property.     |
| **Affects Metal**             | Enable or disable the effect of the **Metal** property.      |
| **Affects Ambient Occlusion** | Enable or disable the effect of the **Ambient Occlusion** property. |
| **Affects Smoothness**        | Enable or disable the effect of the **Smoothness** property. |
| **Affects Emission**          | Enable or disable the effect of the **Emission** property.   |
| **Override ShaderGUI**        | Lets you override the [ShaderGUI](https://docs.unity3d.com/ScriptReference/ShaderGUI.html) that this Shader Graph uses. If `true`, the **ShaderGUI** property appears, which lets you specify the ShaderGUI to use. |
| **- ShaderGUI**                 | The full name of the ShaderGUI class to use, including the class path. |

<a name="MaterialProperties"></a>

### Material Properties

When creating a Decal Material from the Decal MAster node, following properties are available, alongside the properties that you exposed in the Shader Graph's Blackboard.

#### Surface Options

These properties allow you to set the affected attributes of the Material the decal is project onto when HDRP renders it into the Scene.

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Affect BaseColor**            | Enable the checkbox to make this decal use the **baseColor** properties. Otherwise the decal has no baseColor effect. Either disabled or enabled, the alpha channel of the base color is still use as an opacity for its other properties. |
| **Affect Normal**               | Enable the checkbox to make the decal use the **normal** property. Otherwise the decal do not modify normal of receiving Material. |
| **Affect Metal**                | Enable the checkbox to make the decal use the metallic property of its **Mask Map**. Otherwise the decal has no metallic effect. Uses the red channel of the **Mask Map**.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.html#Decals). |
| **Affect Ambient Occlusion**    | Enable the checkbox to make the decal use the ambient occlusion property of its **Mask Map**. Otherwise the decal has no ambient occlusion effect. Uses the green channel of the **Mask Map**.<br />This property only appears when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](HDRP-Asset.html#Decals). |
| **Affect Smoothness**           | Enable the checkbox to make the decal use the smoothness property of its **Mask Map**. Otherwise the decal has no smoothness effect. Uses the alpha channel of the **Mask Map**.<br /> |
| **Affect Emissive**             | Enable the checkbox to make this decal emissive. When enabled, this Material appears self-illuminated and acts as a visible source of light. This property don't work with Transparent Material receiver. |

#### Sorting Inputs

These properties allow you to change the rendering behavior of the decal.

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Draw Order**            | Controls the order in which HDRP draws decals in the Scene. HDRP draws decals with lower values first, so it draws decals with a higher draw order value on top of those with lower values. This feature works for decals projected on opaque and transparent surfaces.<br />**Note**: This property only applies to decals the [Decal Projector](Decal-Projector.md) creates and has no effect on Mesh decals. Additionally, if you have multiple Decal Materials with the same **Draw Order**, the order HDRP renders them in depends on the order you create the Materials. HDRP renders Decal Materials you create first before those you create later with the same **Draw Order**. |
| **Mesh Decal Depth Bias** | A depth bias that HDRP applies to the decal’s Mesh to stop it from overlapping with other Meshes. A negative value draws the decal in front of any overlapping Mesh, while a positive value offsets the decal and draw it behind. This property only affects decal Materials directly attached to GameObjects with a Mesh Renderer, so Decal Projectors do not use this property. |
