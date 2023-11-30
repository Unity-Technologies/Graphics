# High Dynamic Range (HDR) tonemapping properties

HDRP provides the **Tonemapping** modes **Neutral** and **ACES**. Each Tonemapping mode has some unique properties.

- **Neutral** mode is especially suitable for situations where you do not want the tone mapper to [ color grade](https://en.wikipedia.org/wiki/Color_grading) your content.
- [**ACES**](https://en.wikipedia.org/wiki/Academy_Color_Encoding_System) mode uses the ACES reference color space for feature films. It produces a cinematic, contrasty result.

#### Neutral

| **Property**                         | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Neutral HDR Range Reduction Mode** | The curve that the Player uses for tone mapping. The options are:<br />- **BT2390**: The default. Defined by the [BT.2390](https://www.itu.int/pub/R-REP-BT.2390) broadcasting recommendations.<br />- **Reinhard**: A simple Tone Mapping operator.<br />This option is only available when you enable **[Additional Properties](More-Options.html)**. |
| **Hue Shift Amount**                 | The value determines the extent to which your content retains its original hue after you apply HDR settings. When this value is 0, the tonemapper attempts to preserve the hue of your content as much as possible by only tonemapping [luminance](Physical-Light-Units.nd). |
| **Detect Paper White**               | Enable this property if you want HDRP to use the Paper White value that the display communicates to the Unity Engine. In some cases, the value the display communicates may not be accurate. Implement a calibration menu for your application so that users can display your content correctly on displays that communicate inaccurate values. |
| **Paper White**                      | The Paper White value of the display. If you do not enable **Detect Paper White**, you must specify a value here. |
| **Detect Brightness Limits**         | Enable this property if you want HDRP to use the minimum and maximum nit values that the display communicates. In some cases, the value the display communicates may not be accurate. It is best practice to implement a calibration menu for your application to allow for these situations. |
| **Min Nits**                         | The minimum brightness value of the display. If you do not enable **Detect Brightness Limits**, you must specify a value here and in **Max Nits**. |
| **Max Nits**                         | The maximum brightness value of the display. If you do not enable **Detect Brightness Limits**, you must specify a value here and in **Min Nits**. |

#### ACES

This mode has fixed presets to target 1000, 2000, and 4000 nit displays. It is best practice to implement a calibration menu for your application to ensure that the user can select the right preset.

| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **ACES Preset**        | The tone mapper preset to use. The options are:<br />- ACES 1000 Nits: The default. This curve targets 1000 nits displays<br />- ACES 2000 Nits: Curve that targets 2000 nits displays<br />- ACES 4000 Nits: Curve that targets 4000 nits displays |
| **Detect Paper White** | Enable this property if you want HDRP to use the Paper White value that the display communicates. In some cases, the value the display communicates may not be accurate. It is best practice to implement a calibration menu for your application to allow for these situations. |
| **Paper White**        | The Paper White value of the display. If you do not enable **Detect Paper White**, you must specify a value here. |