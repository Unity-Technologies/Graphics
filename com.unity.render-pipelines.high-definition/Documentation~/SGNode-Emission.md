# Emission Node

The Emission Node allows you to apply emission in your Shader Graph.

## Ports

| Name          | Direction | Type           | Description                                                  |
| :------------ | :-------- | :------------- | :----------------------------------------------------------- |
| **color**     | Input     | LDR Color(RGB) | Sets the low dynamic range (LDR) color of the emission.      |
| **intensity** | Input     | Float          | Sets the intensity of the emission color.                    |
| **output**    | Output    | HDR Color(RGB) | Outputs the high dynamic range (HDR) color that this Node produces. |

## Emission Unit
You can use two [physical light units](Physical-Light-Units.html) to control the strength of the emission:

* [Nits](Physical-Light-Units.html#Nits).
* [EV<sub>100</sub>](Physical-Light-Units#EV).


## Exposure Weight
You can use Exposure Weight to determine how exposure affects emission. It is a value between **0** and **1** where. A value of **0** means that exposure does not effect this part of the emission. A value of **1** means that exposure fully affects this part of the emission.
