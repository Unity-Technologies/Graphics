# Alpha Clipping

The **Alpha Clipping** option controls whether your Material acts as a [Cutout Shader](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) or not.

Enable **Alpha Clipping** to create a transparent effect with hard edges between the opaque and transparent areas. HDRP achieves this effect by not rendering pixels with alpha values below the value you specify in the **Threshold** field. For example, a **Threshold** of 0.1 means that HDRP doesn't render alpha values below 0.1.

If you enable this feature, HDRP exposes the following properties for you to use to customize the Alpha Cutoff effect:

| Property                 | Description                                                  |
| ------------------------ | ------------------------------------------------------------ |
| **Threshold**            | Set the alpha value limit that HDRP uses to determine whether it should render each pixel. If the alpha value of the pixel is equal to or higher than the limit then HDRP renders the pixel. If the value is lower than the limit then HDRP does not render the pixel. The default value is 0.5. |
| **Use Shadow Threshold** | Enable the checkbox to set another threshold value for alpha clipping shadows. |
| **- Shadow Threshold**   | Set the alpha value limit that HDRP uses to determine whether it should render shadows for a pixel. |
| **Alpha to Mask**        | Enable the checkbox to turn on alpha-to-coverage. When MSAA is used, alpha-to-coverage modifies multisample coverage mask proportionally to the pixel Shader result alpha value. This is typically used for anti-aliasing vegetation and other alpha-tested shaders.  |

If you set your [Surface Type](Surface-Type.md) to **Transparent**, HDRP exposes the **Transparent Depth Prepass** and **Transparent Depth Postpass** properties. HDRP allows you to set individual thresholds for these two passes.

| Property                      | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Prepass Threshold**  | Use the slider to set the alpha value limit that HDRP uses for the Transparent depth prepass. This works in the same way as the main **Threshold** property described above.<br />This property only appears when you enable the **Transparent Depth Prepass** checkbox. |
| **Postpass Threshold** | Use the slider to set the alpha value limit that HDRP uses for the transparent depth postpass. This works in the same way as the main **Threshold** property described above.<br />This property only appears when you enable the **Transparent Depth Postpass** checkbox. |

