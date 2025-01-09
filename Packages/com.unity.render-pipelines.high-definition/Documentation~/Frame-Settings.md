# Frame Settings

Frame Settings are settings HDRP uses to render Cameras, real-time, baked, and custom reflections. To find the right balance between render quality and runtime performance, adjust the Frame Settings for your [Cameras](hdrp-camera-component-reference.md) to enable or disable effects at runtime on a per-Camera basis.

You can set the default values for Frame Settings for each of these three individually from within the [HDRP Graphics settings window](Default-Settings-Window.md).

To make Cameras and Reflection Probes use their respective default values for Frame Settings, disable the **Custom Frame Settings** checkbox under the **General** settings of Cameras or under **Capture Settings** of Reflection Probes.

You can override the default value of a Frame Setting on a per component basis. Enable the **Custom Frame Settings** checkbox to set specific Frame Settings for individual Cameras and Reflection Probes. This exposes the Frame Settings Override which gives you access to the same settings as within the HDRP Global Settings. Edit the settings within the Frame Settings Override to create a Frame Settings profile for an individual component.

**Note**: Baked Reflection Probes use the Frame Settings at baking time only. After that, HDRP uses the baked texture without modifying it with updated Frame Settings.

**Note**: If [Virtual Texturing](https://docs.unity3d.com/Documentation/Manual/svt-streaming-virtual-texturing.html) is disabled in your project, the **Virtual Texturing** setting is grayed out.

Frame Settings affect all Cameras and Reflection Probes. HDRP handles Reflection Probes in the same way it does Cameras, this includes Frame Settings. All Cameras and Reflection Probes either use the default Frame Settings or a Frame Settings Override to render the Scene.

## Debugging Frame Settings

You can use the [Rendering Debugger](use-the-rendering-debugger.md) to temporarily change Frame Settings for a Camera without altering the actual Frame Settings data of the Camera itself. This means that, when you stop debugging, the Frame Settings for the Camera are as you set them before you started debugging.
