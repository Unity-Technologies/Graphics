# Injection Points

To determine when Unity executes a Custom Pass Volume, select an **Injection Point** in the [Custom Pass Volume](Custom-Pass-Creating.md#Custom-Pass-Volume) component.

Each injection point affects the way Custom Passes appear in your scene. There are six injection points in the High Definition Render Pipeline (HDRP). If there are multiple Custom Pass volumes assigned to one Injection Point, HDRP executes them in order of priority. For more information see [Custom Pass Volume workflow](#Custom-Pass-Volume-Workflow.md)

Injection points give a Custom Pass Volume component access to a selection of buffers. Each available buffer has different read or write access at each injection point. Each buffer contains a subset of objects rendered before your pass. HDRP creates a color pyramid and depth pyramid at specific points in the rendering pipeline. For more information, see [Custom Pass buffers and pyramids](Custom-Pass-buffers-pyramids.md).

In a **DrawRenderers Custom Pass** you can only use certain materials at specific injection points. For a full list of compatible materials, see [Material and injection point compatibility](Custom-Pass-Creating.md#Material-Injection-Point-Compatibility).

To analyse the actions Unity performs in a render loop and see where Unity executes your Custom Pass, use the [frame debugger](https://docs.unity3d.com/Manual/FrameDebugger.html).

Unity triggers the following injection points in a frame, in order from top to bottom:

| **Injection point**       | **Available buffers and pyramids**                           | **Buffer contents**                                          | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| BeforeRendering           | Depth Buffer                                                 | Empty. <br/>Unity clears the depth buffer immediately before this injection point. | In this injection point you can write to the depth buffer so that Unity doesn’t render depth-tested, opaque objects.<br/>You can also clear the buffer you allocated or the `Custom Buffer`.<br/><br/>When you select this Injection point for a [Fullscreen Custom Pass](Custom-Pass-Creating.md#Full-Screen-Custom-Pass), Unity assigns the camera color buffer as the target by default. |
| AfterOpaqueDepthAndNormal | Depth buffer<br/><br/>Normal buffer                          | All opaque objects.                                          | In this injection point you can modify the normal, roughness and depth buffer. HDRP takes this into account in the lighting and the depth pyramid.<br/><br/>Normal and roughness data is stored in the same buffer. You can use `DecodeFromNormalBuffer` and `EncodeIntoNormalBuffer` methods to read/write normal and roughness data |
| BeforePreRefraction       | Color buffe<br/>Depth buffer<br/>Depth Pyramid (read-only)<br/>Normal buffer (read-only) | All opaque objects and the sky.                              | In this injection point you can render any transparent objects that require refraction. These objects are then included in the color pyramid that Unity uses for refraction when it renders transparent objects. |
| BeforeTransparent         | Color buffer<br/>Depth buffer<br/>Color Pyramid (read-only)<br/>Depth Pyramid (read-only)<br/>Normal buffer (read-only) | All opaque objects.<br/>Transparent PreRefraction objects.<br/>Transparent objects with depth-prepass and screen space reflections (SSR) enabled. | In this Injection Point you can sample the color pyramid that Unity uses for transparent refraction. You can use this to create a blur effect. All objects Unity renders in this injection point will not be in the color pyramid.<br/><br/>You can also use this injection point to draw transparent objects that refract the whole scene, like water. |
| BeforePostProcess         | Color buffer<br/>Depth buffer<br/>Color Pyramid (read-only)<br/>Depth Pyramid (read-only)<br/>Normal buffer (read-only) | All geometry in the frame that uses High Dynamic Range (HDR). | You can use this injection point to perform any post processing effects.<br/><br/>You can also use this injection point to draw opaque and transparent objects. |
| AfterPostProcess          | Color buffer<br/>Depth buffer<br/>Color Pyramid (read-only)<br/>Depth Pyramid (read-only)<br/>Normal buffer (read-only) | The final render of the scene, including post-process effects | This injection point executes the available buffers after Unity applies any post-processing effects.<br/><br/>If you select this injection point, objects that use the depth buffer display jittering artifacts.<br/><br/>When you select this injection point for a [FullscreenCustom Pass](Custom-Pass-Creating.md#Full-Screen-Custom-Pass), Unity assigns the camera color buffer as the target by default. |

The following diagram describes where Unity injects Custom Passes into an HDRP frame:

[![](Images/HDRP-frame-graph-diagram.png)](Images/HDRP-frame-graph-diagram.png)
