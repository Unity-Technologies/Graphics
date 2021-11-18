# Refract Node

## Description

The Refract node generates a refraction using an **Incident** vector, a normalized **Normal** vector of the surface, and the refractive index ratio, or **Eta** and produces a new refracted vector. The node uses the principles described in [Sahl-Snell's Law](https://en.wikipedia.org/wiki/Snell%27s_law).

Based on Sahl-Snell's Law, for a medium's given refractive index, there is an angle where the surface behaves like a perfect mirror. When you set the Refract node's Mode to **Safe**, the node generates a null vector when it reaches this critical angle.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Incident      | Input | Vector | None | The normalized vector of what should be refracted to the surface causing the refraction. For example, this could be from a light source to a pixel or the surface, or from the eye to the pixel or surface. |
| Normal      | Input | Vector | None | The normalized normal of the surface that should cause the refraction. |
| IOR Input      | Input | Float    | None | The refractive index Source (where the light is coming from). |
| IOR Medium     | Input | Float    | None | The refractive index Medium (the medium causing the refraction). |
| Out | Output      |  Vector | None | The refracted vector |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | Select **Safe** to prevent the Refract node from returning a NaN result when there is a risk of critical angle refraction. **Safe** will return a null vector result, instead. Select **CriticalAngle** to avoid this check for a potential NaN result. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_RefractCriticalAngle(float3 Incident, float3 Normal, float IORInput, float IORMedium, out float Out)
{
    $precision internalIORInput = max(IORInput, 1.0);
    $precision internalIORMedium = max(IORMedium, 1.0);
    $precision eta = internalIORInput/internalIORMedium;
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - eta*eta*(1.0 - cos0*cos0);
    Out = k >= 0.0 ? eta*Incident - (eta*cos0 + sqrt(k))*Normal : 0.0;
}

void Unity_RefractSafe(float3 Incident, float3 Normal, float IORInput, float IORMedium, out float Out)
{
    $precision internalIORInput = max(IORInput, 1.0);
    $precision internalIORMedium = max(IORMedium, 1.0);
    $precision eta = internalIORInput/internalIORMedium;
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - eta*eta*(1.0 - cos0*cos0);
    Out = eta*Incident - (eta*cos0 + sqrt(max(k, 0.0)))*Normal;
}
```
