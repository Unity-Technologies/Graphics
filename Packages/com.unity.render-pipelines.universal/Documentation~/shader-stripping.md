# Shader Stripping

The shaders in the Universal Render Pipeline (URP) use [shader keywords](https://docs.unity3d.com/Manual/shader-keywords) to support many different features, which can mean Unity compiles a lot of [shader variants](https://docs.unity3d.com/Manual/shader-variants).

If you disable features in the [URP Asset](universalrp-asset.md), URP automatically excludes ('strips') the related shader variants. This speeds up builds, and reduces memory usage and file sizes.

For example, if your project doesn't use shadows for directional lights, by default Unity still includes variants that support directional light shadows in your build. If you disable **Cast Shadows** in the URP Asset, URP strips these variants.

If you want to examine the code that strips shaders in URP, check the `Editor/ShaderPreprocessor.cs` file. The file uses the [IPreprocessShaders](https://docs.unity3d.com/ScriptReference/Build.IPreprocessShaders.html) API.

For more information on stripping shader variants, refer to the following pages:

* [Check how many shader variants you have](https://docs.unity3d.com/Manual/shader-how-many-variants.html).
* [General guidance on shader stripping](https://docs.unity3d.com/Manual/shader-variant-stripping.html), which applies to all render pipelines.

## Strip feature shader variants

By default, URP compiles variants where a feature is enabled, and variants where a feature is disabled.

To reduce the number of variants, you can enable **Strip Unused Variants** in [URP Graphics settings](urp-global-settings.md) and do the following:

* Disable a feature in all URP Assets in your build, so URP keeps only variants where the feature is disabled.
* Enable a feature in all URP Assets in your build, so URP keeps only variants where the feature is enabled.

If you disable the **Strip Unused Variants setting**, URP can't strip variants where the feature is disabled. This might increase the number of variants.

### Disable a feature

To let Unity strip variants related to a feature, make sure you disable it in all the URP Assets in your build.

Unity includes the following URP Assets in your build:

* The URP Asset you set as the default render pipeline asset in [Graphics settings](https://docs.unity3d.com/Manual/class-GraphicsSettings.html).
* Any URP Asset you set as a **Render Pipeline Asset** in a [Quality settings level](https://docs.unity3d.com/Manual/class-QualitySettings.html) you enable for the current build target.

Avoid including URP Assets in your build that use different [rendering paths](urp-universal-renderer.md#rendering-path-comparison) because this causes Unity to create two sets of variants for each keyword.

| **Feature** | **How to disable the feature** | **Shader keywords this turns off** | **Rendering Path** |
| - | - | - | - |
| Accurate G-buffer normals | Disable **Accurate G-buffer normals** in the URP Asset. This has no effect on platforms that use the Vulkan graphics API. | `_GBUFFER_NORMALS_OCT` | Deferred |
| Additional lights | In the **URP Asset**, in the **Lighting section**, disable **Additional Lights**. | `_ADDITIONAL_LIGHTS`, `_ADDITIONAL_LIGHTS_VERTEX` | Forward |
| Ambient occlusion | Remove the [Ambient Occlusion](post-processing-ssao.md) Renderer Feature in all Renderers that URP Assets use. | `_SCREEN_SPACE_OCCLUSION` | Forward and Deferred |
| Decals | Remove the [Decals](renderer-feature-decal.md) Renderer Feature in all Renderers that URP Assets use. |  `_DBUFFER_MRT1`, `_DBUFFER_MRT2`, `_DBUFFER_MRT3`, `_DECAL_NORMAL_BLEND_LOW`, `_DECAL_NORMAL_BLEND_MEDIUM`, `_DECAL_NORMAL_BLEND_HIGH`,  `_DECAL_LAYERS` | Forward and Deferred |
| Fast sRGB to linear conversion | In the **URP Asset**, in the **Post-processing** section, disable **Fast sRGB/Linear conversions**. | `_USE_FAST_SRGB_LINEAR_CONVERSION` | Forward and Deferred |
| Holes in terrain | In the **URP Asset**, in the **Rendering** section, disable **Terrain Holes**. | `_ALPHATEST_ON` | Forward |
| Light cookies | Remove [Cookie textures](https://docs.unity3d.com/Manual/Cookies.html) from all the lights in your project. | `_LIGHT_COOKIES` | Forward and Deferred |
| Rendering Layers for lights | Disable [Rendering Layers for Lights](features/rendering-layers.md). | `_LIGHT_LAYERS` | Forward and Deferred |
| Reflection Probe blending | Disable [Probe Blending](lighting/reflection-probes.md#configuring-reflection-probe-settings). | `_REFLECTION_PROBE_BLENDING` | Forward and Deferred |
| Reflection Probe box projection | Disable [Box Projection](lighting/reflection-probes.md#configuring-reflection-probe-settings). | `_REFLECTION_PROBE_BOX_PROJECTION` | Forward and Deferred |
| Render Pass | Disable **Native Render** in all Renderers that URP Assets use. | `_RENDER_PASS_ENABLED` | Forward and Deferred |
| Shadows from additional lights | In the URP Asset, in the **Additional Lights** section, disable **Cast Shadows**. | `_ADDITIONAL_LIGHT_SHADOWS` | Forward and Deferred |
| Shadows from the main light | In the URP Asset, in the **Main Light** section, disable **Cast Shadows**. The keywords Unity removes might depend on your settings. | `_MAIN_LIGHT_SHADOWS`, `_MAIN_LIGHT_SHADOWS_CASCADE`, `_MAIN_LIGHT_SHADOWS_SCREEN` | Forward and Deferred |
| Soft shadows | In the URP Asset, in the **Shadows** section, disable **Soft shadows**. | `_SHADOWS_SOFT` | Forward and Deferred |

## Strip post-processing shader variants

Enable **Strip Unused Post Processing Variants** in [URP Graphics settings](urp-global-settings.md) to strip shader variants for [Volume Overrides](VolumeOverrides.md) you don't use.

For example if your project uses only the Bloom effect, URP keeps Bloom variants but strips all other post-processing variants.

Unity checks for Volume Overrides in all scenes, so you can't strip variants by removing a scene from your build but keeping it in your project.

| **Volume Override removed** | **Shader keywords this turns off** |
| - | - |
| Bloom | `_BLOOM_HQ`, `BLOOM_HQ_DIRT`, `_BLOOM_LQ`, `BLOOM_LQ_DIRT` |
| Chromatic Aberration | `_CHROMATIC_ABERRATION` |
| Film Grain | `_FILM_GRAIN` |
| HDR Grading | `_HDR_GRADING` |
| Lens Distortion | `_DISTORTION` |
| Tonemapping | `_TONEMAP_ACES`, `_TONEMAP_NEUTRAL`, `_TONEMAP_GRADING` |

You should also enable **Strip Screen Coord Override Variants** in URP Graphics settings, unless you override screen coordinates to support post processing on large numbers of multiple displays ('cluster' displays).

## Strip XR and VR shader variants

If you don't use [XR](https://docs.unity3d.com/Manual/XR.html) or [VR](https://docs.unity3d.com/Manual/VROverview.html), you can [disable the XR and VR modules](https://docs.unity3d.com/Documentation/Manual/upm-ui.html). This allows URP to strip XR and VR related shader variants from its standard shaders.

## Remove variants if you use a custom Renderer Feature

If you create a [custom Renderer Feature](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature), you can use the [FilterAttribute](https://docs.unity3d.com/2023.1/Documentation/ScriptReference/ShaderKeywordFilter.FilterAttribute.html) API to remove shader variants when you enable or disable settings in the [URP Asset](universalrp-asset.md).

For example, you can do the following:

1. Use [[SerializeField]](https://docs.unity3d.com/ScriptReference/SerializeField.html) to add a Boolean variable to the custom Renderer Feature and add it as a checkbox in the URP Asset Inspector.
2. Use [ShaderKeywordFilter.RemoveIf](https://docs.unity3d.com/2023.1/Documentation/ScriptReference/ShaderKeywordFilter.RemoveIfAttribute.html) to remove shader variants when you enable the checkbox.
