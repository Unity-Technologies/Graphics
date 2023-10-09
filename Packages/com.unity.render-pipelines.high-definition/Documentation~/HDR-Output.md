# High Dynamic Range (HDR) Output

[High Dynamic Range](https://en.wikipedia.org/wiki/High_dynamic_range) content has a wider color gamut and greater luminosity range than standard definition content.

HDRP can output HDR content for displays which support that functionality.

## Enabling HDR Output

To activate HDR output, navigate to **Project Settings > Player** > **Other Settings** and enable the following settings:

* **Allow HDR Display Output**
* **Use HDR Display Output**

> **Note**: Only enable **Use HDR Display Output** if you need the main display to use HDR Output.

HDR Output will be active only in Game View and in Player. Currently the feature is not working on DirectX 11 on PC, please use DirectX 12 to make use of it.

## HDR tone mapping in HDRP

After you enable **Use display in HDR mode**, you must configure [Tonemapping](Post-Processing-Tonemapping.md) settings for your HDR input.

 In order to configure these settings effectively, you need to understand how certain values related to tone mapping determine the visual characteristics of your HDR output.

### Important tone mapping values

To properly make use of the capabilities of HDR displays, your **Tonemapping** configuration must take into account the capabilities of the target display, specifically these three values (in[ nits](https://en.wikipedia.org/wiki/Candela_per_square_metre)):

* **Minimum supported brightness.**
* **Maximum supported brightness.**
* **Paper White value.** This value represents the brightness of a paper-white surface represented on the display, which determines the display's brightness overall. Low Dynamic Range (LDR) and High Dynamic Range (HDR) content do not appear equally bright on displays with the same Paper White value. This is because displays apply extra processing to low dynamic range content that bumps its brightness levels up. For this reason, it is best practice to implement a calibration menu for your application.

### Usable user interfaces depend on accurate Paper White values

[Unlit](Unlit-Shader.html) materials do not respond to lighting changes, so it is standard practice to use an Unlit material for user interfaces. Calculations for Unlit material rendering define brightness with values between 0 and 1 when you are not specifically targeting HDR displays. In this context, a value of 1 corresponds to white, and a value of 0 corresponds to black.

However, in HDR mode, HDRP uses Paper White values to determine the brightness of Unlit materials. This is because HDR values can exceed the 0 to 1 range.

As a result, Paper White values determine the brightness of UI elements in HDR mode, especially white elements, whose brightness matches Paper White values.

### Configure HDR Tone Mapping settings in the Volume component

You can select and adjust Tonemapping modes in the [Volume](Volumes.html) component settings. You can also adjust some aspects of your HDR Tonemapping configuration with a script (see [The HDROutputSettings API](#the-hdroutputsettings-api)).

After you enable **Use display in HDR mode**, HDR [Tonemapping](Post-Processing-Tonemapping.html) options become visible in the [Volume](Volume-Components.html) component.

## Tone mapping modes

HDRP provides two **Tonemapping** modes: **Neutral** and **ACES**. Each Tonemapping mode has some unique properties.

* **Neutral** mode is especially suitable for situations where you do not want the tone mapper to [color grade](https://en.wikipedia.org/wiki/Color_grading) your content.
* [**ACES**](https://en.wikipedia.org/wiki/Academy_Color_Encoding_System) mode uses the ACES reference color space for feature films. It produces a cinematic, contrasty result.

### Neutral

| Property | Description |
| -------- | ----------- |
| **Neutral HDR Range Reduction Mode** | The curve that the Player uses for tone mapping. The options are:<ul><li>**BT2390**: The default. Defined by the [BT.2390](https://www.itu.int/pub/R-REP-BT.2390) broadcasting recommendations.</li><li>**Reinhard**: A simple Tone Mapping operator.</li></ul>This option is only available when you enable **[Additional Properties](/More-Options.md)**. |
| **Hue Shift Amount** | The value determines the extent to which your content retains its original hue after you apply HDR settings. When this value is 0, the tonemapper attempts to preserve the hue of your content as much as possible by only tonemapping [luminance](/Physical-Light-Units.md). |
| **Detect Paper White** | Enable this property if you want HDRP to use the Paper White value that the display communicates to the Unity Engine. In some cases, the value the display communicates may not be accurate. Implement a calibration menu for your application so that users can display your content correctly on displays that communicate inaccurate values. |
| **Paper White** | The Paper White value of the display. If you do not enable **Detect Paper White**, you must specify a value here. |
| **Detect Brightness Limits** | Enable this property if you want HDRP to use the minimum and maximum nit values that the display communicates. In some cases, the value the display communicates may not be accurate. It is best practice to implement a calibration menu for your application to allow for these situations. |
| **Min Nits** | The minimum brightness value of the display. If you do not enable **Detect Brightness Limits**, you must specify a value here and in **Max Nits**. |
| **Max Nits** | The maximum brightness value of the display. If you do not enable **Detect Brightness Limits**, you must specify a value here and in **Min Nits**. |

### Misuse of Hue Shift Amount

Creators might author some content with the intention to use **Hue Shift Amount** to produce special effects. In the illustration below, the **Hue Shift Amount** is 0 for image A and 1 for image B. The flames image B appear more intense because of the hue shift effect. It is preferable not to author content in this way, because settings optimized for special effects can have undesirable effects on other content in the Scene.

 ![Hue Shift Amount Comparison](/Images/HDR-Output-HueShift.png)<br/>*Image A: Output when Hue Shift Amount is 0.*<br/>*Image B: Output when Hue Shift Amount is 1.*

### ACES

This mode has fixed presets to target 1000, 2000, and 4000 nit displays. It is best practice to implement a calibration menu for your application to ensure that the user can select the right preset.

| Property | Description |
| -------- | ----------- |
| **ACES Preset** | The tone mapper preset to use. The options are:<ul><li>**ACES 1000 Nits**: The default. This curve targets 1000 nits displays.</li><li>**ACES 2000 Nits**: Curve that targets 2000 nits displays.</li><li>**ACES 4000 Nits**: Curve that targets 4000 nits displays.</li></ul> |
| **Detect Paper White** | Enable this property if you want HDRP to use the Paper White value that the display communicates to the Unity Engine. In some cases, the value the display communicates may not be accurate. Implement a calibration menu for your application so that users can display your content correctly on displays that communicate inaccurate values. |
| **Paper White** | The Paper White value of the display. If you do not enable **Detect Paper White**, you must specify a value here. |

### The HDROutputSettings API

The [HDROutputSettings](https://docs.unity3d.com/ScriptReference/HDROutputSettings.html) API makes it possible to enable and disable HDR mode, as well as query certain values (such as Paper White).

## HDR Debug Views

HDRP offers three debug views for HDR rendering. To access them, navigate to **Window > Analysis > Render Pipeline Debugger > Lighting > HDR**.

### Gamut View

![HDR-Output-GamutView](Images/HDR-Output-GamutView.png)

The triangles in this debug view indicate which parts of three specific color gamuts this scene covers. The small triangle displays the [Rec709](https://en.wikipedia.org/wiki/Rec._709) gamut values, the medium triangle displays the [P3-D65](https://en.wikipedia.org/wiki/DCI-P3) gamut values, and the large triangle displays the [Rec2020](https://en.wikipedia.org/wiki/Rec._2020) gamut values. This enables you to check color plot changes while color grading. It can also help you ensure that you benefit from the wider color gamut available in HDR.

### Gamut Clip

![HDR-Output-GamutClip](Images/HDR-Output-GamutClip.png)

This debug view indicates the relationship between scene values and specific color gamuts. Areas of the screen with values within the Rec709 gamut are green, areas outside of the Rec709 gamut but inside the P3-D65 gamut are blue, and areas outside of both are red.

### Values exceeding Paper White

![HDR-Output-OverPaperWhite](Images/HDR-Output-OverPaperWhite.png)

This debug view uses a color coded gradient to indicate parts of the Scene that exceed the Paper White value. The gradient ranges from yellow to red. Yellow corresponds to **Paper White** +1, and red corresponds to **Max Nits**.

## Platform Compatibility

HDRP only supports HDR Output on the following platforms:

* Windows with DirectX 11, DirectX 12 or Vulkan
* MacOS with Metal
* Consoles
* XR devices with HDR support

> **Note**: DirectX 11 only supports HDR Output in the Player, it does not support HDR Output in the Editor.
