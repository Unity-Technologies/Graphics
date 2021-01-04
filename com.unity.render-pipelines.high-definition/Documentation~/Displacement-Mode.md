# Displacement Mode

This setting controls the method that the High Definition Render Pipeline (HDRP) uses to displace Materials.

## Options

The options in the **Displacement Mode** drop-down change depending on the Shader you use.

### Lit Shaders

| **Drop-down option**    | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **None**                | Select this option to apply no displacement to the Material. |
| **Vertex displacement** | Select this option to displace the Mesh’s vertices according to the **Height Map**. |
| **Pixel displacement**  | Select this option to displace the pixels on the Mesh surface according to the **Height Map**. |

### Tessellation Shaders

| **Drop-down option**          | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **None**                      | Select this option to apply no displacement to the Material. |
| **Tessellation displacement** | Select this option to displace the Mesh’s surface according to the **Height Map**. Tessellation Shaders subdivide the Mesh and add vertices according to the Material’s tessellation options. **Tessellation displacement** also affects these vertices. |

## Properties

### Surface Options

#### Shared Properties

| **Property**                         | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Lock with object scale**           | Enable the checkbox to alter the height of the displacement using the **Scale** of the **Transform**. This allows you to preserve the ratio between the amplitude of the displacement and the **Scale** of the **Transform**. |
| **Lock with height map tiling rate** | Enable the checkbox to alter the amplitude of the displacement using the tiling of the **Height Map**. This allows you to preserve the ratio between the amplitude of the displacement and the scale of the **Height Map** Texture. |

#### Pixel Displacement 

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Minimum steps**          | Use the slider to set the minimum number of Texture samples which Unity performs to process pixel displacement. |
| **Maximum steps**          | Use the slider to set the maximum number of Texture samples which Unity performs to process pixel displacement. |
| **Fading mip level start** | Use the slider to set the mip level at which the pixel displacement effect begins to fade out. |
| **Primitive length**       | The length of the Mesh (in meters) on which Unity applies the displacement mapping. |
| **Primitive width**        | The width of the Mesh (in meters) on which Unity applies the displacement mapping. |
| **Depth Offset**           | Enable the checkbox to modify the depth buffer according to the displacement. This allows effects that use the depth buffer ([Contact Shadows](Override-Contact-Shadows.md) for example) to capture pixel displacement details. |

### Surface Inputs

#### Shared Properties

| **Property**          | **Description**                                              |
| --------------------- | ------------------------------------------------------------ |
| **Height Map**        | Assign a Texture that defines the heightmap for this Material. Unity uses this map to apply pixel or vertex displacement to this Material’s Mesh. |
| **- Parametrization** | Use the drop-down to select the parametrization method for the to use for the **Height Map**.<br />&#8226;**Min/Max**: HDRP compares the **Min** and **Max** value to calculate the peak, trough, and base position of the heightmap. If the **Min** is -1 and the **Max** is 3, then the base is at the Texture value 0.25. This uses the full range of the heightmap.<br />&#8226;**Amplitude**: Allows you to manually set the amplitude and base position of the heightmap. This uses the full range of the heightmap. |
| **- Min**             | Set the minimum value in the **Height Map**.                 |
| **- Max**             | Set the maximum value in the **Height Map**.                 |
| **- Offset**          | Set the offset that HDRP applies to the **Height Map**.      |
| **- Amplitude**       | Set the amplitude of the **Height Map**.                     |
| **- Base**            | Use the slider to set the base for the **Height Map**.       |
