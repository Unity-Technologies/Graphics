# Graphics settings window reference in URP 

If a project has the Universal Render Pipeline (URP) package installed, Unity shows URP-specific graphics settings in **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **URP**.

The section contains the following settings that let you define project-wide settings for URP.

You can also add your own settings. Refer to [Add custom settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/manual/add-custom-graphics-settings.md) in the Scriptable Render Pipeline (SRP) Core manual for more information.

## Shader Stripping

The check boxes in this section define which shader variants Unity strips when you build the Player.

| **Property**              | **Description**                                              |
| --------------------------| ------------------------------------------------------------ |
| **Shader Variant Log Level**  | Select what information about Shader variants  Unity saves in logs when you build your Unity Project.<br/>Options:<br/>• Disabled: Unity doesn't save any shader variant information.<br/>• Only SRP Shaders: Unity saves only shader variant information for URP shaders.<br/>• All Shaders: Unity saves shader variant information for every shader type. |
| **Strip Debug Variants** | When enabled, Unity strips all debug view shader variants when you build the Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.  |
| **Strip Unused Post Processing Variants** | When enabled, Unity assumes that the Player does not create new [Volume Profiles](Volume-Profile.md) at runtime. With this assumption, Unity only keeps the shader variants that the existing [Volume Profiles](Volume-Profile.md) use, and strips all the other variants. Unity keeps shader variants used in Volume Profiles even if the scenes in the project do not use the Profiles. |
| **Strip Unused Variants** | When enabled, Unity performs shader stripping in a more efficient way. This option reduces the amount of shader variants in the Player by a factor of 2 if the project uses the following URP features:<ul><li>Rendering Layers</li><li>Native Render Pass</li><li>Reflection Probe Blending</li><li>Reflection Probe Box Projection</li><li>SSAO Renderer Feature</li><li>Decal Renderer Feature</li><li>Certain post-processing effects</li></ul>Disable this option only if you notice issues in the Player. |
| **Strip Screen Coord Override Variants** | When enabled, Unity strips Screen Coordinates Override shader variants in Player builds. |

## Default Volume Profile

Use this section to assign and edit a [Volume Profile](Volume-Profile.md) for the Default Volume that all scenes use. Refer to [Understand Volumes](volumes.md) for more information.

| **Property**              | **Description**                                              |
| --------------------------| ------------------------------------------------------------ |
| **Volume Profile** | Set the [Volume Profile](Volume-Profile.md) the global Default Volume uses. You can't set **Volume Profile** to **None**. |

URP displays all the properties for all the possible [Volume Overrides](VolumeOverrides.md). You can edit the properties, or override the properties using [the global volume for a quality level](set-up-a-volume.md#configure-the-global-volume-for-a-quality-level) or by [creating a volume](set-up-a-volume.md#add-a-volume).
