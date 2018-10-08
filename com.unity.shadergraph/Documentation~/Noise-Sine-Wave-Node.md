# Noise Sine Wave Node

## Description

Returns the sine of the value of input **In**. For variance, psuedo-random noise is added to the amplitude of the sine wave, within a range determined by input **Min Max**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Min Max | Input | Vector 2 | Minimum and Maximum values for noise intensity  |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

```
void Unity_NoiseSineWave_float4(float4 In, float2 MinMax, out float4 Out)
{
    float sinIn = sin(In);
    float sinInOffset = sin(In + 1.0);
    float randomno =  frac(sin((sinIn - sinInOffset) * (12.9898 + 78.233))*43758.5453);
    float noise = lerp(MinMax.x, MinMax.y, randomno);
    Out = sinIn + noise;
}
```