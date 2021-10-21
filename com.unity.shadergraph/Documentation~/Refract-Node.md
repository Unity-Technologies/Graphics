# Refract Node

## Description

Generates a refraction [Ibn Sahl-Snell-Descartes's Law](https://en.wikipedia.org/wiki/Snell%27s_law). Based on an **Incident** vector, the **Normal** of the surface and **Eta** we produce the refracted vector.

Based on Ibn Sahl's Law, for a given refractive index of the medium, we can reach an angle where all the surface behave like a perfect mirror, that's why in **Safe** Mode generate a null vector, this implementation is here to not double the reflection intensity which is handle by Fresnel.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Incident      | Input | Vector | None | Incident normalied vector to the surface (From surface to source, for instance from pixel to eye). |
| Normal      | Input | Vector | None | Normal normalized of the surface |
| Eta      | Input | Float    | None | The ratio of refractive index (Eta == (source refractive index)/(medium refractive index)), For instance for an interaction from air to glass **Eta** will be 1.0/1.5 and for an interaction from glass to air **Eta** will be 1.5/1.0. |
| Out | Output      |  Vector | None | Refracted vector |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | CriticalAngle, Safe | Select if the implementation will protect against critical angle refraction which will produce a NaN (Square root of a negative number), if the Safe mode is used when we reach the critical angle a null vector is returned. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_RefractCriticalAngle(float3 Incident, float3 Normal, float Eta, out float Out)
{
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - Eta*Eta*(1.0 - cos0*cos0);
    Out = k >= 0.0 ? Eta*Incident - (Eta*cos0 + sqrt(k))*Normal : 0.0;
}

void Unity_RefractSafe(float3 Incident, float3 Normal, float Eta, out float Out)
{
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - Eta*Eta*(1.0 - cos0*cos0);
    Out = Eta*Incident - (Eta*cos0 + sqrt(max(k, 0.0)))*Normal;
}
```
