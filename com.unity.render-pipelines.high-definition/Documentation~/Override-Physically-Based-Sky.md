# Physically Based Sky

Physically Based Sky simulates a spherical planet with a two-part atmosphere which has an exponentially decreasing density with respect to its altitude. This means that the higher you go above sea level, the less dense the atmosphere is. It is a practical implementation of the method outlined in the paper [Precomputed Atmospheric Scattering](http://www-ljk.imag.fr/Publications/Basilic/com.lmc.publi.PUBLI_Article@11e7cdda2f7_f64b69/article.pdf) (Bruneton and Neyret, 2008).

The simulation runs as a pre-process, meaning that it runs once instead of on every frame. The simulation evaluates the atmospheric scattering of all combinations of light and view angles and then stores the results in several 3D Textures, which Unity resamples at runtime. The pre-computation is Scene-agnostic, and only depends on the settings of the Physically Based Sky. 

The Physically Based Sky’s atmosphere is composed of two types of particles: 

- Colored air particles with [Rayleigh scattering](https://en.wikipedia.org/wiki/Rayleigh_scattering).
- Monochromatic aerosol particles with anisotropic [Mie scattering](https://en.wikipedia.org/wiki/Mie_scattering). You can use aerosols to model pollution, height fog, or mist.

## Using Physically Based Sky

Physically Based Sky uses the [Volume](Volumes.html) framework. To enable and modify **Physically Based Sky** properties, add a **Physically Based Sky** override to a [Volume](Volumes.html) in your Scene. To add **Physically Based Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override > Sky** and select **Physically Based Sky**.

Next, set the Volume to use **Physically Based Sky**. The [Visual Environment](Override-Visual-Environment.html) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **Physically Based Sky**. HDRP now renders a **Physically Based Sky** for any Camera this Volume affects.

To change how much the atmosphere attenuates light, you can change the density of both air and aerosol molecules (participating media) in the atmosphere. You can also use aerosols to simulate real-world pollution or fog. 

Real-world air and aerosols can have different chemical compositions depending on their location on the planet. To change the density of the simulated air and aerosol fog, use the **Air** **Attenuation Distance** and **Aerosol Attenuation Distance** properties to specify how far you can see GameObjects clearly through the atmosphere. At the distance you specify, visibility reduces to roughly a half. At double the distance, visibility is roughly a quarter. 

The **Attenuation Distance** properties use a color picker which you can use to set the distance on a per-color channel basis. This makes the Physically Based Sky tint GameObjects that are further away from the Camera. For example, if you set the Attenuation Distance to (1.0, 0.5, 0.5), the red channel maintains its intensity longer and a GameObject that is further away from the Camera appears to be slightly orange to simulate what it would be like at sunset.

## Properties

![](Images/Override-PhysicallyBasedSky1.png)

| **Name**                         | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Planetary Radius**             | Set the radius of the planet in kilometers. This is the distance from the center of the planet to the sea level. |
| **Planet Center Position**       | Set the world space position for the center of the planet in kilometers. |
| **Air Attenuation Distance**     | Use the color picker to set the average distance a light particle travels in the air between collisions. This controls the density of the air at sea level and represents the distance at which air reduces background light intensity by 63%. The units for this property is per 1000 kilometers. |
| **Air Albedo**                   | Use the color picker to set the single scattering albedo of air molecules, per color channel. This setting is the ratio between the scattering and attenuation coefficients and essentially . Set the value to black (0, 0, 0) to absorb molecules or set the value to white (1, 1, 1) to scatter molecules. |
| **Air Maximum Altitude**         | Set the depth of the atmospheric layer, from sea level, composed of air particles, in kilometers. This controls the rate of height-based density falloff. |
| **Aerosol Attenuation Distance** | Use the color picker to set the average distance a light particle travels in an aerosol between collisions. This controls the density of aerosols at sea level and represents the distance at which aerosols reduces background light intensity by 63%. The units for this property is per 1000 kilometers. |
| **Aerosol Albedo**               | Use the slider to set the single scattering albedo of aerosol molecules. This is the ratio between the scattering and attenuation coefficients. Set the value to 0 to absorb molecules or set the value to 1 to scatter molecules. |
| **Aerosol Maximum Altitude**     | Set the depth of the atmospheric layer, from sea level, composed of aerosol particles, in kilometers. This controls the rate of height-based density falloff. |
| **Aerosol Anisotropy**           | Use the slider to set the direction of the anisotropy:<br />&#8226; Set this value to 1 for forward scattering.<br />&#8226; Set this value to 0 to make the anisotropy almost isotropic.<br />&#8226; Set this value to -1 for backward scattering.<br /><br />For high values of anisotropy:<br />&#8226; If the light path and view direction are aligned, you see a bright atmosphere/fog effect.<br />&#8226; If they are not aligned, you see a dim atmosphere/fog effect.<br />If anisotropy is 0, the atmosphere and fog look similar regardless of view direction. |
| **Number of Bounces**            | Use the slider to set the number of scattering events. This increases the quality of the sky visuals but also increases the pre-computation time |
| **Ground Color**                 | Use the color picker to set the color of the planet's surface. |
| **Ground Albedo Texture**        | Assign a Texture that represents the planet's surface.       |
| **Ground Emission Texture**      | Assign a Texture that represents the emissive areas of the planet's surface. |
| **Planet Rotation**              | Set the orientation of the planet.                           |
| **Space Emission Texture**       | Assign a Texture that represents the emissive areas of space. |
| **Space Rotation**               | Set the orientation of space.                                |
| **Exposure**                     | Set the exposure for HDRP to apply to the Scene as environmental light. HDRP uses 2 to the power of your **Exposure** value to calculate the environment light in your Scene. |
| **Multiplier**                   | Set the multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value. |
| **Update Mode**                  | Use the drop-down to set the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br />&#8226; **On Changed**: HDRP updates the sky environment when one of the sky properties changes.<br />&#8226; **On Demand**: HDRP waits until you manually call for a sky environment update from a script.<br />&#8226; **Realtime**: HDRP updates the sky environment at regular intervals defined by the **Update Period**. |
| **- Update Period**              | Set the period (in seconds) for HDRP to update the sky environment. Set the value to 0 if you want HDRP to update the sky environment every frame. This property only appears when you set the **Update Mode** to **Realtime**. |

 