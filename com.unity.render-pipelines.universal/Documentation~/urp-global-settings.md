# URP Global Settings

If a project has the URP package installed, Unity shows the URP Global Settings section in the Graphics tab in the Project Settings window.

The URP Global Settings section lets you define project-wide settings for URP.

![URP Settings Window](Images/Inspectors/global-settings.png)

The section contains the following settings.

## Light Layer Names (3D)

Use this section to define the names of Light Layers. Light Layers only work with 3D Renderers.

## Shader Stripping

The check boxes in this section define which shader variants Unity strips when you build the Player.

| **Property**              | **Description**                                              |
| --------------------------| ------------------------------------------------------------ |
| Strip Debug Variants     | When enabled, Unity strips all debug view shader variants when you build the Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.  |
| Strip Unused Post Processing Variants    | When enabled, Unity assumes that the Player does not create new [Volume Profiles](VolumeProfile.md) at runtime. With this assumption, Unity only keeps the shader variants that the existing [Volume Profiles](VolumeProfile.md) use, and strips all the other variants. Unity keeps shader variants used in Volume Profiles even if the Scenes in the project do not use the Profiles. |
| Strip Unused Variants        | When enabled, Unity performs shader stripping in a more efficient way. This option reduces the amount of shader variants in the Player by a factor of 2 if the project uses the following URP features:<ul><li>Light Layers</li><li>Native Render Pass</li><li>Reflection Probe Blending</li><li>Reflection Probe Box Projection</li><li>SSAO Renderer Feature</li><li>Decal Renderer Feature</li><li>Certain post-processing effects</li></ul>Disable this option only if you see issues in the Player. |
