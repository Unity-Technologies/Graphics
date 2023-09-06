
# Optimize for better performance

If the performance of your project seems slow, you can adjust settings in the [Univeral Render Pipeline (URP) Asset](universalrp-asset.md) or the [Universal Renderer asset](urp-universal-renderer.md) that have an effect on performance.

Depending on your project or the platforms you target, some settings might not have a big effect. You can use the [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html) or a GPU profiler such as [RenderDoc](https://docs.unity3d.com/Manual/RenderDocIntegration.html) or [Xcode](https://docs.unity3d.com/Manual/XcodeFrameDebuggerIntegration.html) to measure the effect of each setting on the performance of your project.


| **Setting** | **Where the setting is** | **What to do for better performance** |
| ------------------------------------------- | ------------------------------ | --------------------------------------------------------------------------- |
| **Accurate G-buffer normals** | [Universal Renderer](urp-universal-renderer.md) > **Rendering** | Disable if you use the Deferred rendering path |
| **Additional Lights** > **Cast Shadows** | [URP Asset](universalrp-asset.md) > **Lighting** | Disable |
| **Additional Lights** > **Cookie Atlas Format** | URP Asset > **Lighting** | Set to **Color Low** |
| **Additional Lights** > **Cookie Atlas Resolution** | URP Asset > **Lighting** | Set to the lowest you can accept |
| **Additional Lights** > **Per Object Limit** | URP Asset > **Lighting** | Set to the lowest you can accept. This setting has no effect if you use the Deferred or Forward+ rendering paths. |
| **Additional Lights** > **Shadow Atlas Resolution** | URP Asset > **Lighting** | Set to the lowest you can accept |
| **Additional Lights** > **Shadow Resolution** | URP Asset > **Lighting** | Set to the lowest you can accept |
| **Cascade Count** | URP Asset > **Shadows** | Set to the lowest you can accept |
| **Conservative Enclosing Sphere** | URP Asset > **Shadows** | Enable |
| **Technique** | [Decal Renderer Feature](renderer-feature-decal.md) | Set to **Screen Space**, and set **Normal Blend** to **Low** or **Medium** |
| **Fast sRGB/Linear conversion** | URP Asset > **Post Processing** | Enable |
| **Grading Mode** | URP Asset > **Post Processing** | Set to **Low Dynamic Range** |
| **LOD Cross Fade Dither** | URP Asset > **Quality** | Set to **Bayer Matrix** |
| **LUT size** | URP Asset > **Post Processing** | Set to the lowest you can accept |
| **Main Light** > **Cast Shadows** | URP Asset > **Lighting** | Disable |
| **Max Distance** | URP Asset > **Shadows** | Reduce |
| **Opaque Downsampling** | URP Asset > **Rendering** | If **Opaque Texture** is enabled in the URP Asset, set to **4x Bilinear** |
| **Render Scale** | URP Asset > **Quality** | Set to below 1.0 |
| **Soft Shadows** | URP Asset > **Shadows** | Disable, or set to **Low** |
| **Upscaling Filter** | URP Asset > **Quality** | Set to **Bilinear** or **Nearest-Neighbor** |

Refer to the following for more information on the settings:

- [Deferred Rendering Path in URP](rendering/deferred-rendering-path.md)
- [Forward+ Rendering Path](rendering/forward-plus-rendering-path.md)
- [Decal Renderer Feature](renderer-feature-decal.md)
- [Universal Render Pipeline Asset](universalrp-asset.md)
- [Universal Renderer](urp-universal-renderer.md)

## Additional resources

- [Understand performance in URP](understand-performance.md)
- [Configure for better performance](configure-for-better-performance.md)
- [Camera component](https://docs.unity3d.com/Documentation/Manual/class-Camera.html)
- [Player Settings > Quality](https://docs.unity3d.com/Manual/class-QualitySettings.html)
