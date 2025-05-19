# Physically Based Sky Volume Override reference

The Physically Based Sky Volume Override lets you configure how the High Definition Render Pipeline (HDRP) renders physically based sky.

Refer to [Create a physically based sky](create-a-physically-based-sky.md) for more information.

![A forest in daytime, with a clear sky and a hazy sun.](Images/Override-PhysicallyBasedSky2.png)

## Understand the sky model

The Physically Based Sky simulates a spherical planet with a two-part atmosphere that has an exponentially decreasing density based on its altitude. This means that the higher you go above sea level, the less dense the atmosphere is. Additionally, the model includes an ozone layer following a tent distribution based on altitude. For information on the implementation for this sky type, see [Understand Sky](understand-sky.md#physically-based-sky).

Depending on the **Rendering Space** specified in the **Visual Environement**, the rendering process for the sky is different.
In **World Mode**, the simulation runs as a pre-process, meaning that it runs once instead of on every frame. The simulation evaluates the atmospheric scattering of all combinations of light and view angles and then stores the results in several 3D Textures, which Unity resamples at runtime. The pre-computation is Scene-agnostic, and only depends on the settings of the Physically Based Sky.
In **Camera Mode**, the simulation is precomputed for a given set of lights, meaning that it needs to be recomputed every time a light moves. However, since the position of the camera is not taken into account in this mode, the tables used to store the precomputation are much smaller and faster to compute. This mode is more optimized when the camera doesn't need to go in space and the atmosphere composition is changed over time for weather changes.

The Physically Based Sky’s atmosphere has three types of particles:

* Air particles with [Rayleigh scattering](<https://en.wikipedia.org/wiki/Rayleigh_scattering>).
* Aerosol particles with anisotropic [Mie scattering](https://en.wikipedia.org/wiki/Mie_scattering). You can use aerosols to model pollution, height fog, or mist.
* Ozone particles, which do not contribute to scattering but absorp light. It contributes greatly to the blue color of the sky during twilight.

You can use the Physically Based Sky to simulate the sky during both daytime and night-time. You can change the time of day at runtime without reducing performance.

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

### Model

| **Property**                   | **Description**                                         |
| ------------------------------ | ------------------------------------------------------- |
| **Type**                       | Indicates a preset HDRP uses to simplify the Inspector. If you select **Earth (Simple)** or **Earth (Advanced)**, the Inspector only shows properties suitable to simulate Earth. |
| **Atmospheric Scattering**     | Indicates if HDRP should take into account atmosphere when computing fog on scene objects. This option has a slight performance cost but should be enabled for physically accurate results. This setting is unrelated to the parameters specified in the [Fog override](Override-Fog.md). |

### Rendering

| **Property**                   | **Description**                                         |
| ------------------------------ | ------------------------------------------------------- |
| **Rendering Mode**             | Indicates wether HDRP should use the default shader or a custom material. |
| **Material**                   | The material used to render the fulscreen sky pass. It is recommended to make it using the **Physically Based Sky** Material type of ShaderGraph. |

### Planet

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Planet Rotation**            | The orientation of the planet. Not available in Custom **Rendering Mode**. |
| **Ground Color Texture**       | Specifies a Texture that represents the planet's surface. Not available in Custom **Rendering Mode**. |
| **Ground Tint**                | Specifies a color that HDRP uses to tint the **Ground Color Texture**. |
| **Ground Emission Texture**    | Specifies a Texture that represents the emissive areas of the planet's surface. Not available in Custom **Rendering Mode**. |
| **Ground Emission Multiplier** | A multiplier that HDRP applies to the **Ground Emission Texture**. Not available in Custom **Rendering Mode**. |

### Space

To make this section visible, set **Type** to **Earth (Advanced)** or **Custom Planet**, and the **Rendering Mode** to **Default**.

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

### Ozone

| **Ozone Density Dimmer**     | Controls the density of ozone in the ozone layer. A value of **1** will result in Earth like density. If the value is set to **0**, there will be no ozone in the atmosphere. |
| **Ozone Minimum Altitude**   | The altitude at which ozone layer starts. |
| **Ozone Layer Width**        | Ozone density increases linearly from **Minimum Altitude** until half of the **Layer Width**, then decreases linearly until reaching **0**. |

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
| **Intensity Mode**        | Use the drop-down to select the method that HDRP uses to calculate the sky intensity:<br/>&#8226; **Exposure**: HDRP calculates intensity from an exposure value in EV100.<br/>&#8226; **Multiplier**: HDRP calculates intensity from a flat multiplier. |
| **- Exposure Compensation**    | The exposure compensation for HDRP to apply to the Scene as environmental light. HDRP uses 2 to the power of your **Exposure Compensation** value to calculate the environment light in your Scene. |
| **- Multiplier**          | The multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value. To make this property visible, set **Intensity Mode** to **Multiplier**. |
| **Update Mode**           | The rate at which HDRP updates the sky environment (using Ambient and Reflection Probes):<br/>&#8226; **On Changed**: HDRP updates the sky environment when one of the sky properties changes.<br/>&#8226; **On Demand**: HDRP waits until you manually call for a sky environment update from a script.<br/>&#8226; **Realtime**: HDRP updates the sky environment at regular intervals defined by the **Update Period**. |
| **- Update Period**       | The period (in seconds) for HDRP to update the sky environment. Set the value to 0 if you want HDRP to update the sky environment every frame. This property only appears when you set the **Update Mode** to **Realtime**. |
| **Include Sun In Baking** | Indicates whether the light and reflection probes generated for the sky contain the sun disk. For details on why this is useful, see [Environment Lighting](Environment-Lighting.md#DecoupleVisualEnvironment). |

## Implementation details

This sky type is a practical implementation of the method outlined in the paper [Precomputed Atmospheric Scattering](https://hal.inria.fr/inria-00288758/en) (Bruneton and Neyret, 2008), as well as the method outlined in [A Scalable and Production Ready Sky and Atmosphere Rendering Technique](https://sebh.github.io/publications/egsr2020.pdf) (Hillaire 2020).

This technique assumes that you always view the Scene from above the surface of the planet. This means that if a camera goes below the planet's surface, the sky renders as if the camera was at ground level. Where the surface of the planet is depends on the **Planet** settings set from the **Visual Environement**.

The planet does not render in the depth buffer, this means it won't occlude lens flare and will not behave correctly when using motion blur.

### Precomputation

HDRP will precompute the sky appearance when some parameters are changed. This operation is fast but may slightly impact the framerate if done everyframe. The parameters that affect the result of the precomputation are:
- **Type**
- **Ground Tint**
- **Air Maximum Altitude**
- **Air Density**
- **Air Tint**
- **Aerosol Maximum Altitude**
- **Aerosol Density**
- **Aerosol Tint**
- **Aerosol Anisotropy**
- **Ozone Density Dimmer**
- **Ozone Minimum Altitude**
- **Ozone Layer Width**

Additionally, the precomputation depends on the **Radius** and **Rendering Space** parameters of the visual environement.

### Additional resources

* Bruneton, Eric, and Fabrice Neyret. 2008. “Precomputed Atmospheric Scattering.” *Computer Graphics Forum* 27, no. 4 (2008): 1079–86. https://hal.inria.fr/inria-00288758/en.
* Hillaire. 2020. “A Scalable and Production Ready Sky and Atmosphere Rendering Technique.” *Eurographics Symposium on Rendering 2020*. https://sebh.github.io/publications/egsr2020.pdf.
