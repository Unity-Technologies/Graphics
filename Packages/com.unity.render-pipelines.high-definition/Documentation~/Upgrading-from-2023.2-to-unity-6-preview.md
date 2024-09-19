# Upgrading HDRP from 2023.2 to Unity 6 Preview

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 16.x to 17.x.

## Materials

HDRP 17.x makes the following changes to Materials:

- When using path tracing, the effect of the **ambient occlusion** channel of materials with a mask map has been slightly modified. This was done to ensure that both rectangular area lights and rectangular meshes with emissive materials have the same lighting effect under ambient occlusion. As a consequence, the visual appearance of **ambient occlusion** has changed slightly.

In case you were using the internal enums `UnityEditor.Rendering.HighDefinition.TransparentCullMode` and `UnityEditor.Rendering.HighDefinition.OpaqueCullMode` in a Material GUI, please replace them by `UnityEngine.Rendering.HighDefinition.TransparentCullMode` and `UnityEngine.Rendering.HighDefinition.OpaqueCullMode` respectively.

## Reflection Probes and Planar Reflection Probes

**Reflection Probes** and **Planar Reflection Probes** now have an **Importance** setting to better sort them. The default value is 1 for **Reflection Probes** and 64 for **Planar Reflection Probes** so **Planar Reflection Probes** are displayed on top of **Reflection Probes**, as they are most of the time more accurate, while still allowing to sort **Reflection Probes** without interfering with **Planar Reflection Probes** until a certain point.

## Adaptive Probe Volume

With the introduction of the sky occlusion feature some asset data layout has changed. Previously baked data for adaptive probe volume will need to be rebaked.

## Path tracing noise pattern

Path tracing now has a *Seed Mode* parameter. The default is the **non repeating** noise pattern, which is different from the previous behavior. To match behavior in the last version, select the **repeating** pattern.

The [Raytracing Quality Keyword](SGNode-Raytracing-Quality.md) has been updated to include a Pathtraced input. 
In previous versions, when Path tracing is enabled, the default input was used. Now, it uses the Pathtraced input. This is to prevent compilation error in graphs using unsupported nodes. 
If you had Shader Graph materials using the Raytracing Quality Keyword, the result will stay unchanged until you re-save them. To upgrade the behavior, you need to delete the keyword in the blackboard and re-add it manually. 

## Enabling light sources in Path Tracing

In this version, the setting to include light sources in ray traced effects has been split in one checkbox for hybrid ray tracing effects (`include for Ray Tracing`) and one checkbox for inclusion in Path Tracing (`include for Path Tracing`). When upgrading, this last checkbox might need to be updated.

## Deferred lighting using pixel shader

The pixel shader variant of the deferred lighting pass was removed, the lighting is now always computed with a compute shader.
The framesetting to enable this option was deleted.

## Physically Based Sky

The Shader Config option `PrecomputedAtmosphericAttenuation` is now enabled by default. This optimizes the rendering on GPU by computing the directional light atmospheric attenuation on the CPU.

Note that this change will result in a loss of precision for the attenuation value, if your game have a very large scale (like space games) it may be good to disable this option.

To disable `PrecomputedAtmosphericAttenuation`, first you need to install the HDRP config package which can be done using the [Render Pipeline Wizard](Render-Pipeline-Wizard.md). For more info, see [HDRP Config](configure-a-project-using-the-hdrp-config-package.md).
Once installed, go in ShaderConfig.cs and set `PrecomputedAtmosphericAttenuation` to 0.

## Physically Based Depth Of Field

We improved the performances of the PBR DoF and removed the parameter "High Quality Filtering" as it was too costly to be used in a reasonable scenario. The replacement of this option is the resolution dropdown which allows to use full resolution physically based depth of field whereas before it was maxed at half resolution. This allows for more precise depth of field and less artifacts but it's still very costly.
The PBR DoF now also take in account the aperture shape defined in the physical camera settings (blade count, etc.)
