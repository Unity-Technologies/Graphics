## Description

Controls the amount each of the channels of input **In** contribute to each of the output channels. The slider parameters on the node control the contribution of each of the input channels. The toggle button parameters control which of the output channels is currently being edited.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Out | Output      |    Vector 3 | None | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|       | Toggle Button Array | R, G, B | Selects the output channel to edit. |
| R      | Slider |  | Controls contribution of input red channel to selected output channel. |
| G      | Slider |  | Controls contribution of input green channel to selected output channel. |
| B      | Slider |  | Controls contribution of input blue channel to selected output channel. |

## Shader Function

```
_Node_OutRed = float3 (OutRedInRed, OutRedInGreen, OutRedInBlue);
_Node_OutGreen = float3 (OutGreenInRed, OutGreenInGreen, OutGreenInBlue);
_Node_OutBlue = float3 (OutBlueInRed, OutBlueInGreen, OutBlueInBlue);
Out = float3(dot(In, _Node_OutRed), dot(In, _Node_OutGreen), dot(In, _Node_OutBlue));
```