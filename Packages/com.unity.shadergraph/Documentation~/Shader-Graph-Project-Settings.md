# Shader Graph project settings reference

Use the Shader Graph project settings to define shader graph settings for your entire project. To access the Shader Graph project settings, do the following:

1. From the main menu select **Edit** > **Project Settings**. The **Project Settings** window is displayed. 
2. Select **ShaderGraph**.


| Property                 | Description |
|:-------------------------|:----------- |
| **Shader Variant Limit** | Set the maximum number of variants allowed in the project. If your graph exceeds this maximum value, Unity returns the following error:<br/> Validation: Graph is generating too many variants. Either delete Keywords, reduce Keyword variants or increase the Shader Variant Limit in Preferences > Shader Graph. <br/>For more information about shader variants, refer to [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html). |


## Custom Interpolator Channel Settings

It's impossible to limit the number of channels users can create in a Shader Graph. Use **Custom Interpolator Channel Settings** to create alerts that let users know when they're close to, or have exceeded, the channel limit for the target platform. Unity uses these settings to help Shader Graph users maintain compatibility with target platforms when using custom interpolators. For more information, refer to [Custom interpolators](Custom-Interpolators.md).

| Property                 | Description                        |
|:-------------------------|:-----------------------------------|
| **Error Threshold**      | Specify the number of channels at which users are notified that they're at, or have surpassed, the channel limit. This property has a minimum value of 8 channels and must be higher than **Warning Threshold**. |
| **Warning Threshold**    | Specify the number of channels at which users are warned that they are close to the channel limit. This property must be lower than **Error Threshold** and between 8 and 32 channels. |

## Heatmap Color Mode Settings 

You can customize the [Heatmap color mode](Color-Modes.md#heatmap-colors) and use different sets of colors with different node assignments according to your project needs. For this, you first have to [create a custom Shader Graph Heatmap Values asset](Color-Modes.md#customize-the-heatmap-color-mode). 

| Property          | Description                                |
|:------------------|:-------------------------------------------|
| **Custom Values** | Specify the Shader Graph Heatmap Values asset to use for your project, or **None** if you want to use the default values. |

## Additional resources

- [Custom interpolators](Custom-Interpolators.md)
- [Making multiple shader program variants](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html)
