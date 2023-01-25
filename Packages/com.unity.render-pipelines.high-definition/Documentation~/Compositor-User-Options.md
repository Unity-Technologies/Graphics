# Graphics Compositor window
This page provides an outline of the options available in the Graphics Compositor window.

## Output Options
This section allows you to change where and how the Graphics Compositor outputs the final frame.

The Graphics Compositor can only output to the Game View. To preview the output while you edit the Scene, enable the **Enable Preview** property. **Tip**: To get the best live preview, undock the Game view window into a separate maximized window. If you have two monitors, you can drag the window to a second screen.

| **Property**           | **Description** |
| ----------------------  | --------------- |
| **Enable Compositor** | Specifies whether your Unity Project uses the Graphics Compositor. If you disable this property and the Scene did not previously use the Graphics Compositor, this is the only property visible in the window. If you disable this property and the Scene did previously use the Graphics Compositor, the rest of the properties appear in the window, but you can not edit them. |
| **Enable Preview**   | Specifies whether the Graphics Compositor always outputs to the Game view, even when you are not in Play Mode. If you disable this property, performance increases while you edit a Scene, but the Graphics Compositor output is only available in Play Mode. |
| **Output Camera**      | Specifies the Camera that the Graphics Compositor outputs to. You should use a dedicated Camera rather than re-use another Camera from your Scene. Also, make sure the Compositor's Camera targets a different Display to all other Cameras. This is important because, if the Compositor Camera uses the same display as another Camera, the Display output depends on which Camera Unity rendered last. |
| **Composition Graph** | Specifies the Shader that the Graphics Compositor uses to create the final output. When you first enable the Graphics Compositor for a Scene, this is a pass-through Shader Graph that passes a copy of the input to the output. To set up Camera stacking behavior, this kind of Shader is sufficient, but, for more complex compositing operations, define your own graph. For more details, see [Using the Compositor](Compositor-User-Guide.md). |
| **Display Output**    | Specifies the display that the Graphics Compositor renders to. Unity supports up to eight displays. To see the compositor output, click the **Display** drop-down in the upper left corner of the Game view and select the display number you specified here. |

## Composition Layer Properties

To expose these properties, select a Composition Layer in the **Render Schedule**.

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Color Buffer Format** | Specifies the format to use for this layer's color buffer.   |
| **Resolution**          | Specifies the pixel resolution of this layer. If you use **Full**, the layer's resolution corresponds to the main Camera in the Scene. If you use **Half** or **Quarter** resolution for a layer, it improves performance when Unity renders that layer, but if you combine layers of different resolutions, artifacts may occur depending on the content and the compositing operation. |
| **Output Renderer**     | Specifies a Renderer to direct the output of this layer to. The compositor overrides and automatically updates the **_BaseColorMap** Texture from the Material attached to this Renderer. This is useful when the selected Renderer should be visible by a Camera on another layer. |
| **AOVs**                | Specifies the type of output variable in this layer. Aside from Color, you can also output variables like **Albedo**, **Normal**, or **Smoothness**. This option affects all Cameras stacked in this layer. To make this functionality work in the Unity Player, enable **Runtime AOV API** in the [HDRP Asset](HDRP-Asset.md).  |

## Sub-layer Properties

To expose these properties, select a Sub-layer in the **Render Schedule**.

