# Simple Noise node

The Simple Noise node generates a pseudo-random value, also known as value noise, for each UV coordinate in the input **UV**.

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **UV** | Input | Vector 2 | UV | The UV coordinates to input. For example UV coordinates, refer to [UV Nodes](UV-Nodes.md). |
| **Scale** | Input | Float | None | How much to scale the size of the output. A higher value zooms out so there are more noise values in the same space. The default is 500. |
| **Out** | Output | Float | None | The simple noise. The range of each value is between 0 and 1. |

## Hash Type

The **Hash Type** dropdown determines the hash function Unity uses to generate random numbers for the noise generation.

| **Option** | **Description** |
|-|-|
| **Deterministic** | Uses a hash function that generates the same noise across different platforms. This is the default option. |
| **Legacy Sine** | Uses a sine-based hash function. For most uses, **Deterministic** replaces **Legacy Sine**. |

## Generated code example

The following example code represents one possible outcome of this node.

```
inline float unity_noise_randomValue (float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
}

inline float unity_noise_interpolate (float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}

inline float unity_valueNoise (float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);

    uv = abs(frac(uv) - 0.5);
    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);
    float r0 = unity_noise_randomValue(c0);
    float r1 = unity_noise_randomValue(c1);
    float r2 = unity_noise_randomValue(c2);
    float r3 = unity_noise_randomValue(c3);

    float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
    float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
    float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
    return t;
}

void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
{
    float t = 0.0;

    float freq = pow(2.0, float(0));
    float amp = pow(0.5, float(3-0));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(1));
    amp = pow(0.5, float(3-1));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(2));
    amp = pow(0.5, float(3-2));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    Out = t;
}
```