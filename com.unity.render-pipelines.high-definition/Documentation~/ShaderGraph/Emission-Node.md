# Emission Node

## Ports
| Name         | Direction   | Type           | Description                     |
|:------------ |:------------|:---------------|:--------------------------------|
| color        | Input       | LDR Color(RGB) | LDR color of the emission       |
| intensity    | Input       | Float          | intensity of the emission color |
| output       | Output      | HDR Color(RGB) | Output value                    |

## Emission Unit
There are two emission units available for this node: Luminance and EV<sub>100</sub>.


## Exposure Weight
The Exposure Weight is a value between 0 and 1 which determines how much the emission color will be exposed (the exposure won't affect this part of the emission).