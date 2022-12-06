## Description
Adds equations that affect Material interactions to the Fresnel Component.

## Input
**Dot Vector** - The dot product between the normal and the surface.
**F0** - The reflection of the surface when facing the viewer.
**IOR Source** - Refractive index of the medium the light source originates in.
**IOR Medium** - Refractive index of the medium that the light refracts into.
**IOR Medium K** - Refractive index Medium (imaginary part), or the medium causing the refraction.

## Output
**Fresnel** - The fresnel coefficient, which describes the amount of light reflected or transmitted.

## Controls
**Mode** - The Fresnel equation to use: Schlick, Dielectric, Dielectric Generic.