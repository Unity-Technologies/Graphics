# Shader Stripping

The shaders in the Universal Render Pipeline (URP) use [shader keywords](https://docs.unity3d.com/Manual/shader-keywords) to support many different features, which can mean Unity compiles a lot of [shader variants](https://docs.unity3d.com/Manual/shader-variants).

If you disable features in the [URP Asset](universalrp-asset.md), URP automatically excludes ('strips') the related shader variants. This speeds up builds, and reduces memory usage and file sizes.

For example, if your project doesn't use shadows for directional lights, by default Unity still includes variants that support directional light shadows in your build. If you disable **Cast Shadows** in the URP Asset, URP strips these variants.

If you want to examine the code that strips shaders in URP, see the `Editor/ShaderPreprocessor.cs` file. The file uses the [IPreprocessShaders](https://docs.unity3d.com/ScriptReference/Build.IPreprocessShaders.html) API.

For more information on stripping shader variants, see the following pages:

- [Check how many shader variants you have](https://docs.unity3d.com/Manual/shader-how-many-variants.html).
- See the [standard guidance about shader stripping](https://docs.unity3d.com/Manual/shader-variant-stripping.html), which applies to all render pipelines.

## Strip feature shader variants

By default, URP compiles variants where a feature is enabled, and variants where a feature is disabled.

To reduce the number of variants, you can enable **Strip Unused Variants** in the [URP Global Settings](urp-global-settings.md) and do the following:

- Disable a feature in all URP Assets in your build, so URP keeps only variants where the feature is disabled.
- Enable a feature in all URP Assets in your build, so URP keeps only variants where the feature is enabled.

If you disable the **Strip Unused Variants setting**, URP can't strip variants where the feature is disabled. This might increase the number of variants.

### Disable a feature

To let Unity strip variants related to a feature, make sure you disable it in all the URP Assets in your build.

Unity includes the following URP Assets in your build:

- The URP Asset you set as the default render pipeline asset in [Graphics Settings](https://docs.unity3d.com/Manual/class-GraphicsSettings.html).
- Any URP Asset you set as a **Render Pipeline Asset** in a [Quality Settings level](https://docs.unity3d.com/Manual/class-QualitySettings.html) you enable for the current build target.

| **Feature** | **How to disable the feature** |
| :-- | :-- |
| Additional lights | In the [URP Asset](universalrp-asset.md), in the **Lighting** section, disable **Additional Lights**. |
| Ambient occlusion | Remove the [**Ambient Occlusion**](post-processing-ssao.md) Renderer Feature in all Renderers that URP Assets use. |
| Decals | Remove the [**Decals**](renderer-feature-decal.md) Renderer Feature in all Renderers that URP Assets use. |
| Reflection Probe blending | Disable [**Probe Blending**](lighting/reflection-probes.md). |
| Reflection Probe box projection | Disable [**Box Projection**](lighting/reflection-probes.md). |
| Render Pass | Disable **Native Render** in all Renderers that URP Assets use. |
| Light Layers | Disable [**Light Layers**](features/light-layers.md). |
| Shadows from additional lights | In the URP Asset, in the **Additional Lights** section, disable **Cast Shadows**. |
| Shadows from the main light | In the URP Asset, in the **Main Light** section, disable **Cast Shadows**. |

## Strip post-processing shader variants

Enable **Strip Unused Post Processing Variants** in [URP Global Settings](urp-global-settings.md) to strip shader variants for [Volume Overrides](VolumeOverrides.md) you don't use.

For example if your project uses only the Bloom effect, URP keeps Bloom variants but strips all other post-processing variants.

Unity checks for Volume Overrides in all scenes, so you can't strip variants by removing a Scene from your build but keeping it in your project.

You should also enable **Strip Screen Coord Override Variants** in URP Global Settings, unless you override screen coordinates to support post processing on large numbers of multiple displays ('cluster' displays).

## Strip XR and VR shader variants

If you don't use [XR](https://docs.unity3d.com/Manual/XR.html) or [VR](https://docs.unity3d.com/Manual/VROverview.html), you can [disable the XR and VR modules](https://docs.unity3d.com/Documentation/Manual/upm-ui.html). This allows URP to strip XR and VR related shader variants from its standard shaders.
