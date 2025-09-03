# Artistic nodes

Adjust colors, blend layers, filter images, mask regions, manipulate normal maps, and convert color spaces.

## Adjustment

| **Topic**                              | **Description**                                                                                         |
|:---------------------------------------|---------------------------------------------------------------------------------------------------------|
| [Channel Mixer](Channel-Mixer-Node.md) | Controls the amount each of the channels of input In contribute to each of the output channels.         |
| [Contrast](Contrast-Node.md)           | Adjusts the contrast of input In by the amount of input Contrast.                                       |
| [Hue](Hue-Node.md)                     | Offsets the hue of input In by the amount of input Offset.                                              |
| [Invert Colors](Invert-Colors-Node.md) | Inverts the colors of input In on a per channel basis.                                                  |
| [Replace Color](Replace-Color-Node.md) | Replaces values in input In equal to input From to the value of input To.                               |
| [Saturation](Saturation-Node.md)       | Adjusts the saturation of input In by the amount of input Saturation.                                   |
| [White Balance](White-Balance-Node.md) | Adjusts the temperature and tint of input In by the amount of inputs Temperature and Tint respectively. |


## Blend

| **Topic**              | **Description**                                                                                    |
|:-----------------------|----------------------------------------------------------------------------------------------------|
| [Blend](Blend-Node.md) | Blends the value of input Blend onto input Base using the blending mode defined by parameter Mode. |



## Filter

| **Topic**                | **Description**                                                                                                                                           |
|:-------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Dither](Dither-Node.md) | Dither is an intentional form of noise used to randomize quantization error. It is used to prevent large-scale patterns such as color banding in images.. |



## Mask

| **Topic**                            | **Description**                                                     |
|--------------------------------------|---------------------------------------------------------------------|
| [Channel Mask](Channel-Mask-Node.md) | Masks values of input In on channels selected in dropdown Channels. |
| [Color Mask](Color-Mask-Node.md)     | Creates a mask from values in input In equal to input Mask Color.   |


## Normal

| **Topic**                                        | **Description**                                                                             |
|--------------------------------------------------|---------------------------------------------------------------------------------------------|
| [Normal Blend](Normal-Blend-Node.md)             | Blends two normal maps defined by inputs A and B together.                                  |
| [Normal From Height](Normal-From-Height-Node.md) | Creates a normal map from a height map defined by input Texture.                            |
| [Normal Strength](Normal-Strength-Node.md)       | Adjusts the strength of the normal map defined by input In by the amount of input Strength. |
| [Normal Unpack](Normal-Unpack-Node.md)           | Unpacks a normal map defined by input In.                                                   |


## Utility

| **Topic**                                              | **Description**                                                                              |
|--------------------------------------------------------|----------------------------------------------------------------------------------------------|
| [Colorspace Conversion](Colorspace-Conversion-Node.md) | Returns the result of converting the value of input In from one colorspace space to another. |
