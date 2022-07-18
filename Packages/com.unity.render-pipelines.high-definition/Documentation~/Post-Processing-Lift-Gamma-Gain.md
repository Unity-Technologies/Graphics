# Lift Gamma Gain

This effect allows you to perform three-way color grading. The **Lift Gamma Gain** trackballs follow the [ASC CDL](<https://en.wikipedia.org/wiki/ASC_CDL>) standard. When you adjust the position of the point on the trackball, it shifts the hue of the image towards that color in the given tonal range. Use the different trackballs to affect different ranges within the image. Adjust the slider under the trackball to offset the color lightness of that range.

## Using Lift Gamma Gain

**Lift Gamma Gain** uses the [Volume](Volumes.md) framework, so to enable and modify the lift, gamma, or gain of the render, you must add a **Lift Gamma Gain** override to a [Volume](Volumes.md) in your Scene. To add **Lift Gamma Gain** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Post-processing** and select **Lift Gamma Gain**. HDRP now applies **Lift Gamma Gain** to any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Post-processingLiftGammaGain1.png)

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Lift**     | Use this to control the dark tones. This has a more exaggerated effect on shadows.<br>&#8226; Use the trackball to select which color HDRP should shift the hue of the dark tones to.<br/>&#8226;Use the slider to offset the color lightness of the trackball color. |
| **Gamma**    | Use this to control the mid-range tones with a power function.<br/>&#8226; Use the trackball to select which color HDRP should use to shift the hue of the mid-tones to.<br/>&#8226; Use the slider to offset the color lightness of the trackball color. |
| **Gain**     | Use this to increase the signal and make highlights brighter.<br/>&#8226; Use the trackball to select which color that HDRP uses to shift the hue of the highlights to.<br/>&#8226; Use the slider to offset the color lightness of the trackball color. |
