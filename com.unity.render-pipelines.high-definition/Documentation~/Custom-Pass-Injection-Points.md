# Injection Points

To determine when Unity executes a Custom Pass Volume, select an **Injection Point** in the [Custom Pass Volume](Custom-Pass-Creating.md#Custom-Pass-Volume) component.

Each injection point affects the way Custom Passes appear in your scene. There are six injection points in the HDRP render loop. Unity can only execute one Custom Pass Volume in each injection point.

Injection points give the Custom Pass Volume component access to a selection of buffers. Each available buffer has different read or write access at each **injection point**. Each buffer contains a subset of objects rendered before your pass.

In a **DrawRenderers Custom Pass** you can only use certain materials at specific injection points. For a full list of compatible materials, see [Material and injection point compatibility](Custom-Pass-Creating.md#Material-Injection-Point-Compatibility).

To analyse the actions Unity performs in a render loop and see where Unity executes your Custom Pass, use the [frame debugger](https://docs.unity3d.com/Manual/FrameDebugger.html).

Unity triggers the following injection points in a frame, in order from top to bottom:



| **Injection point**       | **Available buffers**                                        | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| BeforeRendering           | Depth (Write)                                                | Unity clears the depth buffer immediately before this injection point. <br/><br/>In this injection point you can write to the depth buffer so that Unity doesn’t render depth-tested, opaque objects.<br/><br/>You can also clear the buffer you allocated or the `Custom Buffer`.<br/><br/>When you select this Injection point for a [FullscreenCustom Pass](Custom-Pass-Creating.md#Full-Screen-Custom-Pass), Unity assigns the camera color buffer as the target by default. |
| AfterOpaqueDepthAndNormal | Depth (Read \| Write), Normal and roughness (Read \| Write)  | The available buffers for this injection point contain all opaque objects.<br/><br/>In this injection point you can modify the normal, roughness and depth buffer. HDRP takes this into account in the lighting and the depth pyramid.<br/><br/>Normals and roughness are in the same buffer. You can use `DecodeFromNormalBuffer` and `EncodeIntoNormalBuffer` methods to read/write normal and roughness data. |
| BeforePreRefraction       | Color (no pyramid \| Read \| Write), Depth (Read \| Write), Normal and roughness (Read) | The available buffers for this injection point contain all opaque objects and the sky.<br/><br/>In this injection point you can render any transparent objects that require refraction. These objects are then included in the color pyramid that Unity uses for refraction when it renders transparent objects. |
| BeforeTransparent         | Color (Pyramid \| Read \| Write), Depth (Read \| Write), Normal and roughness (Read) | The available buffers for this injection point contain:<br/>- All opaque objects.<br/>- Transparent PreRefraction objects.<br/>- Transparent objects with depth-prepass and screen space reflections (SSR) enabled.<br/><br/>In this Injection Point you can sample the color pyramid that Unity uses for transparent refraction. You can use this to create a blur effect. All objects Unity renders in this injection point will not be in the color pyramid.<br/><br/>You can also use this injection point to draw some transparent objects that refract the whole scene, like water. |
| BeforePostProcess         | Color (Pyramid \| Read \| Write), Depth (Read \| Write), Normal and roughness (Read) | The available buffers for this injection point contain all geometry in the frame that uses High Dynamic Range (HDR). |
| AfterPostProcess          | Color (Read \| Write), Depth (Read)                          | The available buffers for this injection point contain the final render of the scene, including post-process effects.<br/><br/>This injection point executes the available buffers after Unity applies any post-processing effects.<br/><br/>If you select this injection point, objects that use the depth buffer display jittering artifacts.<br/><br/>When you select this injection point for a [FullscreenCustom Pass](Custom-Pass-Creating.md#Full-Screen-Custom-Pass), Unity assigns the camera color buffer as the target by default. |

The following diagram describes where Unity injects Custom Passes into an HDRP frame:

[![](Images/HDRP-frame-graph-diagram.png)](Images/HDRP-frame-graph-diagram.png)
