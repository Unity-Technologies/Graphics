# Colorspace Conversion Node

## Description

Returns the result of converting the value of input **In** from one colorspace space to another. The spaces to transform from and to are defined by the values of the dropdowns on the node.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Vector 3 | Input value |
| Out | Output      |   Vector 3 | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| From      | Dropdown | RGB, Linear, HSV | Selects the colorspace to convert from |
| To      | Dropdown | RGB, Linear, HSV | Selects the colorspace to convert to |

## Generated Code Example

The following example code represents one possible outcome of this node per from/to permutation.

**RGB > RGB**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    Out =  In;
}
```

**RGB > Linear**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    float3 linearRGBLo = In / 12.92;;
    float3 linearRGBHi = pow(max(abs((In + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
    Out = float3(In <= 0.04045) ? linearRGBLo : linearRGBHi;
}
```

**RGB > HSV**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float  E = 1e-10;
    Out = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}
```

**Linear > RGB**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    float3 sRGBLo = In * 12.92;
    float3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    Out = float3(In <= 0.0031308) ? sRGBLo : sRGBHi;
}
```

**Linear > Linear**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    Out = In;
}
```

**Linear > HSV**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    float3 sRGBLo = In * 12.92;
    float3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), float3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    float3 Linear = float3(In <= 0.0031308) ? sRGBLo : sRGBHi;
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(Linear.bg, K.wz), float4(Linear.gb, K.xy), step(Linear.b, Linear.g));
    float4 Q = lerp(float4(P.xyw, Linear.r), float4(Linear.r, P.yzx), step(P.x, Linear.r));
    float D = Q.x - min(Q.w, Q.y);
    float  E = 1e-10;
    Out = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}
```

**HSV > RGB**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    Out = In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
}
```

**HSV > Linear**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    float3 RGB = In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
    float3 linearRGBLo = RGB / 12.92;
    float3 linearRGBHi = pow(max(abs((RGB + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
    Out = float3(RGB <= 0.04045) ? linearRGBLo : linearRGBHi;
}
```

**HSV > HSV**

```
void Unity_ColorspaceConversion_RGB_RGB_float(float3 In, out float3 Out)
{
    Out = In;
}
```