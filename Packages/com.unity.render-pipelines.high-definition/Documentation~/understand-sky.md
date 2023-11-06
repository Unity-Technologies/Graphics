# Understand sky

You can create the following types of sky if your project use the High Definition Render Pipeline (HDRP):

- [Physically based sky](create-a-physically-based-sky.md)
- [High dynamic range imaging (HDRI) sky](create-an-hdri-sky.md), which is a simple sky that uses a cubemap texture.
- [Gradient sky](create-a-gradient-sky.md), which is a simple sky where HDRP interpolates between three colors.
- [Custom sky effects](create-a-custom-sky.md)

Use a [Visual Environment Volume Override](visual-environment-volume-override-reference.md) to change the type of sky HDRP uses, and configure the sky and clouds.

## Physically based sky

Physically Based Sky simulates a spherical planet with a two-part atmosphere that has an exponentially decreasing density based on its altitude. This means that the higher you go above sea level, the less dense the atmosphere is.

The simulation runs as a pre-process, meaning that it runs once instead of on every frame. The simulation evaluates the atmospheric scattering of all combinations of light and view angles and then stores the results in several 3D Textures, which Unity resamples at runtime. The pre-computation is Scene-agnostic, and only depends on the settings of the Physically Based Sky.

The Physically Based Sky’s atmosphere has two types of particles:

* Air particles with [Rayleigh scattering](<https://en.wikipedia.org/wiki/Rayleigh_scattering>).
* Aerosol particles with anisotropic [Mie scattering](https://en.wikipedia.org/wiki/Mie_scattering). You can use aerosols to model pollution, height fog, or mist.

You can use Physically Based Sky to simulate the sky during both daytime and night-time. You can change the time of day at runtime without reducing performance. The following images display a Physically Based Sky in Unity's Fontainebleau demo. For more information about the Fontainebleau demo, and for instructions on how to download and use the demo yourself, see https://github.com/Unity-Technologies/FontainebleauDemo. The Fontainebleau demo only uses Physically Based Sky for its daytime setup in version 2019.3.

Refer to [Create a physically based sky](create-a-physically-based-sky.md) for more information.

<a name="ImplementationDetails"></a>
### Implementation details

This sky type is a practical implementation of the method outlined in the paper [Precomputed Atmospheric Scattering](https://hal.inria.fr/inria-00288758/en) (Bruneton and Neyret, 2008). This technique assumes that you always view the Scene from above the surface of the planet. This means that if a camera goes below the planet's surface, the sky renders as if the camera was at ground level. 


### Warmup performance impact

When you switch to or from a Physically Based Sky, it might cause a noticeable drop in frame rate. This is because HDRP performs a large amount of precomputations to render a Physically Based Sky, so the first few frames (depending on the **Number of bounces** parameter) takes more time to render than other HDRP sky types.

This also applies when HDRP uses the volume system to interpolate between two different Physically Based Skies with different sets of parameters. To do this, HDRP restarts the precomputation every frame in which it performs interpolation. This causes a noticeable drop in frame rate. To avoid this, use a single set of Physically Based Sky parameters for a scene and change the sun light direction and intensity to achieve the result you want.

HDRP restarts precomputation when you change the following parameters:

- **Type**
- **Planetary Radius**
- **Ground Tint**
- **Air Maximum Altitude**
- **Air Density**
- **Air Tint**
- **Aerosol Maximum Altitude**
- **Aerosol Density**
- **Aerosol Tint**
- **Aerosol Anisotropy**
- **Number of Bounces**

## Additional resources

- Bruneton, Eric, and Fabrice Neyret. 2008. “Precomputed Atmospheric Scattering.” *Computer Graphics Forum* 27, no. 4 (2008): 1079–86. https://hal.inria.fr/inria-00288758/en.