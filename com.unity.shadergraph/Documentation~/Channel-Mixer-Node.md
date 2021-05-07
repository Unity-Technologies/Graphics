# Channel Mixer Node

## Description

Controls the amount each of the channels of input **In** contribute to each of the channels of output **Out**. The slider parameters on the node control the contribution of each of the input channels. The toggle button parameters control which of the output channels is currently being edited. Slider controls for editing the contribution of each input channnel range between -2 and 2.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Out | Output      |    Vector 3 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|       | Toggle Button Array | R, G, B | Selects the output channel to edit. |
| R      | Slider |  | Controls contribution of input red channel to selected output channel. |
| G      | Slider |  | Controls contribution of input green channel to selected output channel. |
| B      | Slider |  | Controls contribution of input blue channel to selected output channel. |

## Shader Function

## Generated Code Example

The following example code represents one possible outcome of this node.

```
_ChannelMixer_Red = float3 (OutRedInRed, OutRedInGreen, OutRedInBlue);
_ChannelMixer_Green = float3 (OutGreenInRed, OutGreenInGreen, OutGreenInBlue);
_ChannelMixer_Blue = float3 (OutBlueInRed, OutBlueInGreen, OutBlueInBlue);

void Unity_ChannelMixer_float(float3 In, float3 _ChannelMixer_Red, float3 _ChannelMixer_Green, float3 _ChannelMixer_Blue, out float3 Out)
{
    Out = float3(dot(In, _ChannelMixer_Red), dot(In, _ChannelMixer_Green), dot(In, _ChannelMixer_Blue));
}
```
