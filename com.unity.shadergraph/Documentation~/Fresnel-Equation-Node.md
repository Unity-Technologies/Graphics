# Fresnel Equation Node

## Description

The Fresnel Equation Node adds equations that affect Material interactions to the Fresnel Component. You can select an equation in the **Mode** dropdown.

You can find Numerical values of refractive indices at [refractiveindex.info](https://refractiveindex.info/).

## Ports (Schlick)

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| f0 | Input | Vector{1, 2, 3} | None | Represente the reflection of the surface when we face typically 0.02-0.08 for a dielectric material. |
| DotVector | Input | Float | None | The dot product between the normal and the surface. |
| Fresnel | Output      |  same as f0 | None | Fresnel coefficient, which describe the amount of light reflected or transmitted. |

## Ports (Dielectric)

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| IOR Source | Input | Vector | None | The refractive index of the medium the light source originates in. |
| IOR Medium     | Input | Vector | None | The refractive index of the medium that the light refracts into. |
| DotVector | Input | Float | None | The dot product between the normal and the surface. |
| Fresnel | Output      |  same as f0 | None | The fresnel coefficient, which describe the amount of light reflected or transmitted. |

## Ports (DielectricGeneric)

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| IOR Source | Input | Vector | None | The refractive index of the medium the light source originates in. |
| IOR Medium     | Input | Vector | None | The refractive index of the medium that the light refracts into. |
| IOR MediumK     | Input | Vector | None | The refractive index Medium (imaginary part), or the medium causing the refraction. |
| DotVector | Input | Float | None | The dot product between the normal and the surface. |
| Fresnel | Output      |  same as f0 | None | Fresnel coefficient, which describe the amount of light reflected or transmitted. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | &#8226; **Schlick**: This mode produces an approximation based on [Schlick's Approximation](https://en.wikipedia.org/wiki/Schlick%27s_approximation). Use the Schlick mode for interactions between air and dielectric materials. <br/>&#8226; **Dielectric**: Use this mode for interactions between two dielectric Materials. For example, air to glass, glass to water, or water to air.<br/>&#8226; **DielectricGeneric**: This mode computes a [Fresnel equation](https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations) for interactions between a dielectric and a metal. For example, clear-coat- to metal, glass to metal, or water to metal. <br/>**Note:** if the **IORMediumK** value is 0, **DielectricGeneric** behaves in the same way as the **Dielectric** mode. ||

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_FresnelEquation_Schlick(out float Fresnel, float cos0, float f0)
{
    Fresnel = F_Schlick(f0, cos0);
}

void Unity_FresnelEquation_Dielectric(out float3 Fresnel, float cos0, float3 iorSource, float3 iorMedium)
{
    FresnelValue = F_FresnelDielectric(iorMedium/iorSource, cos0);
}

void Unity_FresnelEquation_DielectricGeneric(out float3 Fresnel, float cos0, float3 iorSource, float3 iorMedium, float3 iorMediumK)
{
    FresnelValue = F_FresnelConductor(iorMedium/iorSource, iorMediumK/iorSource, cos0);
}
```
