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

### Support for full ACES tone mapper

HDRP 13.0 introduces an option to enable the usage of full ACES tonemapper rather than the default approximation.

This will help projects that want to have a matching reference with other projects and that are not comfortable with the shortcomings of the approximation.

### Depth Of Field
HDRP version 13 includes optimizations in the physically based depth of field implementation. In particular, image regions that are out-of-focus are now computed at lower resolution, while in-focus regions retain the full resolution. For many scenes this results in significant speedup, without any visible reduction in image quality.

### Mixed Cached Shadows for Directional Lights

HDRP 13.0 introduces the option to use the [Always Draw Dynamic](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Shadows-in-HDRP.html#mixed-cached-shadow-maps) option for directional light shadow maps.

This effectively allow to have cached shadow maps for static objects, while allowing the shadow update for dynamic objects.
Because this feature will require additional memory cost, it needs to be enabled in the HDRP Asset of your project.

### Path Tracing

HDRP 13.0 you can use new APIs to check the accumulation progress and reset it.

The [SensorSDK package](https://docs.unity3d.com/Packages/com.unity.sensorsdk@1.0/manual/index.html) leverages the path tracer for its computations, in particular in Lidar mode, or to render images with specific camera lenses.

Orthographic views are now fully supported by the path tracer.

Last but not least, volumetric scattering now takes the fog color into account, adding scattered contribution on top of the non-scattered result. In addition, the sampling quality has been improved when dealing with multiple light sources.

### Custom Pass

Added new functions that sample the custom buffer in custom passes that deals with scaling automatically, it's recommended to use them over the standard texture sampling. Learn more about the **CustomPassSampleCustomColor** and **CustomPassLoadCustomColor** function in the [Creating a Custom Pass](Custom-Pass-Creating.md) documentation.
