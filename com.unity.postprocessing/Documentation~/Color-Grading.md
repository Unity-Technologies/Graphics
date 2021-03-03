# Color Grading

The **Color Grading** effect alters or corrects the color and luminance of the final image that Unity produces. You can use this to alter the look and feel of your application.

![](images\screenshot-grading.png)

The **Color Grading** effect comes with three modes:

- **Low Definition Range (LDR):** ideal for lower-end platforms. Grading is applied to the final rendered frame clamped in a [0,1] range and stored in a standard LUT.
- **High Definition Range (HDR):** ideal for platforms that support HDR rendering. All color operations are applied in HDR and stored into a 3D log-encoded LUT to ensure a sufficient range coverage and precision (Alexa LogC El1000).
- **External:** for use with custom 3D LUTs authored in external software.

## Global Settings

Use these settings to control how the **Color Grading** effect operates.

The Lookup Texture and Contribution settings are only available for **Low Definition Range** and **External** modes.

![](images/grading-1.png)

### Properties

| Property       | Function                                                   |
| :-------------- | :------------------------------------------------------------ |
| Mode | Select the **Color Grading** effect mode. |
| Lookup Texture | **LDR:** Select a custom lookup texture (strip format, e.g. 256x16) to apply before the rest of the color grading operators. If none is provided, a neutral one will be generated internally.<br /><br />**External**: A custom 3D log-encoded texture.|
| Contribution   | **LDR:** Set how much of the lookup texture will contribute to the color grading. |

> **Note:** Volume blending between multiple LDR lookup textures is supported but only works correctly if they're the same size. For this reason it is recommended to stick to a single LUT size for the whole project (256x16 or 1024x32).


## Tonemapping

The **Tonemapping** effect remaps high dynamic range (HDR) colors into a range suitable for mediums with low dynamic range (LDR), such as CRT or LCD screens. Its most common purpose is to make an image with a low dynamic range appear to have a higher range of colors.

