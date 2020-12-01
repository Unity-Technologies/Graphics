# Color Adjustments

Use this effect to tweak the overall tone, brightness, and contrast of the final rendered image.

## Using Color Adjustments

**Color Adjustments** uses the [Volume](Volumes.html) framework, so to enable and modify **Color Adjustments** properties, you must add a **Color Adjustments** override to a [Volume](Volumes.html) in your Scene. To add **Color Adjustments** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Color Adjustments**. URP now applies **Color Adjustments** to any Camera this Volume affects.

## Properties

![](Images/Inspectors/ColorAdjustments.png)

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Post Exposure** | Adjusts the overall exposure of the Scene in EV (not EV<sub>100</sub>). URP applies this after the HDR effect and before tonemapping, which means that it does not affect previous effects in the chain. |
| **Contrast**      | Use the slider to expand or shrink the overall range of tonal values. Larger positive values expand the tonal range and lower negative values shrink the tonal range. |
| **Color Filter**  | Use the color picker to select which color the Color Adjustment effect should use to multiply the render and tint the result. |
| **Hue Shift**     | Use the slider to shift the hue of all colors.               |
| **Saturation**    | Use the slider to push the intensity of all colors.          |
