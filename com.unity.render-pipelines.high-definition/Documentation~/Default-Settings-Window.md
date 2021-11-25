# HDRP Global Settings Window

The High Definition Render Pipeline (HDRP) adds the HDRP Settings tab to Unity's Graphics Settings window. You can use this tab to set up default settings for certain features in your Project. You can:

- Assign Render Pipeline Resources Assets for your HDRP Project.
- Set the verboseness of Shader variant information that Unity writes to the Console window when you build your Project.
- Set up default [Frame Setting](Frame-Settings.md) values for [Cameras](HDRP-Camera.md) to use.
- Assign and edit a default [Volume Profile](Volume-Profile.md).

The HDRP Settings tab is part of the Graphics Settings window. To get to this tab, select **Edit > Project Settings > Graphics** and then, in the sidebar, click **HDRP Settings**.

## Volume Profiles

You can use this section to assign and edit a [Volume Profile](Volume-Profile.md) that [Volumes](Volumes.md) use by default in your Scenes. You do not need to create a Volume for this specific Volume Profile to be active, because HDRP always processes it as if it is assigned to a global Volume in the Scene, but with the lowest priority. This means that any Volume that you add to a Scene takes priority.

The Default Volume Profile Asset references a Volume Profile in the HDRP Package folder called DefaultSettingsVolumeProfile by default. Below it, you can add [Volume overrides](Volume-Components.md), and edit their properties. You can also assign your own Volume Profile to this property field. Be aware that this property must always reference a Volume Profile. If you assign your own Volume Profile and then delete it, HDRP automatically re-assigns the DefaultSettingsVolumeProfile from the HDRP Package folder.

The LookDev Volume Profile Asset references the Volume Profile HDRP uses in the [LookDev window](Look-Dev.md). This Asset works in almost the same way as the Default Volume Profile Asset, except that it overrides [Visual Environment Components](Override-Visual-Environment.md) and sky components.

## Diffusion Profile Assets

Use this section to select which custom [Diffusion Profiles](Diffusion-Profile.md) can be in view at the same time. To use more than 15 custom Diffusion Profiles in a Scene, use the [Diffusion Profile Override](Override-Diffusion-Profile.md) inside a Volume. This allows you to specify which Diffusion Profiles to use in a certain area (or in the Scene if the Volume is global).

## Frame Settings

The [Frame Settings](Frame-Settings.md) control the rendering passes that Cameras make at runtime. Use this section to set default values for the Frame Settings that all Cameras use if you do not enable their Custom Frame Settings checkbox. For information about what each property does, see [Frame Settings](Frame-Settings.md).

## Layers Names

| **Property**              | **Description**                                              |
| --------------------------| ------------------------------------------------------------ |
| Light Layer Names                     | The name displayed on Lights and Meshes when using [Light Layers](Light-Layers.md). |
| Decal Layer Names                     | The name displayed on decals and Meshes when using [Decal Layers](Decal.md). |

## Custom Post Process Orders

Use this section to select which custom post processing effect will be used in the project and in which order they will be executed.
You have one list per post processing injection point: `After Opaque And Sky`, `Before Post Process` and `After Post Process`. See the [Custom Post Process](Custom-Post-Process.md) documentation for more details.

## Miscellaneous

| **Property**              | **Description**                                              |
| --------------------------| ------------------------------------------------------------ |
| Shader Variant Log Level              | Use the drop-down to select what information HDRP logs about Shader variants when you build your Unity Project. • Disabled: HDRP doesn’t log any Shader variant information.• Only HDRP Shaders: Only log Shader variant information for HDRP Shaders.• All Shaders: Log Shader variant information for every Shader type. |
| Lens Attenuation Mode                 | Set the attenuation mode of the lens that is used to compute exposure. With imperfect lens some energy is lost when converting from EV100 to the exposure multiplier, while a perfect lens has no attenuation and no energy is lost. |
| Dynamic Render Pass Culling           | When this option is enabled, HDRP will use the RendererList API to dynamically skip certain drawing passes based on the type of currently visible objects. For example if no objects with distortion are drawn, the Render Graph passes that draw the distortion effect (and their dependencies - like the color pyramid generation) will be skipped.
| Use DLSS Custom Project Id            | Controls whether to use a custom project ID for the NVIDIA Deep Learning Super Sampling module. If you enable this property, you can use **DLSS Custom Project Id** to specify a custom project ID.<br/>This property only appears if you enable the NVIDIA package (com.unity.modules.nvidia) in your Unity project. |
| DLSS Custom Project Id                | Controls whether to use a custom project ID for the NVIDIA Deep Learning Super Sampling (DLSS) module. If you enable this property, you can use **DLSS Custom Project Id** to specify a custom project ID. If you disable this property, Unity generates a unique project ID. <br/>This property only appears if you enable the NVIDIA package (com.unity.modules.nvidia) in your Unity project. |
| Runtime Debug Shaders                 | When enabled, Unity includes shader variants that let you use the Rendering Debugger window to debug your build. When disabled, Unity excludes ("strips") these variants. Enable this when you want to debug your shaders in the Rendering Debugger window, and disable it otherwise. |

## Resources

The Resources list includes the Shaders, Materials, Textures, and other Assets that the High Definition Render Pipeline (HDRP) uses.

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Player Resources**      | Stores references to Shaders and Materials that HDRP uses. When you build your Unity Project, HDRP embeds all of the resources that this Asset references. It allows you to set up multiple render pipelines in a Unity Project and, when you build the Project, Unity only embeds Shaders and Materials relevant for that pipeline. This is the Scriptable Render Pipeline equivalent of Unity’s Resources folder mechanism. When you create a new HDRP Global Settings Asset, the HDRenderPipelineRuntimeResources from HDRP package is automatically referenced in it. |
| **Ray Tracing Resources** | Stores references to Shaders and Materials that HDRP uses for ray tracing. HDRP stores these resources in a separate Asset file from the main pipeline resources so that it can use less memory for applications that don't support ray tracing. When you create a new HDRP Global Settings Asset, the HDRenderPipelineRayTracingResources from HDRP package is automatically referenced in it if your project use ray tracing. |
| **Editor Resources**      | Stores reference resources for the Editor only. Unity does not include these when you build your Unity Project. When you create a new HDRP Global Settings Asset, the HDRenderPipelineEditorResources from HDRP package is automatically referenced in it. |
