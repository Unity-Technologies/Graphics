# Gradient Noise Node

## Description

Generates a gradient, or [Perlin](https://en.wikipedia.org/wiki/Perlin_noise), noise based on input **UV**. The scale of the generated noise is controlled by input **Scale**.

You can also choose to use two different hashing methods for calculating the noise. As of Unity version 2021.2, the Gradient Noise node defaults to the **Deterministic** hash, to ensure consistent results for noise generation across platforms.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Scale      | Input | Float    | None | Noise scale |
| Out | Output      |    Float    | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Hash Type      | Dropdown | Deterministic, LegacyMod | Selects the hash function used to generate random numbers for noise generation. |


## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2 unity_gradientNoise_dir(float2 p)
{
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

float unity_gradientNoise(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(unity_gradientNoise_dir(ip), fp);
    float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
}

void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
{
    Out = unity_gradientNoise(UV * Scale) + 0.5;
}
```