| **Property** | **Description** |
| ------------ | --------------- |
| **Name** |Sets the name of the Sub-layer. |
| **Source Image** |Specifies a static image/Texture to use as the background for this Sub-layer.<br />This property is only available for **Image** Sub-layers.|
| **Background Fit** |Specifies the method the Graphics Compositor uses to fit the **Source Image** to the screen. The options are:<br />&#8226; **Stretch**: Stretches the image that it completely fills the screen. This method does not maintain the image's original aspect ratio.<br />&#8226; **Fit Horizontally**: Resizes the image so that it fits the screen horizontally. For the vertical axis, Unity either expands the image off the bounds of the screen or uses black bars depending on how tall the image is.<br />&#8226; **Fit Vertically**: Resizes the image so that it fits the screen vertically. For the horizontal axis, Unity either expands the image off the bounds of the screen or uses black bars depending on how wide the image is.<br />This property is only available for **Image** Sub-layers.|
| **Source Video**| Specifies the [Video Player](https://docs.unity3d.com/ScriptReference/Video.VideoPlayer.html) to use for this Sub-layer.<br />This property is only available for **Video** Sub-layers. |
| **Source Camera** |Specifies the Camera to use for this Sub-layer. By default, this is set to the main Camera in the Scene.<br />This property is only available for **Camera** Sub-layers.|
| **Clear Depth**| Specifies whether Unity clears the depth buffer before it draws the contents of this Sub-layer. This option is inactive for the first Sub-layer in a stack, since this Sub-layer always clears the depth.|
| **Clear Alpha** | Specifies whether Unity clears the alpha channel before it draws the contents of this Sub-layer. If you enable this property, post-processing only affects pixels drawn by this Sub-layer. Otherwise, post-processing also affects pixels drawn in previous Composition Layers.|
| **Alpha Range** | Controls how steep the transition between the post-processed and plain regions is. The full [0, 1] range provides the smoothest transition, while shorter ranges provide a steeper transition. Adjusted this option if you see outlines around transparent objects when using per-layer post-processing. For more information on how the compositor uses the alpha channel, see [Using the Graphics Compositor](Compositor-User-Guide.md).|
| **Clear Color** | Overrides the **Background Type** for this Sub-layer. By default, this has the same value as **Background Type** on the Sub-layer's Camera. To override this value, enable the checkbox then select the new value from the drop-down. This option is only available for the first Sub-layer in a stack. The other Sub-layers never clear the color buffer (as required for camera stacking). Setting this option to **None** means the color values are uninitialized when rendering for the sub-layer starts. This guarantees that the compositor does not clear the color, but there is no guarantee of what the contents of the buffer are when you start drawing.|
| **Post Anti-aliasing** |Overrides the **Postprocess Anti-aliasing** mode for this Sub-layer. By default, this has the same value as **Postprocess Anti-aliasing** on the Sub-layer's Camera.|
| **Culling Mask** |Overrides the **Culling Mask** for this Sub-layer. By default, this has the same value as **Culling Mask** on the Sub-layer's Camera.|
| **Volume Mask** |Overrides the **Volume Layer Mask** for this Sub-layer. By default, this has the same value as **Volume Layer Mask** on the Sub-layer's Camera. You can use this to have different post-processing effects for each Sub-layer.|
| **Input Filters**| A list of [filters](#sub-layer-filters) to apply to this Sub-layer. |

### Sub-layer Filters
When you add a new **Input Filter** to a Sub-layer, the properties that appear depend on which filter type you select. The current filter types are:

* [Chroma Keying](#chroma-keying)
* [Alpha Mask](#alpha-mask)

you can use filters to apply common color processing operations to Sub-layers. The filter list is empty by default. To add new filters, click the add (**+**) button.

**Note**: It is possible to implement the functionality of many filters with nodes in the Composition Graph, but if you use the built-in filters instead, it makes the Composition Graph simpler.

#### Chroma Keying
Applies a chroma keying algorithm to the Sub-layer. When you select this filter, you can use the following properties to customize it.
| **Property** | **Description** |
| ---- | ---- |
|**Key Color**| Specifies a color to indicate the areas of the image to mask/make transparent. |
|**Key Threshold**| Sets a threshold that helps to smooth out the edges of the mask. A value of **0** results in sharp edges. |
|**Key Tolerance**| Sets the sensitivity of the **Key Color** property. If you increase this value, the mask includes pixels with color values close to the **Key Color**. |
|**Spill Removal**| Sets a value that Unity uses to change the tint of non-masked areas. |

#### Alpha Mask

Takes as input a static texture that overrides the alpha mask of the Sub-layer. Post-processing is then applied only on the masked frame regions. When you select this filter, you can use the following properties to customize it.

| **Property** | **Description** |
| ---- | ---- |
| **Alpha Mask** |Specifies the Texture that overrides the Sub-layer's alpha mask.|
