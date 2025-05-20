# Eye Material Inspector reference

You can modify the properties of an Eye material in the Eye Material Inspector.

Refer to [Eyes](eyes.md) for more information.

## Properties

[!include[](snippets/shader-properties/surface-options/lit-surface-options.md)]

### Exposed Properties

#### Sclera

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Sclera Texture**           | Assign a Texture that controls color of the Sclera.          |
| **Sclera Smoothness**        | Sets the smoothness of the Sclera.                           |
| **Sclera Normal**            | Assign a Texture that defines the normal map for the Sclera. |
| **Sclera Normal Strength**   | Modulates the Sclera normal intensity between 0 and 8.       |
| **Sclera Diffusion Profile** | Sets a Diffusion Profile, controlling the Subsurface Scattering properties of the Sclera. |

#### Iris

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Iris Texture**             | Assign a Texture that controls color of the eye’s Iris.      |
| **Iris Clamp Color**         | Sets the color that will be used if the refraction ray reached the inside of the Cornea |
| **Iris Offset**              | Sets the offset of the Iris placement, useful since real world eyes are never symmetrical and centered. |
| **Iris Normal**              | Assign a Texture that defines the normal map for the eye’s Iris. |
| **Iris Normal Strength**     | Modulates the Iris’ normal intensity between 0 and 8.        |
| **Iris Diffusion Profile**   | Sets a Diffusion Profile, controlling the Subsurface Scattering properties of the Iris. |

#### Pupil

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Pupil Radius**             | Sets the radius of the Pupil in the Iris Map as a percentage. |
| **Pupil Debug Mode**         | When enabled, displays a debug mode that allows you to calibrate the desired **Pupil Radius** for your Iris Map. For proper calibration, ensure that the **Iris Offset** is **0**, the **Pupil Aperture** is **0.5** (the neutral position) and then the white circle must be inside the iris pattern. See the following screenshot for an example:<br/>![A front view of an eye, with a white pupil inside the iris.](Images/eye-shader-pupil-debug-mode.png) |
| **Pupil Aperture**           | Sets the state of the pupil’s aperture, 0 being the smallest aperture (**Min Pupil Aperture**) and 1 the widest aperture (**Max Pupil Aperture**). |
| **Minimal Pupil Aperture**   | Sets the minimum pupil aperture value.                       |
| **Maximal Pupil Aperture**   | Sets the maximum pupil aperture value.                       |

#### Limbal Ring

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Limbal Ring Size Iris**    | Sets the relative size of the Limbal Ring in the Iris.       |
| **Limbal Ring Size Sclera**  | Sets the relative size of the Limbal Ring in the Sclera.     |
| **Limbal Ring Fade**         | Sets the fade out strength of the Limbal Ring.               |
| **Limbal Ring Intensity**    | Sets the darkness of the Limbal Ring.                        |

#### Cornea

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Cornea Smoothness**        | Sets the smoothness of the Cornea.                           |

#### Geometry

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Mesh Scale**               | The Eye Shader expects a Mesh of size 1 in Object space. If needed, set this parameter to compensate the mesh size. This is independant of the scale on the Transform component. |

[!include[](snippets/shader-properties/advanced-options/lit-advanced-options.md)]
