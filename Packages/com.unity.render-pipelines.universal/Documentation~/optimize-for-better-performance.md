# Optimize for better performance

If the performance of your Universal Render Pipeline (URP) project seems slow, you can analyze your project and adjust settings to increase performance.

## Use the Unity Profiler to analyze your project

You can use the [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html) to get data on the performance of your project in areas such as the CPU and memory.

## Profiler markers

The following table lists markers that appear in the Unity Profiler for a URP frame and have a significant effect on performance.

The table doesn't include a marker if it's deep in the Profiler hierarchy, or the label already describes what URP does.

| **Marker** | **Sub-marker** | **Description** |
|-|-|-|
| **Inl_UniversalRenderPipeline. RenderSingleCameraInternal** || URP builds a list of rendering commands in the [`ScriptableRenderContext`](https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html), for a single camera. URP only records rendering commands in this marker, but doesn't yet execute them. The marker includes the camera name, for example **Main Camera**. |
|| **Inl_ScriptableRenderer.Setup** | URP prepares for rendering, for example preparing render textures for the camera and shadow maps. |
|| **CullScriptable** | URP generates a list of GameObjects and lights to render, and culls (excludes) any that are outside the camera's view. The time this takes depends on the number of GameObjects and lights in your scene. |
| **Inl_ScriptableRenderContext.Submit** || URP submits the list of commands in the `ScriptableRenderContext` to the graphics API. This marker might appear more than once if URP submits commands more than once per frame, or you call [`ScriptableRenderContext.Submit`](https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.Submit.html) in your own code. |
|| **MainLightShadow** | URP renders a [shadow map](https://docs.unity3d.com/Manual/shadow-mapping.html) for the main Directional Light. |
|| **AdditionalLightsShadow** | URP renders shadow maps for other lights. |
|| **UberPostProcess** | URP renders [post-processing effects](EffectList.md) you enable. This marker contains separate markers for some post-processing effects. |
|| **RenderLoop.DrawSRPBatcher** | URP uses the [Scriptable Render Pipeline Batcher](https://docs.unity3d.com/Manual/SRPBatcher.html) to render one or more batches of objects. |
| **CopyColor** || URP copies the color buffer from one render texture to another. You can disable **Opaque Texture** in the [URP Asset](universalrp-asset.md), so that URP only copies the color buffer if it needs to. |
| **CopyDepth** || URP copies the depth buffer from one render texture to another. You can disable **Depth Texture** in the [URP Asset](universalrp-asset.md) unless you need the depth texture (for example, if you use a shader that uses scene depth). |
| **FinalBlit** || URP copies a render texture to the current camera render target. |

## Use a GPU profiler to analyze your project

You can use a platform GPU profiler such as [Xcode](https://docs.unity3d.com/Manual/XcodeFrameDebuggerIntegration.html) to get data on the performance of the GPU during rendering. You can also use a profiler such as [RenderDoc](https://docs.unity3d.com/Manual/RenderDocIntegration.html), but it might provide less accurate performance data.

Data from a GPU profiler includes URP markers for rendering events, such as different render passes.

## Use other tools to analyze your project

You can also use the following tools to analyze the performance of your project:

- [Scene view View Options](https://docs.unity3d.com/Manual/ViewModes.html)
- [Rendering Debugger](features/rendering-debugger.md)
- [Frame Debugger](https://docs.unity3d.com/Manual/frame-debugger-window.html)

## Adjust settings

Based on your analysis, you can adjust the following settings in the [Universal Render Pipeline (URP) Asset](universalrp-asset.md) or the [Universal Renderer asset](urp-universal-renderer.md) to improve the performance of your project.

Depending on your project or the platforms you target, some settings might not have a significant effect. There might also be other settings that have an effect on performance in your project.

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
- [Graphics performance and profiling](https://docs.unity3d.com/Manual/graphics-performance-profiling.html)
- [Best practices for profiling game performance](https://unity.com/how-to/best-practices-for-profiling-game-performance)
- [Tools for profiling and debugging](https://unity.com/how-to/profiling-and-debugging-tools)
- [Native CPU profiling: Tips to optimize your game performance](https://resources.unity.com/games/native-cpu-profiling-tips-to-optimize-your-game-performance)
