# Artistic Nodes

## Adjustment


|[Channel Mixer](Channel-Mixer-Node.md)| [Contrast](Contrast-Node.md) |
|:---------:|:---------:|
|![A node-based visual interface with a Channel Mixer block connected to a 3-component vector input labeled X 0, Y 0, Z 0, where only the red (R) channel is fully enabled (set to 1) while green (G) and blue (B) are set to 0, resulting in an output that isolates the red channel.](images/ChannelMixerNodeThumb.png)|![A node-based setup featuring a Contrast block connected to two inputs: a 3-component vector input set to (0, 0, 0) and a single contrast value set to 1, resulting in an output where no visible contrast is applied due to the black input vector.](images/ContrastNodeThumb.png)|
|Controls the amount each of the channels of input In contribute to each of the output channels.|Adjusts the contrast of input In by the amount of input Contrast.|
|[**Hue**](Hue-Node.md)|[**Invert Colors**](Invert-Colors-Node.md)|
|![A Hue block. A (0,0,0) Vector3 is connected to the In(3) slot and the 0 scalar value is connected to the Offset(1) slot. No value is connected to the Out(3) slot. The Degrees option is selected in the Range area.](images/HueNodeThumb.png)|![An Invert Colors block. A 0 scalar is connected to the In(1) slot. No value is connected to the Out (1) slot. A Red checkbox is unselected. The Green, Blue, and Alpha checkboxes are grayed out.](images/InvertColorsNodeThumb.png)|
|Offsets the hue of input In by the amount of input Offset.|Inverts the colors of input In on a per channel basis.|
|[**Replace Color**](Replace-Color-Node.md)|[**Saturation**](Saturation-Node.md)|
|![A Replace Color block. A (0,0,0) Vector3 is connected to the In(3) slot and 0 scalar values are connected to the Range(1) and Fuzziness(1) slots. Empty values are connected to the From (3) and To(3) slots. No value is connected to the Out(3) slot.](images/ReplaceColorNodeThumb.png)|![A Saturation block. A (0,0,0) Vector3 is connected to the In(3) slot and a 0 scalar value is connected to the Saturation(1) slot. No value is connected to the Out(3) slot.](images/SaturationNodeThumb.png)|
|Replaces values in input In equal to input From to the value of input To.|Adjusts the saturation of input In by the amount of input Saturation.|
|[**White Balance**](White-Balance-Node.md)||
|![A White Balance block. A (0,0,0) Vector3 is connected to the In(3) slot and 0 scalar values are connected to the Temperature(1) and Tint(1) slots. No value is connected to the Out(3) slot.](images/WhiteBalanceNodeThumb.png)||
|Adjusts the temperature and tint of input In by the amount of inputs Temperature and Tint respectively.||



## Blend

|[Blend](Blend-Node.md)|
|:---------:|
|![A Blend block. A 0 scalar value is connected to the Base(1), Blend(1), and Opacity(1) slots. No value is connected to the Out(1) slot. The Overlay option is selected in the Mode area.](images/BlendNodeThumb.png)|
|Blends the value of input Blend onto input Base using the blending mode defined by parameter Mode.|



## Filter

|[Dither](Dither-Node.md)|
|:---------:|
|![](images/DitherNodeThumb.png)|
|Dither is an intentional form of noise used to randomize quantization error. It is used to prevent large-scale patterns such as color banding in images..|



## Mask


|[Channel Mask](Channel-Mask-Node.md)| [Color Mask](Color-Mask-Node.md) |
|:---------:|:---------:|
|![](images/ChannelMaskNodeThumb.png)|![](images/ColorMaskNodeThumb.png)|
|Masks values of input In on channels selected in dropdown Channels.|Creates a mask from values in input In equal to input Mask Color.|



## Normal


|[Normal Blend](Normal-Blend-Node.md)| [Normal From Height](Normal-From-Height-Node.md) |
|:---------:|:---------:|
|![](images/NormalBlendNodeThumb.png)|![](images/NormalFromHeightNodeThumb.png)|
|Blends two normal maps defined by inputs A and B together.|Creates a normal map from a height map defined by input Texture.|
|[**Normal Strength**](Normal-Strength-Node.md)|[**Normal Unpack**](Normal-Unpack-Node.md)|
|![](images/NormalStrengthNodeThumb.png)|![](images/NormalUnpackNodeThumb.png)|
|Adjusts the strength of the normal map defined by input In by the amount of input Strength.|Unpacks a normal map defined by input In.|



## Utility


|    [Colorspace Conversion](Colorspace-Conversion-Node.md)    |
| :----------------------------------------------------------: |
|      ![](images/ColorspaceConversionNodeThumb.png)      |
| Returns the result of converting the value of input In from one colorspace space to another. |
