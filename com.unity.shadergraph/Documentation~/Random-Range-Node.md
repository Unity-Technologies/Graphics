# Random Range Node

## Description

Returns a pseudo-random number value based on input **Seed** that is between the minimum and maximum values defined by inputs **Min** and **Max** respectively.

Whilst the same value in input **Seed** will always result in the same output value, the output value itself will appear random. Input **Seed** is a **Vector 2** value for the convenience of generating a random number based on a UV input, however for most cases a **Vector 1** input will suffice.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Seed      | Input | Vector 2 | Seed value used for generation |
| Min      | Input | Vector 1 | Minimum value |
| Max      | Input | Vector 1 | Maximum value |
| Out | Output      |    Vector 1 | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
    float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
    Out = lerp(Min, Max, randomno);
}
```