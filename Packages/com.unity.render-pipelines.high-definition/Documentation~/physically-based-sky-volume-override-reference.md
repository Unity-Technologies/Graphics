# Physically Based Sky Volume Override reference

The Physically Based Sky Volume Override lets you configure how the High Definition Render Pipeline (HDRP) renders physically based sky.

Refer to [Create a physically based sky](create-a-physically-based-sky.md) for more information.

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

### Model

| **Property**                   | **Description**                                         |
| ------------------------------ | ------------------------------------------------------- |
| **Type**                       | Indicates a preset HDRP uses to simplify the Inspector. If you select **Earth (Simple)** or **Earth (Advanced)**, the Inspector only shows properties suitable to simulate Earth. |

### Rendering

| **Property**                   | **Description**                                         |
| ------------------------------ | ------------------------------------------------------- |
| **Rendering Mode**             | Indicates wether HDRP should use the default shader or a custom material. |
| **Material**                   | The material used to render the fulscreen sky pass. It is recommended to make it using the **Physically Based Sky** Material type of ShaderGraph. |

### Planet

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Spherical Mode**             | Enables **Spherical Mode**. When in Spherical Mode, you can specify the location of the planet. Otherwise, the planet is always below the Camera in the world-space x-z plane. |
| **Planetary Radius**           | The radius of the planet in meters. The radius is the distance from the center of the planet to the sea level. Only available in **Spherical Mode**. |
| **Planet Center Position**     | The world-space position of the planet's center in meters. This doesn't affect the precomputation. Only available in **Spherical Mode**. |
| **Sea Level**                  | The world-space y coordinate of the planet's sea level in meters. Not available in **Spherical Mode**. |
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
| **- Exposure Compensation**    | The exposure compensation for HDRP to apply to the Scene as environmental light. HDRP uses 2 to the power of your **Exposure Compensation** value to calculate the environment light in your Scene. |
| **- Multiplier**          | The multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value. To make this property visible, set **Intensity Mode** to **Multiplier**. |
| **Update Mode**           | The rate at which HDRP updates the sky environment (using Ambient and Reflection Probes):<br/>&#8226; **On Changed**: HDRP updates the sky environment when one of the sky properties changes.<br/>&#8226; **On Demand**: HDRP waits until you manually call for a sky environment update from a script.<br/>&#8226; **Realtime**: HDRP updates the sky environment at regular intervals defined by the **Update Period**. |
| **- Update Period**       | The period (in seconds) for HDRP to update the sky environment. Set the value to 0 if you want HDRP to update the sky environment every frame. This property only appears when you set the **Update Mode** to **Realtime**. |
| **Include Sun In Baking** | Indicates whether the light and reflection probes generated for the sky contain the sun disk. For details on why this is useful, see [Environment Lighting](Environment-Lighting.md#DecoupleVisualEnvironment). |