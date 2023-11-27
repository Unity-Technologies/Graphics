# Find graphics quality settings in URP

URP splits its quality settings between Project Settings and the URP Asset to allow for more versatility in the quality levels your project has. As a result, some settings that Built-In Render Pipeline (BiRP) listed in the Project Settings **Quality** section have moved or changed, or no longer exist.

The following table describes all the settings that the Built-in Renderer lists in the Project Settings **Quality** section, and where that setting is now located within URP.

| **BiRP Setting** | **URP Setting** |
| ---------------- | --------------- |
| **Rendering** | |
| Render Pipeline Asset | **Project Settings** > **Quality** > **Rendering** > **Render Pipeline Asset** |
| Pixel Light Count | In URP, the maximum number of real-time lights per object depends on the render path in use. For more information, refer to [Rendering Path comparison](./../urp-universal-renderer.md#rendering-path-comparison).<br/><br/>To set the light count per object, use the following property: **URP Asset** > **Lighting** > **Additional Lights** > **Per Pixel** > **Per Object Limit** |
| Anti-aliasing | There are two types of anti-aliasing in URP: you control Multisample Anti-aliasing (MSAA) in the URP Asset, and other anti-aliasing types on a per camera basis. For more information refer to [Anti-aliasing in the Universal Render Pipeline](./../anti-aliasing.md).<br/><br/>To control MSAA, use the following property:<br/><br/>**URP Asset** > **Quality** > **Anti-aliasing (MSAA)**<br/><br/>To control any other type of anti-aliasing, use the following property on a per camera basis:<br/><br/>**Camera** > **Rendering** > **Anti-aliasing** |
| Real-time Reflection Probes | **Project Settings** > **Quality** > **Rendering** > **Real-time Reflection Probes** |
| Resolution Scaling Fixed DPI Factor | This property remains in the same place in URP. However, URP also supports the use of Upscalers to handle resolution scaling in the URP Asset. For more information on the use of upscalers, refer to [Quality in the URP Asset](./../universalrp-asset.md#quality).<br/><br/>To set Resolution Scaling Fixed DPI Factor, use the following property:<br/><br/>**Project Settings** > **Quality** > **Rendering** > **Resolution Scaling Fixed DPI Factor**<br/><br/>To set resolution scaling in the URP Asset, use the following property:<br/><br/>**URP Asset** > **Quality** > **Render Scale** and **Upscaling Filter** |
| VSync Count | **Project Settings** > **Quality** > **Rendering** > **VSync Count** |
| **Textures** | |
| Global Mipmap Limit | **Project Settings** > **Quality** > **Textures** > **Global Mipmap Limit** |
| Anisotropic Textures | **Project Settings** > **Quality** > **Textures** > **Anisotropic Textures** |
| Texture Streaming | **Project Settings** > **Quality** > **Textures** > **Texture Streaming** |
| **Particles** | |
| Soft Particles | To enable soft particles use the shader keyword `_SOFTPARTICLES_ON` inside the relevant particle shaders. |
| Particle Raycast Budget | **Project Settings** > **Quality** > **Particles** > **Particle Raycast Budget** |
| **Terrain** | |
| Billboards Face Camera Position | **Project Settings** > **Quality** > **Terrain** > **Billboards Face Camera Position** |
| Use Legacy Details Distribution | **Project Settings** > **Quality** > **Terrain** > **Use Legacy Details Distribution** |
| **Shadows** | |
| Shadowmask Mode | **Project Settings** > **Quality** > **Shadows** > **Shadowmask Mode** |
| Shadows | In URP you can enable shadows from two types of light separately. One is the Main Light of a scene, and the other is all other Additional Lights. To do this, set following properties as necessary:<br/><br/>To enable shadows cast by the Main Light, use the following property:<br/><br/>**URP Asset** > **Lighting** > **Main Light** > **Cast Shadows**<br/><br/>To enable shadows cast by Additional Lights, use the following property:<br/><br/>**URP Asset** > **Lighting** > **Additional Lights** > **Cast Shadows**<br/><br/>**Note**: You no longer select the type of Shadows when you enable shadows. Instead to use Soft Shadows, enable **URP Asset** > **Shadows** > **Soft Shadows** and select an appropriate quality level. |
| Shadow Resolution | You can set Shadow Resolution separately for the Main Light and Additional Lights. Additional Lights use a Shadow Atlas with three tiers: Low, Medium, and High.<br/><br/>To set the shadow resolution for the Main Light, use the following property:<br/><br/>**URP Asset** > **Lighting** > **Main Light** > **Shadow Resolution**<br/><br/>To set the shadow resolution for Additional Lights, use the following properties:<br/><br/>**URP Asset** > **Lighting** > **Additional Lights** > **Shadow Atlas Resolution** and **Shadow Resolution Tiers** |
| Shadow Projection | URP only supports Stable Fit Shadow Projection. |
| Shadow Distance | **URP Asset** > **Shadows** > **Max Distance** |
| Shadow Near Plane Offset | No equivalent setting, because URP's shadow system doesn't use this property. |
| Shadow Cascades | **URP Asset** > **Shadows** > **Cascade Count** |
| Cascade Splits | Shadow Cascade Splits are now controlled by a dynamic set of properties based on the Cascade Count. The URP Asset displays a visual representation of the Cascade Splits below the Split values as a bar with multiple segments, with each segment representing the size of a given split.<br/><br/>You can control the size of each Shadow Cascade Split with the following properties:<br/><br/>**URP Asset** > **Shadows** > **Cascade Count** > **Split 1**, **Split 2**, **Split 3**, and **Last Border** |
| **Async Asset Upload** | |
| Time Slice | **Project Settings** > **Quality** > **Async Asset Upload** > **Time Slice** |
| Buffer Size | **Project Settings** > **Quality** > **Async Asset Upload** > **Buffer Size** |
| Persistent Buffer | **Project Settings** > **Quality** > **Async Asset Upload** > **Persistent Buffer** |
| **Level of Detail** | |
| LOD Bias | **Project Settings** > **Quality** > **Level of Detail** > **LOD Bias** |
| Maximum LOD level | **Project Settings** > **Quality** > **Level of Detail** > **Maximum LOD Level** |
| LOD Cross Fade | **URP Asset** > **Quality** > **LOD Cross Fade**<br/><br/>**Note**: URP offers two options for LOD Cross Fade: Bayer and Blue Noise. These are both different to Built-In's use of Dither.  |
| **Meshes** | |
| Skin Weights | **Project Settings** > **Quality** > **Meshes** > **Skin Weights** |

## Additional resources

* [URP Quality Presets](./quality-presets.md)
* [URP Asset](./../universalrp-asset.md)
* [Shadows in URP](./../Shadows-in-URP.md)
