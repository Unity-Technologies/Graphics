# Alpha Clipping

The **Alpha Clipping** option controls whether your Material acts as a [Cutout Shader](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) or not.

Enable **Alpha Clipping** to create a transparent effect with hard edges between the opaque and transparent areas. HDRP achieves this effect by not rendering pixels with alpha values below the value you specify in the **Threshold** field.

If you enable this feature, HDRP exposes the following properties for you to use to customize the Alpha Cutoff effect:

| Property      | Description                                                  |
| ------------- | ------------------------------------------------------------ |
| **Threshold** | The alpha value limit that HDRP uses to determine whether it should render each pixel. If the alpha value of the pixel is equal to or higher than the limit then HDRP renders the pixel. If the value is lower than the limit then HDRP does not render the pixel. |

If you set your [Surface Type](Surface-Type.html) to **Transparent**, HDRP exposes the following properties:

| Property                      | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Transparent depth prepass** | Adds polygons from transparent surface to the depth buffer to improve their sorting. |
| **- Threshold**               | The alpha value limit that HDRP uses for the Transparent depth prepass. This works in the same way as the main Threshold property described above. |
| **Transparent depth postpass**    | Enable this option to add polygons to the depth buffer that postprocessing uses. |
| **- Threshold**               | The alpha value limit that HDRP uses for the transparent depth postpass. This works in the same way as the main **Threshold** property described above. |

