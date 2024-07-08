# Reduce shader variants

The standard shaders in the High Definition Render Pipeline (HDRP) support a lot of different features, which can mean Unity compiles a lot of shader variants. To avoid your build growing too big, HDRP automatically excludes ('strips') shader variants for features you don't use in your build.

You can change settings to make Unity strip more variants. This speeds up builds, and reduces memory usage and file sizes.

If you want to examine the code that strips shaders in HDRP, see the following files:

- `Editor/Material/Lit/LitShaderPreprocessor.cs`
- `Editor/Material/BaseShaderPreprocessor.cs`
- `Editor/BuildProcessors/HDRPPreprocessShaders.cs`

The files use the [IPreprocessShaders](https://docs.unity3d.com/ScriptReference/Build.IPreprocessShaders.html) API.

## Check how many shader variants your build has

To log how many variants Unity compiles and strips in total, follow these steps:

1. Open the [Graphics settings window](Default-Settings-Window.md).
2. In the **Additional Shader Stripping Settings** section, select a logging level other than **Disabled**.
3. Build your project.
4. To see the logged information, open the `Editor.log` log file and search for `ShaderStrippingReport`. For the location of `Editor.log`, refer to [log files](xref:LogFiles).

To log more detailed shader variant information, follow these steps:

1. Open the [Graphics settings window](Default-Settings-Window.md).
2. In the **Additional Shader Stripping Settings** section, select **Export Shader Variants**.
3. Build your project.
4. In the folder with your project files, open `Temp/graphics-settings-stripping.json` and `Temp/shader-stripping.json`. 

For more information, refer to the following in the Unity User Manual:

- [Check how many shader variants you have](xref:shader-how-many-variants)
- [Shader variant stripping](xref:shader-variant-stripping)

## Strip feature shader variants

If you disable a feature, HDRP strips any shader variants where the feature is enabled.

You must disable the feature in all the HDRP assets in your build. Unity includes in your build any HDRP asset you set as a **Render Pipeline Asset** in a [Quality Settings level](https://docs.unity3d.com/Manual/class-QualitySettings.html).

| **Feature** | **How to disable the feature** |
| :-- | :-- |
| Built-in fog | In the [Graphics settings window](Default-Settings-Window.md), in the **Shader Stripping** section, set **Fog Modes** to **Custom**, then disable **Linear**, **Exponential**, **Exponential Squared**. This strips built-in fog shaders that HDRP doesn't use. |
| Cameras generate additional [Arbitrary Output Variables (AOV)](AOVs.md) images | In the [HDRP asset](HDRP-Asset.md), in the **Rendering** section, disable **Runtime AOV API**. |
| Cameras use both Deferred and Forward rendering | In the HDRP asset, in the **Rendering** section, set **Lit Shader Mode** to **Deferred**. This creates fewer variants than **Forward** or **Both**. |
| Decals | In the HDRP asset, disable **Decals**. |
| Distortion | In the HDRP asset, in the **Material** section, disable **Distortion**. |
| GPU instancing variants you don't use | In the [Graphics settings window](Default-Settings-Window.md), in the **Shader Stripping** section, set **Instancing Variants** to **Strip Unused**. |
| Holes in Terrain | In the HDRP asset, in the **Rendering** section, disable **Terrain Holes**. |
| Lightmaps HDRP doesn't use | In the [Graphics settings window](Default-Settings-Window.md), in the **Shader Stripping** section, set **Lightmap Modes** to **Custom**, and enable only the **Baked Directional** mode. This strips lightmap shader variants that HDRP doesn't use. |
| Material Quality in Shader Graph shaders | In the HDRP asset, in the **Material** section, disable any **Available Material Quality** levels you don't need. This only has an effect if you use the [Material Quality Node](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@15.0/manual/Scalability-Manual.html) in Shader Graph. |
| Motion vectors | In the HDRP asset, in the **Rendering** section, disable **Motion Vectors**. You shouldn't disable this unless your Scenes are fully static with no deformation. |
| Realtime raytracing | You can do one of the following in the HDRP asset, in the **Rendering** section:<ul><li>Disable **Realtime Raytracing**.</li><li>Enable **Realtime Raytracing**, but set **Supported Ray Tracing Mode** to **Performance**.</li></ul> Performance mode doesn't support path tracing. |
| Rendering Layers | In the HDRP asset, in the **Lighting** section, disable **Light Layers**. This only has an effect if you also set **Lit Shader Mode** to **Deferred**. |
| Subsurface scattering | In the HDRP asset, in the **Material** section, disable **Subsurface Scattering**. This only removes a small number of variants, so you should only disable this if you need to. |
| Transitions between GameObject level of detail (LOD) levels | In the HDRP asset, in the **Rendering** section, disable **Dithering Cross-fade**. |
| Transparent back-face render passes | In the HDRP asset, in the **Rendering** section, disable **Transparent Backface**. Unity might incorrectly render transparent objects. |
| Transparent depth render postpasses | In the HDRP asset, in the **Rendering** section, disable **Transparent Depth Postpass**. Unity might incorrectly render transparent objects. |
| Transparent depth render prepasses | In the HDRP asset, in the **Rendering** section, disable **Transparent Depth Prepass**. Unity might incorrectly render transparent objects and screen space reflections. |

## Strip XR and VR shader variants

If you don't use XR or VR, you can [disable the XR and VR modules](https://docs.unity3d.com/Documentation/Manual/upm-ui.html). This allows HDRP to strip XR and VR related shader variants from its standard shaders.

## Strip debug shader variants

If you don't need to use the [Rendering Debugger](use-the-rendering-debugger.md) in a development build, you can disable **Runtime Debug Shaders** under **Miscellaneous** in the [Graphics settings window](Default-Settings-Window.md). This strips any debug shader variants that the Rendering Debugger uses.

You don't need to do this if you disable **Development Build** in your [Build Settings](https://docs.unity3d.com/Manual/BuildSettings.html).

## Features that affect build time but not variants

If you disable the following features, Unity doesn't reduce the number of variants but your build time will be faster.

| **Feature** | **How to disable the feature** |
| :-- | :-- |
| Decal layers | In the HDRP asset, in the **Decals** section, disable **Layers**. |
| High-quality area shadows | In the HDRP asset, in the **Lighting** section, set **Area Shadow Filtering Quality** to **Low**. |
| High-quality shadows | In the HDRP asset, in the **Lighting** section, set **Shadow Filtering Quality** to **Low**. |
| Light Probe system | In the HDRP asset, in the **Lighting** section, set **Light Probe System** to **Light Probe Groups (Legacy)**. |
| Shadowmasks | In the HDRP asset, in the **Lighting** section, disable **Shadowmask**. |