This result increases the range of colors and contrast in an image to give a more dynamic and realistic effect. See Wikipedia: [Tone mapping](https://en.wikipedia.org/wiki/Tone_mapping).

Always apply **Tonemapping** when using an HDR camera, otherwise color intensity values above 1 will be clamped at 1, altering the Scene's luminance balance.

### Properties

![](images/grading-2.png)

| Property          | Function                                                     |
| :----------------- | :------------------------------------------------------------ |
| Mode              | Only available in the **High Definition Range** mode. Select the Tonemapping mode from the dropdown menu.</br> **None**: No **Tonemapping** applied.</br> **Neutral**: Applies a range-remapping with minimal impact on color hue and saturation. </br> **ACES**: Applies a close approximation of the reference [ACES](http://www.oscars.org/science-technology/sci-tech-projects/aces) tonemapper for a cinematic look. This effect has more contrast than **Neutral** affects color hue and saturation. When this tonemapper is enabled, all grading operations are performed in the ACES color spaces for optimal precision and results.</br> **Custom**: A fully parametric tonemapper. This is the only tonemapper with its own settings.  |
| Toe Strength      | Set a value for the transition between the toe and the mid section of the curve. A value of 0 means no toe, a value of 1 means a very hard transition. |
| Toe Length        | Set the value for how much of the dynamic range is in the toe. With a small value, the toe will be very short and quickly transition into the linear section, and with a longer value having a longer toe. |
| Shoulder Strength | Set the value for the transition between the mid section and the shoulder of the curve. A value of 0 means no shoulder, value of 1 means a very hard transition. |
| Shoulder Length   | Set the value for how many F-stops (EV) to add to the dynamic range of the curve. |
| Shoulder Angle    | Set the value for how much overshot to add to the shoulder.            |
| Gamma             | Set the value for applying a gamma function to the curve.                       |

## White Balance

**White Balance** allows you to adjust the overall tint and temperature of your image to create a colder or warmer feel in the final render.

![](images/grading-3.png)


### Properties

| Property    | Function                                                     |
| :----------- | :------------------------------------------------------------ |
| Temperature | Set the white balance to a custom color temperature.        |
| Tint        | Set the white balance to compensate for a green or magenta tint. |

## Tone


![](images/grading-4.png)


### Properties

| Property      | Function                                                     |
| :------------- | :------------------------------------------------------------ |
| Post-exposure | Only available in the **High Definition Range (HDR)**  mode. </br>Set the value for the overall exposure of the scene in EV units. This is applied after HDR effect and right before tonemapping so it won’t affect previous effects in the chain. |
| Color Filter  | Select a color for the Tint of the render.                     |
| Hue Shift     | Adjust the hue of all colors.                                |
| Saturation    | Adjust the intensity of all colors.                          |
| Brightness    | Only available in the **Low Definition Range (LDR)** mode. </br>Adjust the brightness of the image.<br /> |
| Contrast      | Adjust the overall range of tonal values.        |


## Channel Mixer

You can use the **Channel Mixer** to adjust the color balance of your image.

The **Channel Mixer** effect modifies the influence each input color channel has on the overall mix of the output channel. For example, if you increase the influence of the green channel on the overall mix of the red channel, all areas of the final image that include a green tone tint to a more reddish hue.


![](images/grading-5.png)


### Properties

| Property | Function                                                     |
| :-------- | :------------------------------------------------------------ |
| Channel  | Select the output channel to modify.                        |
| Red      | Adjust the influence of the red channel within the overall mix. |
| Green    | Adjust the influence of the green channel within the overall mix. |
| Blue     | Adjust the influence of the blue channel within the overall mix. |

## Trackballs

Use **Trackballs** to perform three-way color grading. Adjust the position of the point on the trackball to shift the hue of the image towards that color in each tonal range. Each trackball affects different ranges within the image. Adjust the slider under the trackball to offset the color lightness of that range.

> **Note:** you can right-click a trackball to reset it to its default value. To change the trackball's sensitivity go to  `Edit -> Preferences -> PostProcessing`.


![](images/grading-6.png)


### Properties

| Property | Function                             |
| :-------- | :------------------------------------ |
| Lift     | Adjust the dark tones (or shadows). |
| Gamma    | Adjust the mid-tones.               |
| Gain     | Adjust the highlights.              |

## Grading Curves

**Grading Curves** allows you to adjust specific ranges in hue, saturation, or luminosity. You can adjust the curves on the eight available graphs to achieve effects such as specific hue replacement or desaturating certain luminosities.

### YRGB Curves

**YRGB Curves** are only available in the **Low Definition Range (LDR)** mode. These curves, also called `Master`, `Red`, `Green` and `Blue` affect the selected input channel's intensity across the whole image. The X axis of the graph represents input intensity and the Y axis represents output intensity for the selected channel. Use these curves to adjust the appearance of attributes such as contrast and brightness.


![](images/grading-11.png)


### Hue vs Hue

Use **Hue vs Hue** to shift hues within specific ranges. This curve shifts the input hue (X axis) according to the output hue (Y axis). Use this setting to fine tune hues of specific ranges or perform color replacement.


![](images/grading-7.png)


### Hue vs Sat

Use **Hue vs Sat** to adjust the saturation of hues within specific ranges. This curve adjusts saturation (Y axis) according to the input hue (X axis). Use this setting to tone down particularly bright areas or create artistic effects.


![](images/grading-8.png)


### Sat vs Sat

Use **Sat vs Sat** to adjust the saturation of areas of certain saturation. This curve adjusts saturation (Y axis) according to the input saturation (X axis). Use this setting to fine tune saturation adjustments made with settings from the [**Tone**](#tone) section.


![](images/grading-9.png)


### Lum vs Sat

Use **Lum vs Sat** to adjust the saturation of areas of certain luminance. This curve adjusts saturation (Y axis) according to the input luminance (X axis). use this setting to desaturate areas of darkness to provide an interesting visual contrast.


![](images/grading-10.png)



### Requirements

- Shader Model 3
