# Shader Graph project settings reference

Use the Shader Graph project settings to define shader graph settings for your entire project. 

To open these settings, go to  **Edit** > **Project Settings** > **Shader Graph**.

The **Shader Graph** category in the **Project Settings** window contains the following properties:

| **Property**             | **Description** |
|:-------------------------|:----------- |
| **Override Variant Limit** | Overrides the default limit on the number of shader variants Shader Graph generates for the project. When you disable this property, Shader Graph uses a default of 2048 shader variants and ignores the value of **Shader Variant Limit**. Unity disables this property by default. |
| **Shader Variant Limit** | Sets the maximum number of shader variants Shader Graph generates for the project. For more information about reducing shader variants, refer to [Reduce shader variants in URP](https://docs.unity3d.com/Manual/urp/shader-stripping.html).<br/><br/>This property is available only when you enable **Override Variant Limit**. |



## Custom Interpolator Channel Settings

The **Custom Interpolator Channel Settings** section contains the following properties. For more information, refer to [Custom interpolators](Custom-Interpolators.md).

| **Property**             | **Description**                    |
|:-------------------------|:-----------------------------------|
| **Error Threshold**      | Specify the number of channels at which users are notified that they're at, or have surpassed, the channel limit. This property has a minimum value of 8 channels and must be higher than **Warning Threshold**. |
| **Warning Threshold**    | Specify the number of channels at which users are warned that they are close to the channel limit. This property must be lower than **Error Threshold** and between 8 and 32 channels. |

## Heatmap Color Mode Settings

The **Heatmap Color Mode Settings** section contains the following properties. For more information, refer to [Create a custom Shader Graph Heatmap Values asset](Color-Modes.md#customize-the-heatmap-color-mode).

The **Heatmap Color Mode Settings** section contains the following properties:

| **Property**      | **Description**                            |
|:------------------|:-------------------------------------------|
| **Custom Values** | Specify the Shader Graph Heatmap Values asset to use for your project, or **None** if you want to use the default values. |

## Additional resources

- [Custom interpolators](Custom-Interpolators.md)
- [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html)
