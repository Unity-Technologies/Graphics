# Surface Type

The Surface Type option controls whether your Shader supports transparency or not. 

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Opaque**      | Simulates a completely solid Material with no light penetration. |
| **Transparent** | Simulates a translucent Material that light can penetrate, such as clear plastic or glass. |

Materials with **Transparent Surface Types** are more resource intensive to render than Materials with an **Opaque Surface Type.**

If you set the **Surface Type** to **Transparent**, HDRP exposes options to set the **Blending Mode** and other properties relating to transparency. 

| Property                       | Description                                                  |
| ------------------------------ | ------------------------------------------------------------ |
| **Blending Mode**              | Use the drop-down to determine how HDRP calculates the color of each pixel of the transparent Material by blending the Material with the background pixels. |
| **- Alpha**                    | Uses the Material’s alpha value to change how transparent an object is. 0 is fully transparent. 1 appears fully opaque, but the Material is still rendered during the Transparent render pass. This is useful for visuals that you want to be fully visible but to also fade over time, like clouds. |
| **- Additive**                 | Adds the Material’s RGB values to the background color. The alpha channel of the Material modulates the intensity. A value of 0 adds nothing and a value of 1 adds 100% of the Material color to the background color. |
| - **Premultiply**              | Assumes that you have already multiplied the RGB values of the Material by the alpha channel. This gives better results than **Alpha** blending when filtering images or composing different layers. |
| **Preserve specular lighting** | Preserves the specular elements on the transparent surface, such as sunbeams shining off glass or water |
| **Receive fog**                | Allows fog to affect this transparent surface. When disabled, HDRP does not take this Material into account when it calculates the fog in the Scene. |
| **Appear in Refraction**       | Makes HDRP draw the Material before the refraction pass so that it can include the Material  when calculating refraction. By default, HDRP does not take transparent surfaces into account when it calculates the refraction effect. If you layer multiple transparent surfaces that use this Material, only the top one uses refraction.Enabling this option removes the **Refraction Model** settings option under the **Transparency Input**. |
| **Transparent Sort Priority**  | Allows you to change the rendering order of overlaid transparent surfaces. |