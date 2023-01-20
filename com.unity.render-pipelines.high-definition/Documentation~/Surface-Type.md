# Surface Type

The **Surface Type** option controls whether your Material supports transparency or not. Each **Surface Type** has a different workflow and so use the **Surface Type** that is most suitable for the Material you are creating.

| **Surface Type** | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Opaque**       | Simulates a solid Material with no light penetration. |
| **Transparent**  | Simulates a transparent Material that light can penetrate, such as clear plastic or glass. Select **Transparent** to expose more properties in the **Surface Options** section and the [Transparency Inputs](#TransparencyInputs) section. |

Materials with **Transparent Surface Types** are more resource intensive to render than Materials with an **Opaque Surface Type**.

## Properties

If you set the **Surface Type** to **Transparent**, HDRP exposes options to set the **Blending Mode** and other properties relating to transparency.

### Surface Options

| Property                              | Description                                                  |
| ------------------------------------- | ------------------------------------------------------------ |
| **Rendering Pass**                    | Use the drop-down to set the rendering pass that HDRP processes this Material in.<br />&#8226; **Before Refraction**: Draws the GameObject before the refraction pass. This means that HDRP includes this Material when it processes refraction. To expose this option, select **Transparent** from the **Surface Type** drop-down.<br />&#8226; **Default**: Draws the GameObject in the default opaque or transparent rendering pass pass, depending on the **Surface Type**.<br />&#8226; **Low Resolution**: Draws the GameObject in half resolution after the **Default** pass.<br />&#8226; **After post-process**: For [Unlit Materials](Unlit-Shader.md) only. Draws the GameObject after all post-processing effects. |
| **Blending Mode**                     | Use the drop-down to determine how HDRP calculates the color of each pixel of the transparent Material by blending the Material with the background pixels.<br />&#8226; **Alpha**: Uses the Material’s alpha value to change how transparent an object is. 0 is fully transparent. 1 appears fully opaque, but the Material is still rendered during the Transparent render pass. <br />&#8226; **Additive**: Adds the Material’s RGB values to the background color. The alpha channel of the Material modulates the intensity. A value of 0 adds nothing and a value of 1 adds 100% of the Material color to the background color.<br />&#8226; **Premultiply**: Assumes that you have already multiplied the RGB values of the Material by the alpha channel. This gives better results than **Alpha** blending when filtering images or composing different layers. |
| **Preserve specular lighting**        | Enable the checkbox to make alpha blending not reduce the intensity of specular highlights. This preserves the specular elements on the transparent surface, such as sunbeams shining off glass or water. |
| **Sorting Priority**                  | Allows you to change the rendering order of overlaid transparent surfaces. For more information and an example of usage, see the [Material sorting documentation](Renderer-And-Material-Priority.md#SortingByMaterial). |
| **Receive fog**                       | Enable the checkbox to allow fog to affect the transparent surface. When disabled, HDRP does not take this Material into account when it calculates the fog in the Scene. |
| **Back Then Front Rendering**         | Enable the checkbox to make HDRP render this Material in two separate draw calls. HDRP renders the back face in the first draw call and the front face in the second. |
| **Transparent depth prepass**         | Enable the checkbox to add polygons from the transparent surface to the depth buffer to improve their sorting. HDRP performs this operation before the transparent lighting pass. Not supported when rendering pass is Low Resolution. |
| **Transparent depth postpass**        | Enable the checkbox to add polygons from the transparent surface to the depth buffer so they affect post-processing. HDRP performs this operation after the lighting pass. Enabling this feature is useful when using post-processing effects that use depth information, like [motion blur](Post-Processing-Motion-Blur.md) or [depth of field](Post-Processing-Depth-of-Field.md). Not supported when rendering pass is Low Resolution. |
| **Transparent Writes Motion Vectors** | Enable the checkbox to make HDRP write motion vectors for transparent GameObjects that use this Material. This allows HDRP to process effects like motion blur for transparent objects. For more information on motion vectors, see the [motion vectors documentation](Motion-Vectors.md). Not supported when rendering pass is Low Resolution. |
| **Depth Write**                       | Enable the checkbox to make HDRP write depth values for transparent GameObjects that use this Material. Not supported when rendering pass is Low Resolution. |
| **Depth Test**                        | Use the drop-down to select the comparison function to use for the depth test. |
| **Cull Mode**                         | Use the drop-down to select the face to cull for transparent GameObjects that use this Material.<br/>&#8226; **Front**: Culls the front face of the GameObject's Mesh.<br/>&#8226; **Back**: Culls the back face of the GameObject's Mesh. |

<a name="TransparencyInputs"></a>

### Transparency Inputs

To expose this section in the Material Inspector, set the **Surface Type** to **Transparent**.

| **Property**                          | **Description**                                              |
| ------------------------------------- | ------------------------------------------------------------ |
| **Refraction Model**                  | Select the model that HDRP uses to process refraction. See [Set the approximate shape of a refractive object ](refraction-use.md#set-shape).<br />&#8226; **None**: Disable refraction.<br />&#8226; **Planar**:  Calculate refraction by approximating the interior of the object as the area between two parallel planes. Select this option for objects where the entry and exit surfaces are parallel, for example hollow objects. See [planar refraction model](refraction-models.md#sphere-refraction-model) .<br />&#8226; **Sphere**: Calculate refraction by approximating the interior of the object as a sphere. Select this option for organic, solid, convex objects. See [sphere refraction model](refraction-models.md#planar-refraction-model).<br />&#8226; **Thin**: This is the same as **Planar**, but HDRP sets **Refraction Thickness** at 5mm. Use the thin refraction model with thin objects if you use the [Path Tracing Volume Override](Ray-Tracing-Path-Tracing.md). See [thin refraction model](refraction-models.md#thin-refraction-model). |
| **Index of Refraction**               | Set the index of refraction for this Material. Different real-world materials have different indices of refraction. For example, water has an index of refraction of 1.33. See [Set the index of refraction](refraction-use.md#set-ior).<br />This property appears only if you select **Planar**, **Sphere** or **Thin** as the **Refraction Model**. |
| **Refraction Thickness**              | Set the thickness of the refractive object in meters. The higher the value, the more visible the effect is. See [Set the approximate shape of a refractive object ](refraction-use.md#set-shape). <br />This property appears only if you select **Planar** or **Sphere** as the **Refraction Model**. If you select **Thin** as the **Refraction Model**, HDRP sets **Refraction Thickness** as 5mm. |
| **Refraction Thickness Map**          | Assign a texture that controls the thickness of the object for each pixel. See [Set the approximate shape of a refractive object ](refraction-use.md#set-shape).<br />This property appears only if you select **Planar** or **Sphere** as the **Refraction Model**. |
| **Thickness Remapping**               | Remap and adjust the minimum and maximum **Thickness Map** values, in meters. <br/>This property only appears if you provide a **Thickness Map**. |
| **Transmittance Color**               | Refractive Materials can colorize light that passes through them. Assign a Texture to handle this colorization on a per pixel basis, or use the color picker to set a global color. If you assign a Texture and set a color, the final color of the Material is a combination of the Texture you assign and the color you select. See [Set color tint and light absorption](refraction-use.md#set-absorption)<br>This property appears only if you select **Planar**, **Sphere** or **Thin** as the **Refraction Model**. |
| **Transmittance Absorption Distance** | Set the distance of the object at which the **Transmittance Color** affects light passing through this Material at full strength. See [Set color tint and light absorption](refraction-use.md#set-absorption).<br>This property appears only if you select **Planar** or **Sphere** the **Refraction Model**. If you select **Thin** as the **Refraction Model**, HDRP sets **Transmittance Absorption Distance** as 5mm. |
