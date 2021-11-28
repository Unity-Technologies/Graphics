# Fresnel Equation Node

## Description

The Fresnel Equation Node, add various equation for the Fresnel Component. Unity had 3 modes, **Schlick**, **Dielectric** and **DielectricGeneric**.
**Schlick** mode produce an approximation based on the [Schlick's Approximation](https://en.wikipedia.org/wiki/Schlick%27s_approximation), ideal for any interaction between Air-Dielectric material. 
**Dielectric** mode produce code to compute Fresnel equation simplified for Dielectric Material Interaction, for instance Air-Glass, Glass-Water, Water-Air.
**DielectricGeneric** mode produce code to compute [Fresnel equation](https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations) for interaction between Dielectic and any material, for instance clear_coat-Metal, Glass-Metal, Water-Metal, ... (note if we set **IORMediumK** as 0, **DielectricGeneric** behave like **Dielectric**).
Numerical values of refractive indices can be found here [refractiveindex.info](https://refractiveindex.info/).

## Ports (Schlick)

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| f0 | Input | Vector{1, 2, 3} | None | Represente the reflection of the surface when we face typically 0.02-0.08 for a dielectric material. |
| DotVector | Input | Float | None | The dot product between the normal and the surface. |
| Fresnel | Output      |  same as f0 | None | Fresnel coefficient, which describe the amount of light reflected or transmitted. |

## Ports (Dielectric)

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| IOR Source | Input | Vector | None | The refractive index Source, or where the light is coming from. |
| IOR Medium     | Input | Vector | None | The refractive index Medium, or the medium causing the refraction. |
| DotVector | Input | Float | None | The dot product between the normal and the surface. |
| Fresnel | Output      |  same as f0 | None | Fresnel coefficient, which describe the amount of light reflected or transmitted. |

## Ports (DielectricGeneric)

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| IOR Source | Input | Vector | None | The refractive index Source, or where the light is coming from. |
| IOR Medium     | Input | Vector | None | The refractive index Medium, or the medium causing the refraction. |
| IOR MediumK     | Input | Vector | None | The refractive index Medium (imaginary part), or the medium causing the refraction. |
| DotVector | Input | Float | None | The dot product between the normal and the surface. |
| Fresnel | Output      |  same as f0 | None | Fresnel coefficient, which describe the amount of light reflected or transmitted. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | Select **Schlick** for an interaction between Air-Dielectric material, **Dielectric** for an interaction between two dielectric material and **DielectricGeneric** for any interaction between a dielectric and a metal. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_FresnelEquation_Schlick(out float Fresnel, float cos0, float f0)
{
    Fresnel = F_Schlick(f0, cos0);
}

void Unity_FresnelEquation_Dielectric(out float3 Fresnel, float cos0, float3 iorSource, float3 iorMedium)
{
    FresnelValue = F_FresnelDielectric(IorToFresnel0(iorMedium, iorSource), cos0);
}

void Unity_FresnelEquation_DielectricGeneric(out float3 Fresnel, float cos0, float3 iorSource, float3 iorMedium, float3 iorMediumK)
{
    FresnelValue = F_FresnelConductor(iorMedium/iorSource, iorMediumK/iorSource, cos0);
}
```
