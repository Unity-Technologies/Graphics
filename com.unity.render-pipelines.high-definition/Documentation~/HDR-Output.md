# High Definition Range (HDR) Output in HDRP

With HDRP it is possible to output to High Dynamic Range (HDR) devices that allows for wider color gamut and higher brightness range.

HDR Output can be enabled for a given platform in **Player Settings > Other Settings > Use display in HDR mode**.  When this option is enabled, HDRP will process the frame so that when an HDR screen is used as output device it is presented correctly.

## Tonemapping for HDR Output

When the *Use display in HDR mode* option is set in the player settings, all the HDR related options appear in the  [Tonemapping](Post-Processing-Tonemapping.md) volume component. The available options depend on the Tonemapping mode selected.

Unlike tonemapping for LDR displays, the tonemapping for HDR screens require to take into account the capabilities of the device. In particular it is important to adapt to three values:

- Paper white value: determines the brightness value of a paper-white surface. In practice this will determines the overall screen brightness and what brightness the UI will map to. This latter point is important as usually unlit UI is rendered assuming that a value of 1 corresponds to a white color; this assumption is not true when it comes to HDR, so HDRP uses the paper white to tune the UI so that white UI will map to a white value on screen.
- Minimum Brightness: the minimum brightness that the display can display.
- Maximum Brightness: the maximum brightness that the display can display before saturating the values.

All the above values are in nits (candela per square meters).

While it is possible to detect the above three values from the screen outputting your content, it is possible that the values communicated by the device are going to be inaccurate. For this reason, we suggest to implement a calibration menu for your application.

Specifically, detected Paper white values are often going to produce dimmer results than what the LDR content will produce, the reason is that normally TV boost brightness values when displaying LDR content, but respect the actual values when outputting HDR contents.

#### Neutral

![HDR-Output-Neutral](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-Neutral.png)

| **Property**                         | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Neutral HDR Range Reduction Mode** | The tonemapping curve used for the Neutral tonemapper. The options are:<br />- BT2390: Uses a curve defined by the BT2390 broadcasting recommendations. (Default)<br />- Reinhard: A very simple tonemapping operator.<br /><br />This option is available only when Additional Properties are displayed. |
| **Hue Shift Amount**                 | Determines how much hue preservation is desired. When the value is 0, the tonemapper will try to preserve the hue of the content as much as possible, tonemapping only the luminance. However, some content might have been authored assuming that hue-shift will happen when all color channels are tonemapped independently. To recover the hue-shift behaviour this slider can be moved toward See note below for more information. |
| **Detect Paper White**               | Whether the paper white value is detected from the data communicated by the display device. We strongly suggest to provide a calibration screen to let the user set this value depending on their screen and viewing experience. |
| **- Paper White**                    | Paper white value (in nits) set when it is not automatically detected from the display. |
| **Detect Brightness Limits**         | Whether the minimum and maximum brightness values are detected from the data communicated by the display device. While using these values as detected lead to more precise results than detecting paperwhite values, we still suggest to use a calibration screen. |
| **- Min Nits**                       | Minimum brightness supported by the device.                  |
| **- Max Nits**                       | Maximum brightness supported by the device.                  |

While Hue-preserving tonemapping (i.e. Hue Shift Amount set to 0) will better preserve the content colors, sometimes content is authored to rely on the hue shifts that high brightness will produce. A typical example can be for example a very bright flame VFX. The image below shows on the left a flame with Hue Shift Amount set to 0 and on the right the same flame with the hue shift preserved (Hue Shift Amount set to 1).

 <img src="C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-HueShift.png" alt="HDR-Output-HueShift" style="zoom:67%;" />
*Image modified to show the issue clearly.*

The right choice depends on your content, but we suggest to make sure your content is authored to work well without relying on Hue-shift.

#### ACES

Contrary to the neutral version, the ACES tonemapping option has fixed presets. These are meant to target screens with 1000, 2000 and 4000 nits maximum brightness. We suggest to use calibration screen to detect the right preset for the user consuming the content.

![HDR-Output-ACES](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-ACES.png)

##

| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **ACES Preset**        | The tonemapper preset to use. The options are:<br />- ACES 1000 Nits: Curve used to target 1000 nits displays (Default)<br />- ACES 2000 Nits: Curve used to target 2000 nits displays <br />- ACES 4000 Nits: Curve used to target 4000 nits displays |
| **Detect Paper White** | Whether the paper white value is detected from the data communicated by the display device. We strongly suggest to provide a calibration screen to let the user set this value depending on their screen and viewing experience. |
| **- Paper White**      | Paper white value (in nits) set when it is not automatically detected from the display. |

#### Custom

Currently not supported by HDR Output. It is possible to decide whether to fallback on Neutral or ACES for outputting to an HDR device.

#### External

Not supported. Mostly because every different HDR screen used to display the content would need a different LUT. It is possible to decide whether to fallback on Neutral or ACES for outputting to an HDR device.

## Control HDR via script

In addition to controlling the above described Volume Component parameters through the camera volume stack, it is also possible to query the [HDROutputSettings](https://docs.unity3d.com/ScriptReference/HDROutputSettings.html). This can be used for example to:

- Query the display brightness limits ([min](https://docs.unity3d.com/ScriptReference/HDROutputSettings-minToneMapLuminance.htmlhttps://docs.unity3d.com/ScriptReference/HDROutputSettings-minToneMapLuminance.html) and [max](https://docs.unity3d.com/ScriptReference/HDROutputSettings-maxToneMapLuminance.html)) and [paper white value](https://docs.unity3d.com/ScriptReference/HDROutputSettings-paperWhiteNits.html).
- [Request a change in HDR](https://docs.unity3d.com/ScriptReference/HDROutputSettings.RequestHDRModeChange.html), turning it on or off.

## HDR Debug Views

HDRP offers three debug views for HDR rendering. These can be found in **Window > Analysis > Render Pipeline Debugger > Lighting > HDR**

#### Gamut View

Will display on the bottom left of the screen two triangles representing the Rec709 and Rec2020 color gamuts and display what parts of the gamut are covered by the scene.
![HDR-Output-GamutView](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-GamutView.png)

This can be very useful to verify that you are taking advantage of the wider color gamut and is a way to see how color plot changes as you perform color grading.

#### Gamut Clip

Very similar to the above, except it shows red for areas of the screen that are outside the sRGB/Rec709 color gamut and green for anything that is in both the Rec709 and Rec2020 color gamut.

![HDR-Output-GamutClip](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-GamutClip.png)



#### Values over Paperwhite value

This debug view shows the scene as luminance values except for parts of the scene that are over the paper white value that are displayed as a gradient from yellow (paperwhite +1) value to red (max brightness nits)

![HDR-Output-OverPaperWhite](C:\Users\franc\Documents\Github\SRP\com.unity.render-pipelines.high-definition\Documentation~\Images\HDR-Output-OverPaperWhite.png)
