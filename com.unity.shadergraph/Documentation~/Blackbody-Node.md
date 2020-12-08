# Blackbody Node

## Description

Samples a **Gradient** that simulates the effect of black body radiation. 
The calculations in this node are based on data gathered by Mitchell Charity.
This node outputs color in linear RGB space and preforms the conversion using a D65 whitepoint and a CIE 1964 10 degree color space. 
For more information, see [What color is a blackbody?](http://www.vendian.org/mncharity/dir3/blackbody/)

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Temperature      | Input | Float    | None | Temperature or temperature map in Kelvin to sample.  |
| Out | Output      |    Vector 3 | None | Intensity represented by color in Vector 3. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Blackbody_float(float Temperature, out float3 Out)
{
    float3 color = float3(255.0, 255.0, 255.0);
    color.x = 56100000. * pow(Temperature,(-3.0 / 2.0)) + 148.0;
    color.y = 100.04 * log(Temperature) - 623.6;
    if (Temperature > 6500.0) color.y = 35200000.0 * pow(Temperature,(-3.0 / 2.0)) + 184.0;
    color.z = 194.18 * log(Temperature) - 1448.6;
    color = clamp(color, 0.0, 255.0)/255.0;
    if (Temperature < 1000.0) color *= Temperature/1000.0;
    Out = color;
}
```
