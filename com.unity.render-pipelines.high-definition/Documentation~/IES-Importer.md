# IES Importer

![](Images/IES-Importer1.png)

IES Importer inspector provide informations and customization options for the internally used textures.

### Properties

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **File Format Version**        | The internal format of the IES File |
| **Photometric Type**        | The type of the photometric information stored on the IES File |
| **Maximum Intensity**        | The intensity used on the preface generated as the sub-asset |
| **Manufacturer**        | Metadata about the Manufacturer of the luminaire |
| **Luminaire Catalog Number**        | Metadata about the catalog number of the manufactorer of the luminaire |
| **Luminaire Description**        | Metadata about the luminaire |
| **Lamp Catalog Number**        | Metadata about the catalog number of the manufactorer of the Lamp |
| **Lamp Description**        | Metadata about the Lamp |
| **Light Type**        | Light type used for the prefab used |
| **Spot Angle**        | For a spot light a 2D texture is needed, to be able to project the IES in 2D we need an angle on the upper hemisphere for a proper projection (A [Gnomonic projection](https://en.wikipedia.org/wiki/Gnomonic_projection) is used). As we cannot have infinite precision this parameter will decide how the pixels will be distributed. | [Light Mode](https://docs.unity3d.com/Manual/LightModes.html)
| **IES Size**        | The size of the texture generated (Size of the 2D texture (for spot and Area Lights) and the size of the cubemap (for Point lights)). |
| **Apply Light Attenuation**        | For spot light, as its a projection, we need to take in account the distance to the light source to have a proper light attenuation. |
| **IES Compression**        | Compression used for the internal texture |
| **Use IES Maximum Intensity**        | Use the intensity stored in the IES File for the prefab generated as Sub-Asset. |
| **Aim Axis Rotation**        | For IES with less symmetry, this parameter will allow us to choose on which orientation we can project the IES in the texture. |
