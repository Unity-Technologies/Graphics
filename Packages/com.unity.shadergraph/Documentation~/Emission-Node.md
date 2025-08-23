# Emission node

The Emission node outputs a color that makes a material appear as a visible source of light.

## Render pipeline compatibility

The Emission node is compatible only with the High Definition Render Pipeline (HDRP).

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|-|-|-|-|
| **Color** | Input | Low dynamic range (LDR) RGB color | Sets the low dynamic range (LDR) color to make emissive. |
| **Intensity** | Input | Float | Sets the intensity of the emission of the output color. |
| **Exposure Weight** | Input | Float | Sets how much the [exposure](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Exposure.html) of the scene affects emission. The range is between 0 and 1. A value of 0 means that exposure does not affect this part of the emission. A value of 1 means that exposure fully affects this part of the emission. |
| **Output** | Output | High dynamic range (HDR) RGB color | The emissive color.  |

## Properties

| **Name** | **Description** |
|-|-|
| **Intensity Unit** | Sets the unit of the **Intensity** property. For more information, refer to [Understand physical light units](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Physical-Light-Units.html). The options are: <ul><li>**Nits**: Sets the units as nits, which is a measure of luminance, the surface power of a light source.</li><li>**EV100**: Sets the units as EV<sub>100</sub>, which is a measure of exposure value.</li></ul> |
| **Normalize Color** | Adjusts the intensity of the input color so the red, green, and blue values look similar. As a result, colors are more balanced in the **Output**. |

## Generated code example

```hlsl
float3 Unity_HDRP_GetEmissionHDRColor_float(float3 ldrColor, float luminanceIntensity, float exposureWeight)
{
    // Convert the LDR color. This line is generated only if Normalize Color is enabled.
    ldrColor = ldrColor * rcp(max(Luminance(ldrColor), 1e-6));

    float3 hdrColor = ldrColor * luminanceIntensity;
    float inverseExposureMultiplier = GetInverseCurrentExposureMultiplier();
    hdrColor = lerp(hdrColor * inverseExposureMultiplier, hdrColor, exposureWeight);
    return hdrColor;
}
```

## Additional resources

- [Add light emission to a material](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterEmission.html)
