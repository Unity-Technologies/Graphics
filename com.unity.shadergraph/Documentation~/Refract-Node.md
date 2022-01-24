# Refract Node

## Description

You can use the Refract node to give a shader a refraction effect. The Refract node generates a refraction using the following to produce a new refracted vector:

- A normalized incident vector.

- A normalized normal vector of the surface.

- The refractive index of the source and the medium.

The Refract node uses the principles described in [Snell's Law](https://en.wikipedia.org/wiki/Snell%27s_law). A medium's refractive index has an angle where the surface behaves like a perfect mirror. This angle is called total internal reflection. To avoid a NaN result, set the Refract node's **Mode** to **Safe**. This makes the Refract node generate a null vector when it reaches the critical angle before total internal reflection.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Incident      | Input | Vector | None | The normalized vector from the light source to the surface.<br/>For example, this could be from a light source to a pixel, or from the Camera to a surface. |
| Normal      | Input | Vector | None | The normalized normal of the surface that causes the refraction. |
| IOR Source     | Input | Float    | None | The refractive index of the medium the light source originates in. |
| IOR Medium     | Input | Float    | None | The refractive index of the medium that the light refracts into. |
| Refracted | Output      |  Vector | None | The refracted vector. |
| Intensity | Output      |  Float | None | Intensity of the refraction. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | &#8226; **Safe:** Returns a null vector result instead of a NaN result at the point of critical angle refraction. <br/>&#8226; **CriticalAngle:** Avoids the **Safe** check for a potential NaN result. ||

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
    Refracted = k >= 0.0 ? eta*Incident - (eta*cos0 + sqrt(k))*Normal : reflect(Incident, Normal);
    Intensity = internalIORSource <= internalIORMedium ?;
        saturate(F_Transm_Schlick(IorToFresnel0(internalIORMedium, internalIORSource), -cos0)) :
        (k >= 0.0 ? saturate(F_FresnelDielectric(internalIORMedium/internalIORSource, -cos0)) : 0.0);
}

void Unity_RefractSafe(float3 Incident, float3 Normal, float IORInput, float IORMedium, out float Out)
{
    $precision internalIORInput = max(IORInput, 1.0);
    $precision internalIORMedium = max(IORMedium, 1.0);
    $precision eta = internalIORInput/internalIORMedium;
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - eta*eta*(1.0 - cos0*cos0);
    Refracted = eta*Incident - (eta*cos0 + sqrt(max(k, 0.0)))*Normal;
    Intensity = internalIORSource <= internalIORMedium ?;
        saturate(F_Transm_Schlick(IorToFresnel0(internalIORMedium, internalIORSource), -cos0)) :
        (k >= 0.0 ? saturate(F_FresnelDielectric(internalIORMedium/internalIORSource, -cos0)) : 1.0);
}
```
