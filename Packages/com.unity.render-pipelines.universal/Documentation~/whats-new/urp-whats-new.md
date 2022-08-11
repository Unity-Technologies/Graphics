# What's new in URP 13 (Unity 2022.1)

This section contains information about new features, improvements, and issues fixed in URP 13 (Unity 2022.1).

For a complete list of changes made in URP 13, refer to the [Changelog](../../changelog/CHANGELOG.html).

## Improvements

This section contains the overview of the major improvements in this release.

### Implemented RTHandle system support for URP

This version of URP implements the support for the RTHandle system. The RTHandle system is an abstraction on top of Unity's [RenderTexture](https://docs.unity3d.com/ScriptReference/RenderTexture.html) API. The system lets you reuse render textures across Cameras that use various resolutions.

For more information on the RTHandle system, see the page [RTHandle system fundamentals](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@/index.html?subfolder=/manual/rthandle-system-fundamentals.html).

### Implemented the Depth Texture property for overlay Cameras

Overlay Cameras now have the **Depth Texture** property like base Cameras.

### Shader stripping improvements

This URP version contains improvements to shader stripping. New shader stripping options in **URP Global Settings**:

* Main Light Shadows.

* Additional Light Shadows.

* Additional Lights.

* Strip Unused Post-processing Variants.

### Display Stats panel in the Rendering Debugger

The [Display Stats](../features/rendering-debugger.md#display-stats) panel lets you troubleshoot performance issues in your project.

### Added the Preserve Specular Lighting property

The **Alpha** and the **Additive** blending modes now have the **Preserve Specular Lighting** property. For more information, see the **Blending Mode** property description on the [Lit shader](../lit-shader.md#surface-options) page.

## Issues resolved

For a complete list of issues resolved in this version of URP, see the [Changelog](../../changelog/CHANGELOG.html).

## Known issues

For information on the known issues in this version of URP, see the section [Known issues](../known-issues.md).
