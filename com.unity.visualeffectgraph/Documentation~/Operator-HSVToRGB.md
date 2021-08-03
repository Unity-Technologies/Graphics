# HSV to RGB

Menu Path : **Operator > Color > HSV to RGB**

The **HSV to RGB** Operator converts HSV (Hue, Saturation, Value) color values to RGB (Red, Green, Blue) color values.

![](Images/Operator-ColourHSV.gif)

This Operator is useful if you want to construct new colors or if you want to selectively change some aspect of an input color. To do that latter for example, you would use the [RGB to HSV](Operator-RGBToHSV.md) Operator to change an RGB color to HSV, change the hue (the pure spectrum color), saturation (the intensity), or value (the brightness of the color), then use this Operator to convert it back to RBG color.

Note: Color Operators work on a per-particle level. To recolor the particle's texture on a per-pixel level, use **Color Mapping** in the system's output Context or create your own shader via [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html).

## Operator properties

| **Input** | **Type** | **Description**                            |
| --------- | -------- | ------------------------------------------ |
| **HSV**   | Vector3  | The HSV values to convert to an RGB color. |

| **Output** | **Type** | **Description**                                    |
| ---------- | -------- | -------------------------------------------------- |
| **RGB**    | Color    | The converted RGB color from the input HSV values. |