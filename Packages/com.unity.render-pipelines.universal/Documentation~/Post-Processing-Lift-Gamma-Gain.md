# Lift Gamma Gain

This effect allows you to perform three-way color grading. The **Lift Gamma Gain** trackballs follow the [ASC CDL](<https://en.wikipedia.org/wiki/ASC_CDL>) standard. When you adjust the position of the point on the trackball, it shifts the hue of the image towards that color in the given tonal range. Use the different trackballs to affect different ranges within the image. Adjust the slider under the trackball to offset the color lightness of that range.

## Using Lift Gamma Gain

**Lift Gamma Gain** uses the [Volume](Volumes.md) framework, so to enable and modify the lift, gamma, or gain of the render, you must add a **Lift Gamma Gain** override to a [Volume](Volumes.md) in your scene. To add **Lift Gamma Gain** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Lift Gamma Gain**. URP now applies **Lift Gamma Gain** to any Camera this Volume affects.

## Properties

![](Images/Inspectors/LiftGammaGain.png)

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Lift**     | Use this to control the dark tones. This has a more exaggerated effect on shadows.<ul><li>Use the trackball to select which color URP should shift the hue of the dark tones to.</li><li>Use the slider to offset the color lightness of the trackball color.</li></ul> |
| **Gamma**    | Use this to control the mid-range tones with a power function.<ul><li>Use the trackball to select which color URP should use to shift the hue of the mid-tones to.</li><li>Use the slider to offset the color lightness of the trackball color.</li></ul> |
| **Gain**     | Use this to increase the signal and make highlights brighter.<ul><li>Use the trackball to select which color that URP uses to shift the hue of the highlights to.</li><li>Use the slider to offset the color lightness of the trackball color.</li></ul> |
