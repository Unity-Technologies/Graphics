# RGB to HSV

Menu Path : **Operator > Color > RGB to HSV**

This Operator converts RGB (Red, Green, Blue) color values to HSV (Hue, Saturation, Value) color values.

<video src="Images/Operator-ColourHSV.mp4" title="Colors changing through the hue, saturation, and value (brightness) parameters of the HSV color model." width="320" height="auto" autoplay="true" loop="true" controls></video>

This Operator is useful if you want to construct new colors or if you want to selectively change some aspect of an input color. To do that latter for example, you would use this Operator to change an RGB color to HSV, change the hue (the pure spectrum color), saturation (the intensity), or value (the brightness of the color), then use the [HSV to RGB](Operator-HSVToRGB.md) Operator to convert it back to RBG color.

Note: Color Operators work on a per-particle level. To recolor the particle's texture on a per-pixel level, use **Color Mapping** in the system's output Context or create your own shader via [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html).

## Operator properties

| **Input** | **Type** | **Description**                            |
| --------- | -------- | ------------------------------------------ |
| **Color** | Color    | The RGB values to convert to an HSV color. |

| **Output** | **Type** | **Description**                                    |
| ---------- | -------- | -------------------------------------------------- |
| **HSV**    | Color    | The converted HSV color from the input RGB values. |
