---
uid: um-script-colorcheckertool
---

# Color Checker Tool reference

Use the Color Checker Tool to analyze and compare colors across different visual settings and configurations.

## Color Palette

This procedural color checker calibrates colors and lighting, offering customizable and persistent color fields with support for up to 64 values.

| Value                | Description                                                                                                                         |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| **Color Fields**     | Specifies the total number of color fields.                                                                                         |
| **Fields per Row**   | Sets the number of color fields displayed in each row.                                                                              |
| **Fields Margin**    | Defines the spacing between individual color fields.                                                                                |
| **Add Gradient**     | Adds a gradient at the bottom of the color checker.                                                                                 |
| **Sphere Mode**      | Instantiates spheres for each field.                                                                                                |
| **Compare to Unlit** | Splits the fields into lit and pre-exposed unlit values, which is useful for calibration. Post-process still applies to both sides. |

## Cross Polarized Grayscale

Values are measured without specular lighting using a cross-polarized filter, enhancing accuracy for light calibration in PBR.

| Value                | Description                                                                                                                         |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| **Fields Margin**    | Defines the spacing between individual color fields.                                                                                |
| **Add Gradient**     | Adds a gradient at the bottom of the color checker.                                                                                 |
| **Sphere Mode**      | Instantiates spheres for each field.                                                                                                |
| **Compare to Unlit** | Splits the fields into lit and pre-exposed unlit values, which is useful for calibration. Post-process still applies to both sides. |

## Middle Gray

Neutral 5, the mid-gray value.

| Value                | Description                                                                                                                         |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| **Fields Margin**    | Defines the spacing between individual color fields.                                                                                |
| **Sphere Mode**      | Instantiates spheres for each field.                                                                                                |
| **Compare to Unlit** | Splits the fields into lit and pre-exposed unlit values, which is useful for calibration. Post-process still applies to both sides. |

## Reflection

Useful for checking local reflections.

| Value             | Description                                          |
|-------------------|------------------------------------------------------|
| **Fields Margin** | Defines the spacing between individual color fields. |

## Stepped Luminance

Stepped luminance allows to check gamma calibration.

| Value                | Description                                                                                                                         |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| **Add Gradient**     | Adds a gradient at the bottom of the color checker.                                                                                 |
| **Compare to Unlit** | Splits the fields into lit and pre-exposed unlit values, which is useful for calibration. Post-process still applies to both sides. |

## Material Palette

Material fields are customizable and persistent, with up to 12 values. Each row in the palette represents a material with varying smoothness.

| Value               | Description                                          |
|---------------------|------------------------------------------------------|
| **Material Fields** | Specifies the total number of material fields.       |
| **is Metallic**     | Makes the material highly reflective.                |
| **Fields Margin**   | Defines the spacing between individual color fields. |

## External Texture

Useful for calibration using captured data.

| Value             | Description                                                                    |
|-------------------|--------------------------------------------------------------------------------|
| **Texture**       | Select a lit base texture.                                                     |
| **Unlit Texture** | Select an unlit comparison texture.                                            |
| **Pre Exposure**  | Make the texture values adapt to exposure. Uncheck this when using raw values. |
| **Slicer**        | Compares lit values to unlit, raw values. You can disable pre-exposure.        |
