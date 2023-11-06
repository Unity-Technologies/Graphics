# Hair Material Inspector reference

You can modify the properties of a Hair material in the Hair Material Inspector.

Refer to [Hair and fur](hair-and-fur.md) for more information.

## Properties

[!include[](snippets/shader-properties/surface-options/lit-surface-options.md)]

### Exposed Properties

| **Property**                      | **Description**                                              |
| --------------------------------- | ------------------------------------------------------------ |
| **Base Color Map**                | Assign a Texture that controls both the color and opacity of your Material. |
| **Base Color**                    | Set the color of the Material. If you don't assign a Texture, this is the absolute color of the Material. If you do assign a Texture, the final color of the Material is a combination of the Texture you assign and the color you select. The alpha value of the color controls the transparency level for the Material if you select **Transparent** from the **Surface Type** drop-down. |
| **Alpha Cutoff**                  | Set the alpha value limit that HDRP uses to determine whether it should render each pixel. If the alpha value of the pixel is equal to or higher than the limit then HDRP renders the pixel. If the value is lower than the limit then HDRP doesn't render the pixel. This property only appears when you enable the **Alpha Clipping** setting. |
| **Alpha Cutoff Prepass**          | Set the alpha value limit that HDRP uses to determine whether it should discard the fragment from the depth prepass.<br/>This property only appears when you enable the **Alpha Clipping** and **Transparent Depth Postpass** settings. |
| **Alpha Cutoff Postpass**         | Set the alpha value limit that HDRP uses to determine whether it should discard the fragment from the depth postpass.<br/>This property only appears when you enable the **Alpha Clipping** and **Transparent Depth Postpass** settings. |
| **Alpha Cutoff Shadows**          | Set the alpha value limit that HDRP uses to determine whether it should render shadows for a fragment.<br/>This property only appears when you enable the **Use Shadow Threshold** settings. |
| **Base UV Scale Transform**       | Sets the tiling rate (xy) and offsets (zw) for Base Color, Normal, and AO maps. |
| **Normal Map**                    | Assign a Texture that defines the normal map for this Material in tangent space. |
| **Normal Strength**               | Modulates the normal intensity between 0 and 8.              |
| **AO Map**                        | Assign a Texture that defines the ambient occlusion for this material. |
| **AO Use Lightmap UV**            | Set the UV channel used to sample the AO Map. When enabled, UV1 channel will be used. This is useful in the case of overlapping UVs (which is often for hair cards). |
| **Smoothness Mask**               | Assign a Texture that defines the smoothness for this material. |
| **Smoothness UV Scale Transform** | Sets the tiling rate (xy) and offsets (zw) for the Smoothness Mask Map. |
| **Smoothness Min**                | Set the minimum smoothness for this Material.                |
| **Smoothness Max**                | Set the maximum smoothness for this Material.                |
| **Specular Color**                | Set the representative color of the highlight that Unity uses to drive both the primary specular highlight color, which is mainly monochrome, and the secondary specular highlight color, which is chromatic.|
| **Specular Multiplier**           | Modifies the primary specular highlight by this multiplier.  |
| **Specular Shift**                | Modifies the position of the primary specular highlight.     |
| **Secondary Specular Multiplier** | Modifies the secondary specular highlight by this multiplier. |
| **Secondary Specular Shift**      | Modifies the position of the secondary specular highlight    |
| **Transmission Color**            | Set the fraction of specular lighting that penetrates the hair from behind. This is on a per-color channel basis so you can use this property to set the color of penetrating light. Set this to (0, 0, 0) to stop any light from penetrating through the hair. Set this to (1, 1, 1) to have a strong effect with a lot of white light transmitting through the hair. |
| **Transmission Rim**              | Set the intensity of back lit hair around the edge of the hair. Set this to 0 to completely remove the transmission effect. |

[!include[](snippets/shader-properties/advanced-options/lit-advanced-options.md)]
