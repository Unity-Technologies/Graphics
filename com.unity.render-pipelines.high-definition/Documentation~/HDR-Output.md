# High Dynamic Range (HDR) Output in HDRP

[High Dynamic Range](https://en.wikipedia.org/wiki/High-dynamic-range_imaging) content has a wider color gamut and greater luminosity range than standard definition content.

HDRP can output HDRP content for devices which support that functionality.

## Enabling HDR Output

To enable HDR output, navigate to **Project Settings > **Player** > **Other Settings** and enable **Use display in HDR mode**.

## Configuring Tonemapping settings for HDR displays

Enable **Use display in HDR mode** to reveal HDR-related options in the [Tonemapping](https://github.com/Unity-Technologies/Graphics/pull/Post-Processing-Tonemapping.md) [Volume](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.0/manual/Volumes.html) component.

Each **[Tonemapping](Post-Processing-Tonemapping.md)** mode has some unique properties.

When the *Use display in HDR mode* option is set in the player settings, all the HDR related options appear in the  [Tonemapping](Post-Processing-Tonemapping.md) volume component. The available options depend on the Tonemapping mode selected.

To properly make use of the capabilities of HDR displays, your **Tonemapping** configuration must take into account the capabilities of the target device, specifically these three values in [nits](https://en.wikipedia.org/wiki/Candela_per_square_metre):

- Minimum supported brightness.
- Maximum supported brightness.

- Paper white value: determines the brightness value of a paper-white surface. In practice this will determines the overall screen brightness and what brightness the UI will map to. This latter point is important as usually unlit UI is rendered assuming that a value of 1 corresponds to a white color; this assumption is not true when it comes to HDR, so HDRP uses the paper white to tune the UI so that white UI will map to a white value on screen.



While it is possible to detect the above three values from the screen outputting your content, it is possible that the values communicated by the device are going to be inaccurate. For this reason, we suggest to implement a calibration menu for your application.

Specifically, detected Paper white values are often going to produce dimmer results than what the LDR content will produce, the reason is that normally TV boost brightness values when displaying LDR content, but respect the actual values when outputting HDR contents.

#### Neutral

| **Property**                         | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Neutral HDR Range Reduction Mode** | The curve that the Player uses for tone mapping. The options are:<br />- BT2390: The default. Defined by the [BT.2390](https://www.itu.int/pub/R-REP-BT.2390) broadcasting recommendations.<br />- Reinhard: A very simple tonemapping operator.<br /><br />This option is available only when Additional Properties are displayed. |
| **Hue Shift Amount**                 | Determines how much hue preservation is desired. When the value is 0, the tonemapper will try to preserve the hue of the content as much as possible, tonemapping only the luminance. However, some content might have been authored assuming that hue-shift will happen when all color channels are tonemapped independently. To recover the hue-shift behaviour this slider can be moved toward See note below for more information. |
| **Detect Paper White**               | Enable this property if you want HDRP to use the Paper White value that the device communicates. In many cases, this value may be not accurate. It is best practice to implement a calibration menu for your application to allow for these situations. |
| **- Paper White**                    | Paper White value for situations in which you do not use the display-provided value or the display does not provide a value. |
| **Detect Brightness Limits**         | Enable this property if you want HDRP to use the minimum and maximum nit values that the device communicates. In some cases, this value may be incorrect. It is best practice to implement a calibration menu for your application to allow for these situations |
| **- Min Nits**                       | The minimum brightness value for situations in which you do not use the display-provided value or the display does not provide a value. |
| **- Max Nits**                       | The maximum brightness value for situations in which you do not use the display-provided value or the display does not provide a value. |

While Hue-preserving tonemapping (i.e. Hue Shift Amount set to 0) will better preserve the content colors, sometimes content is authored to rely on the hue shifts that high brightness will produce. A typical example can be for example a very bright flame VFX. The image below shows on the left a flame with Hue Shift Amount set to 0 and on the right the same flame with the hue shift preserved (Hue Shift Amount set to 1).

 <img src="C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-HueShift.png" alt="HDR-Output-HueShift" style="zoom:67%;" />
*Image modified to show the issue clearly.*

The right choice depends on your content, but we suggest to make sure your content is authored to work well without relying on Hue-shift.

#### ACES

This mode has fixed presets to target 1000, 2000, and 4000 nit displays. It is best practice to implement a calibration menu for your application to select the right preset.

| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **ACES Preset**        | The tonemapper preset to use. The options are:<br />- ACES 1000 Nits: The default. Curve that targets 1000 nits displays<br />- ACES 2000 Nits: Curve that targets 2000 nits displays<br />- ACES 4000 Nits: Curve that targets 4000 nits displays |
| **Detect Paper White** | Enable this property if you want HDRP to use the Paper White value that the device communicates. In many cases, this value may be not accurate. It is best practice to implement a calibration menu for your application to allow for these situations. |
| **- Paper White**      | Paper White value for situations in which you do not use the display-provided value or the display does not provide a value. |

#### Custom

Currently not supported by HDR Output. It is possible to decide whether to fallback on Neutral or ACES for outputting to an HDR device.

#### External

Not supported. Mostly because every different HDR screen used to display the content would need a different LUT. It is possible to decide whether to fallback on Neutral or ACES for outputting to an HDR device.

## Scripting Tonemapping settings for HDR displays

In addition to controlling the above described Volume Component parameters through the camera volume stack, it is also possible to query the [HDROutputSettings](https://docs.unity3d.com/ScriptReference/HDROutputSettings.html). This can be used for example to:

- Query the display brightness limits ([min](https://docs.unity3d.com/ScriptReference/HDROutputSettings-minToneMapLuminance.htmlhttps://docs.unity3d.com/ScriptReference/HDROutputSettings-minToneMapLuminance.html) and [max](https://docs.unity3d.com/ScriptReference/HDROutputSettings-maxToneMapLuminance.html)) and [paper white value](https://docs.unity3d.com/ScriptReference/HDROutputSettings-paperWhiteNits.html).
- [Request a change in HDR](https://docs.unity3d.com/ScriptReference/HDROutputSettings.RequestHDRModeChange.html), turning it on or off.

## HDR Debug Views

HDRP offers three debug views for HDR rendering. To access them, navigate to **Window > Analysis > Render Pipeline Debugger > Lighting > HDR**

#### Gamut View

This debug view displays two triangles that indicate which parts of the [Rec709](https://en.wikipedia.org/wiki/Rec._709) and [Rec2020](https://en.wikipedia.org/wiki/Rec._2020) color gamuts the Scene covers.

This view enables you check color plot changes during color grading, and to ensure that you are making use of the wider color gamut available in HDR.![HDR-Output-GamutView](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-GamutView.png)

#### Gamut Clip

This debug view indicates values that exceed or remain within specific color gamuts. Areas of the screen outside of the sRGB/Rec709 color gamut are red, and areas within the Rec709 and Rec2020 gamuts are green.

![HDR-Output-GamutClip](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-GamutClip.png)



#### Values exceeding Paper White

This debug view shows the scene as luminance values except for parts of the scene that are over the paper white value that are displayed as a gradient from yellow (paperwhite +1) value to red (max brightness nits)

![HDR-Output-OverPaperWhite](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-OverPaperWhite.png)
