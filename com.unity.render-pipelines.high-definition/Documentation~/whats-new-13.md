# What's new in HDRP version 13 / Unity 2022.1

This page contains an overview of new features, improvements, and issues resolved in version 13 of the High Definition Render Pipeline (HDRP), embedded in Unity 2022.1.

## Added

### New Runtime APIs

#### Materials

From this HDRP version, you can use new APIs to run shader validation steps from script at runtime, both in the editor and in standalone builds. You can use this to change the keyword state or one or more properties in order to enable or disable HDRP shader features on a Material.
For more information, see the [Material Scripting API documentation](Material-API.md).

#### Diffusion Profiles

All properties of diffusion profile are now public and can be modified during runtime or in the editor.
See all the available properties in the [scripting documentation](../api/UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings.html).

#### Lights

Access to a light IES profile is now available in the editor through the [HDLightUtils](../api/UnityEditor.Rendering.HighDefinition.HDLightUtils.html), as well as manually setting the [IES texture](../api/UnityEngine.Rendering.HighDefinition.HDAdditionalLightData.html#UnityEngine_Rendering_HighDefinition_HDAdditionalLightData_IESTexture) during runtime or in the editor.

### Material Variants

![](Images/material-variants.png)

HDRP 13.0 comes with the support of Material Variants for all Shaders and Shader Graphs.
Material Variants allow you to have a set of predefined variations of a Material, in which you can override specific properties.
Additionally, Materials can put a lock on a property to prevent children from modifying the value.

### Access main directional light from ShaderGraph

From HDRP version 13.0, [ShaderGraph](https://docs.unity3d.com/Packages/com.unity.shadergraph@13.1/manual/index.html) includes a new node called *Main Light Direction* that you can use to control the direction of the main light.
For more information, see the [Main Light Direction Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@13.1/manual/Main-Light-Direction-Node.html).

### HDR Output Support

HDRP 13.0 introduces support for HDR display output, including both the HDR10 and scRGB standards.

As a result, HDRP is now able to take advantage of the higher brightness contrast and wider color gamut capabilities of HDR displays.

This functionality includes a variety of customization options for adapting content for a variety of displays based on device metadata or user preferences.

For more information consult the [HDR Output](HDR-Output.md) documentation

## Updated

### Depth Of Field
HDRP version 13 includes optimizations in the physically based depth of field implementation. In particular, image regions that are out-of-focus are now computed at lower resolution, while in-focus regions retain the full resolution. For many scenes this results in significant speedup, without any visible reduction in image quality.
