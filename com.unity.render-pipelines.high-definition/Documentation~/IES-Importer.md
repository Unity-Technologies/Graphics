# IES importer

The High Definition Render Pipeline (HDRP) includes an importer to handle the .ies file format. When you add an [IES profile](IES-Profile.md) to your project, the IES importer Inspector provides information and customization options for the internally used textures.

![](Images/IESImporter1.png)

When you apply the import settings, the importer generates a [Light](Light-Component.md) Prefab as a sub-asset of the IED profile. You can drag the Prefab into the Scene view or Hierarchy to create an instance of a Light that uses the IES profile.

### Properties

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **File Format Version**        | The internal format of the IES file. |
| **Photometric Type**        | The type of the photometric information the IES file stores. |
| **Maximum Intensity**        | The intensity the importer uses to generate Light Prefab. |
| **Manufacturer**        | Metadata about the manufacturer of the luminaire. |
| **Luminaire Catalog Number**        | Metadata about the catalog number of the manufacturer of the luminaire. |
| **Luminaire Description**        | Metadata about the luminaire. |
| **Lamp Catalog Number**        | Metadata about the catalog number of the manufacturer of the Lamp. |
| **Lamp Description**        | Metadata about the Lamp. |
| **Light Type**        | The [Light](Light-Component.md) type the importer uses to generate the Light Prefab. |
| **Spot Angle**        | The distribution of pixels in the projection of spot Light IES profiles. Spot Lights require a 2D texture and, to project the IES in 2D, HDRP uses an angle on the upper hemisphere for a proper projection (specifically, HDRP uses a [Gnomonic projection](https://en.wikipedia.org/wiki/Gnomonic_projection)). |
| **IES Size**        | The size of the texture generated . For spot and area Lights, this is the size of the 2D texture. For point Lights, this is the size of the Cubemap. |
| **Apply Light Attenuation**        | Specifies whether to take the distance to the light source into account for spot Lights in order to have correct light attenuation. |
| **IES Compression**        | The compression Unity uses for the internal texture. |
| **Use IES Maximum Intensity**        | Specifies whether to use the intensity stored in the IES File for the Prefab the importer generates. |
| **Aim Axis Rotation**        | For IES with less symmetry, this parameter will allow us to choose on which orientation we can project the IES in the texture. |
