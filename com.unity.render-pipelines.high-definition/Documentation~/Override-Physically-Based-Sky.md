# Physically Based Sky

Physically Based Sky simulates a spherical planet with a two-part atmosphere that has an exponentially decreasing density with respect to its altitude. This means that the higher you go above sea level, the less dense the atmosphere is. For information on the implementation for this sky type, see [Implementation details](#ImplementationDetails).

The simulation runs as a pre-process, meaning that it runs once instead of on every frame. The simulation evaluates the atmospheric scattering of all combinations of light and view angles and then stores the results in several 3D Textures, which Unity resamples at runtime. The pre-computation is Scene-agnostic, and only depends on the settings of the Physically Based Sky. 

The Physically Based Sky’s atmosphere is composed of two types of particles: 

* Air particles with [Rayleigh scattering](<https://en.wikipedia.org/wiki/Rayleigh_scattering>).
* Aerosol particles with anisotropic [Mie scattering](https://en.wikipedia.org/wiki/Mie_scattering). You can use aerosols to model pollution, height fog, or mist.

You can use Physically Based Sky to simulate the sky during both daytime and night-time. The time of day may be arbitrarily changed at runtime without any incurring any extra cost. The following images show Physically Based Sky in Unity's Fontainebleau demo. For more information about the Fontainebleau demo, and for instructions on how to download and use the demo yourself, see https://github.com/Unity-Technologies/FontainebleauDemo. Note that the available Fontainebleau demo only uses Physically Based Sky for its daytime setup in version 2019.3.

![](Images/Override-PhysicallyBasedSky2.png)

![](Images/Override-PhysicallyBasedSky3.png)

## Using Physically Based Sky

Physically Based Sky uses the [Volume](Volumes.md) framework. To enable and modify **Physically Based Sky** properties, add a **Physically Based Sky** override to a [Volume](Volumes.md) in your Scene. To add **Physically Based Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.

2. In the Inspector, go to **Add Override > Sky** and select **Physically Based Sky**.

Next, set the Volume to use **Physically Based Sky**. The [Visual Environment](Override-Visual-Environment.md) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **Physically Based Sky**. HDRP now renders a **Physically Based Sky** for any Camera this Volume affects.

To change how much the atmosphere attenuates light, you can change the density of both air and aerosol molecules (participating media) in the atmosphere. You can also use aerosols to simulate real-world pollution or fog.

![](Images/Override-PhysicallyBasedSky4.png)

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

### Model

| **Property**                   | **Description**                                         |
| ------------------------------ | ------------------------------------------------------- |
| **Type**                       | Indicates a preset HDRP uses to simplify the Inspector. If you select **Earth (Simple)** or **Earth (Advanced)**, the Inspector only shows properties suitable to simulate Earth. |

### Planet

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Spherical Mode**             | Enables **Spherical Mode**. When in Spherical Mode, you can specify the location of the planet. Otherwise, the planet is always below the Camera in the world-space x-z plane. |
| **Planetary Radius**           | The radius of the planet in meters. The radius is the distance from the center of the planet to the sea level.  Only available in **Spherical Mode**. |
| **Planet Center Position**     | The world-space position of the planet's center in meters. This does not affect the precomputation. Only available in **Spherical Mode**. |
| **Sea Level**                  | The world-space y coordinate of the planet's sea level in meters. Not available in **Spherical Mode**. |
| **Planet Rotation**            | The orientation of the planet.                               |
| **Ground Color Texture**       | Specifies a Texture that represents the planet's surface.    |
| **Ground Tint**                | Specifies a color that HDRP uses to tint the **Ground Color Texture**. |
| **Ground Emission Texture**    | Specifies a Texture that represents the emissive areas of the planet's surface. |
| **Ground Emission Multiplier** | A multiplier that HDRP applies to the **Ground Emission Texture**. |

### Space

To make this section visible, set **Type** to **Earth (Advanced)** or **Custom Planet**.

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Space Rotation**            | The orientation of space.                                    |
| **Space Emission Texture**    | Specifies a Texture that represents the emissive areas of space. |
| **Space Emission Multiplier** | A multiplier that HDRP applies to the **Space Emission Texture**. |

### Air

To make this section visible, set **Type** to **Custom Planet**.

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Air Maximum Altitude** | The depth, in meters, of the atmospheric layer, from sea level, composed of air particles. This controls the rate of height-based density falloff. HDRP assumes the air density is negligibly small at this altitude. |
| **Air Density R**        | The red color channel opacity of air at the point in the sky directly above the observer (zenith). This directly affects the color of air at the zenith. |
| **Air Density G**        | The green color channel opacity of air at the point in the sky directly above the observer (zenith). This directly affects the color of air at the zenith. |
| **Air Density B**        | The blue color channel opacity of air at the point in the sky directly above the observer (zenith). This directly affects the color of air at the zenith. |
| **Air Tint**             | The single scattering albedo of air molecules (per color channel). A value of **0** results in absorbing molecules, and a value of **1** results in scattering ones. |

### Aerosols

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Aerosol Maximum Altitude** | The depth, in meters, of the atmospheric layer, from sea level, composed of aerosol particles. This controls the rate of height-based density falloff. HDRP assumes the aerosol density is negligibly small at this altitude. |
| **Aerosol Density**          | The opacity of aerosols at the point in the sky directly above the observer (zenith). This directly affects the color of aerosols at the zenith. |
| **Aerosol Tint**             | The single scattering albedo of aerosol molecules (per color channel). A value of **0** results in absorbing molecules, and a value of **1** results in scattering ones. |
| **Aerosol Anisotropy**       | Specifies the direction of anisotropy:<br/>&#8226; Set this value to **1** for forward scattering.<br/>&#8226; Set this value to **0** to make the anisotropy almost isotropic.<br/>&#8226; Set this value to **-1** for backward scattering.<br/>For high values of anisotropy:<br/>&#8226; If the light path and view direction are aligned, you see a bright atmosphere/fog effect.<br/>&#8226; If they are not aligned, you see a dim atmosphere/fog effect.<br/>&#8226; If anisotropy is **0**, the atmosphere and fog look similar regardless of view direction. |

### Artistic Overrides

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Color Saturation**     | Controls the saturation of the color of the sky.             |
| **Alpha Saturation**     | Controls the saturation of the opacity of the sky.           |
| **Alpha Multiplier**     | A multiplier that HDRP applies to the opacity of the sky.    |
| **Horizon Tint**         | Specifies a color that HDRP uses to tint the sky at the horizon. |
| **Horizon Zenith Shift** | Controls how HDRP blends between the **Horizon Tint** and **Zenith Tint**. If you set this to **-1**, the **Zenith Tint** expands down to the horizon. If you set this to **1**, the **Horizon Tint** expands up to the zenith. |
| **Zenith Tint**          | Specifies a color that HDRP uses to tint the point in the sky directly above the observer (the zenith). |

### Miscellaneous

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Number Of Bounces**     | The number of scattering events. This increases the quality of the sky visuals but also increases the pre-computation time. |
| **Intensity Mode**        | Use the drop-down to select the method that HDRP uses to calculate the sky intensity:<br/>&#8226; **Exposure**: HDRP calculates intensity from an exposure value in EV100.<br/>&#8226; **Multiplier**: HDRP calculates intensity from a flat multiplier. |
| **- Exposure**            | The exposure for HDRP to apply to the Scene as environmental light. HDRP uses 2 to the power of your **Exposure** value to calculate the environment light in your Scene. |
| **- Multiplier**          | The multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value. To make this property visible, set **Intensity Mode** to **Multiplier**. |
| **Update Mode**           | The rate at which HDRP updates the sky environment (using Ambient and Reflection Probes):<br/>&#8226; **On Changed**: HDRP updates the sky environment when one of the sky properties changes.<br/>&#8226; **On Demand**: HDRP waits until you manually call for a sky environment update from a script.<br/>&#8226; **Realtime**: HDRP updates the sky environment at regular intervals defined by the **Update Period**. |
| **- Update Period**       | The period (in seconds) for HDRP to update the sky environment. Set the value to 0 if you want HDRP to update the sky environment every frame. This property only appears when you set the **Update Mode** to **Realtime**. |
| **Include Sun In Baking** | Indicates whether the light and reflection probes generated for the sky contain the sun disk. For details on why this is useful, see [Environment Lighting](Environment-Lighting.md#DecoupleVisualEnvironment). |

<a name="ImplementationDetails"></a>

## Implementation details

This sky type is a practical implementation of the method outlined in the paper [Precomputed Atmospheric Scattering](http://www-ljk.imag.fr/Publications/Basilic/com.lmc.publi.PUBLI_Article@11e7cdda2f7_f64b69/article.pdf) (Bruneton and Neyret, 2008).

This technique assumes that you always view the Scene from above the surface of the planet. This means that if a camera goes below the planet's surface, the sky renders as it would do if the camera was at ground level. Where the surface of the planet is depends on whether you enable or disable **Spherical Mode**:

* If you enable **Spherical Mode**, the **Planetary Radius** and **Planet Center Position** properties define where the surface is. In this mode, the surface is at the distance set in **Planetary Radius** away from the position set in **Planet Center Position**.
* Otherwise, the **Sea Level** property defines where the surface is. In this mode, the surface stretches out infinitely on the xz plane and **Sea Level** sets its world space height.

The default values in either mode make it so the planet's surface is at **0** on the y-axis at the Scene origin. Since the default values for **Spherical Mode** simulate Earth, the radius is so large that, when you create you Scene environment, you can consider the surface to be flat. If you want some areas of your Scene environment to be below the current surface height, you can either vertically offset your Scene environment so that the lowest areas are above **0** on the y-axis, or decrease the surface height. To do the latter:

* If in **Spherical Mode**, either decrease the **Planetary  Radius**, or move the **Planet Center Position** down.

* If not in **Spherical Mode**, decrease the **Sea Level**.

### Reference list

* Bruneton, Eric, and Fabrice Neyret. 2008. “Precomputed Atmospheric Scattering.” *Computer Graphics Forum* 27, no. 4 (2008): 1079–86. https://doi.org/10.1111/j.1467-8659.2008.01245.x.
