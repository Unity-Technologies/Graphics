# Surface Type

The **Surface Type** option controls whether your Material supports transparency or not. Each **Surface Type** has a different workflow and so use the **Surface Type** that is most suitable for the Material you are creating. 

| **Surface Type** | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Opaque**       | Simulates a completely solid Material with no light penetration. |
| **Transparent**  | Simulates a translucent Material that light can penetrate, such as clear plastic or glass. Selecting **Transparent** exposes more properties in the **Surface Options** section as well as an extra [Transparency Inputs](#TransparencyInputs) section. |

Materials with **Transparent Surface Types** are more resource intensive to render than Materials with an **Opaque Surface Type**.

## Properties

If you set the **Surface Type** to **Transparent**, HDRP exposes options to set the **Blending Mode** and other properties relating to transparency. 

### Surface Options

| Property                              | Description                                                  |
| ------------------------------------- | ------------------------------------------------------------ |
| **Rendering Pass**                    | Use the drop-down to set the rendering pass that HDRP processes this Material in.<br />&#8226; **Before Refraction**: Draws the GameObject before the refraction pass. This means that HDRP includes this Material when it processes refraction. To expose this option, select **Transparent** from the **Surface Type** drop-down.<br />&#8226; **Default**: Draws the GameObject in the default opaque or transparent rendering pass pass, depending on the **Surface Type**.<br />&#8226; **Low Resolution**: Draws the GameObject in half resolution after the **Default** pass.<br />&#8226; **After post-process**: For [Unlit Materials](Unlit-Shader.html) only. Draws the GameObject after all post-processing effects. |
| **Blending Mode**                     | Use the drop-down to determine how HDRP calculates the color of each pixel of the transparent Material by blending the Material with the background pixels.<br />&#8226; **Alpha**: Uses the Material’s alpha value to change how transparent an object is. 0 is fully transparent. 1 appears fully opaque, but the Material is still rendered during the Transparent render pass. This is useful for visuals that you want to be fully visible but to also fade over time, like clouds.<br />&#8226; **Additive**: Adds the Material’s RGB values to the background color. The alpha channel of the Material modulates the intensity. A value of 0 adds nothing and a value of 1 adds 100% of the Material color to the background color.<br />&#8226; **Premultiply**: Assumes that you have already multiplied the RGB values of the Material by the alpha channel. This gives better results than **Alpha** blending when filtering images or composing different layers. |
| **Preserve specular lighting**        | Enable the checkbox to make alpha blending not reduce the intensity of specular highlights. This preserves the specular elements on the transparent surface, such as sunbeams shining off glass or water. |
| **Sorting Priority**                  | Allows you to change the rendering order of overlaid transparent surfaces. For more information and an example of usage, see the [Material sorting documentation](Renderer-And-Material-Priority.html#SortingByMaterial). |
| **Receive fog**                       | Enable the checkbox to allow fog to affect the transparent surface. When disabled, HDRP does not take this Material into account when it calculates the fog in the Scene. |
| **Back Then Front Rendering**         | Enable the checkbox to make HDRP render this Material in two separate draw calls. HDRP renders the back face in the first draw call and the front face in the second. |
| **Transparent depth prepass**         | Enable the checkbox to add polygons from the transparent surface to the depth buffer to improve their sorting. HDRP performs this operation before the lighting pass and this process improves GPU performance. |
| **Transparent depth postpass**        | Enable the checkbox to add polygons to the depth buffer that post-processing uses. HDRP performs this operation before the lighting pass. Enabling this feature is useful if you want to use post-processing effects that use depth information, like [motion blur](Post-Processing-Motion-Blur.html) or [depth of field](Post-Processing-Depth-of-Field.html). |
| **Transparent Writes Motion Vectors** | Enable the checkbox to make HDRP write motion vectors for transparent GameObjects that use this Material. This allows HDRP to process effects like motion blur for transparent objects. For more information on motion vectors, see the [motion vectors documentation](Motion-Vectors.html). |
| **Depth Write**                       | Enable the checkbox to make HDRP write depth values for transparent GameObjects that use this Material. |
| **Depth Test**                        | Use the drop-down to select the comparison function to use for the depth test. |
| **Cull Mode**                         | Use the drop-down to select the face to cull for transparent GameObjects that use this Material.<br/>&#8226; **Front**: Culls the front face of the GameObject's Mesh.<br/>&#8226; **Back**: Culls the back face of the GameObject's Mesh. |

<a name="TransparencyInputs"></a>

### Transparency Inputs

To expose this section in the Material Inspector, set the **Surface Type** to **Transparent**.

| **Property**                          | **Description**                                              |
| ------------------------------------- | ------------------------------------------------------------ |
| **Refraction Model**                  | Use the drop-down to select the model that HDRP uses to process refraction.<br />&#8226; **None**: No refraction occurs. Select this option to disable refraction.<br />&#8226; **Box**: A box-shaped model where incident light enters through a flat surface and leaves through a flat surface. Select this option for hollow surfaces.<br />&#8226; **Sphere**: A sphere-shaped model that produces a magnifying glass-like effect to refraction. Select this option for solid surfaces.<br />&#8226; **Thin**: A thin box surface type, equivalent to Box with a fixed thickness of 5cm. Select this for thin window-like surfaces. |
| **Index of Refraction**               | Use the slider to set the index of refraction for this Material. The index of refraction defines the ratio between the speed of light in a vacuum and the speed of light in the medium of the Material. Higher values produce more intense refraction.<br />This property only appears when you select **Box**, **Sphere** or **Thin** from the **Refraction Model** drop-down. |
| **Refraction Thickness**              | Use the slider to set the overall thickness of the refractive object between 0 and 1. The thicker the Material, the more visible the effect is.<br /> This property only appears when you select **Box** or **Sphere** from the **Refraction Model** drop-down. For the **Thin** model it is implied to be 5cm. |
| **Refraction Thickness Map**          | Assign a Texture that defines the thickness map for this Material. This map controls the thickness of the object on a per-pixel level.<br />This property only appears when you select **Box** or **Sphere** from the **Refraction Model** drop-down. |
| **Transmittance Color**               | Refractive Materials can colorize light which passes through them. Assign a Texture to handle this colorization on a per pixel basis. Use the color picker to set a global color to handle the colorization. If you assign a Texture and set a color, the final color of the Material is a combination of the Texture you assign and the color you select.<br />This property only appears when you select **Box**, **Sphere** or **Thin** from the **Refraction Model** drop-down. |
| **Transmittance Absorption Distance** | Set the thickness of the object at which the **Transmittance Color** affects incident light at full strength.<br />This property only appears when you select **Box** or **Sphere** from the **Refraction Model** drop-down. For the **Thin** model it is implied to be 5cm. |
| **Distortion**                        | Enable the checkbox to distort the light passing through this transparent Material. When enabled, this exposes the following properties. <br />This property is available only for [Unlit Materials](Unlit-Shader.html). |
| **Distortion Blend Mode**             | Set the mode HDRP uses to blend overlaid distortion surfaces.<br />This property is available only for Unlit materials. |
| **Distortion Depth Test**             | Check this box to make GameObjects that are closer to the Camera hide the distortion effect, otherwise you can always see the effect. If you do not enable this feature then the distortion effect appears on top of the rendering.<br />This property is available only for [Unlit Materials](Unlit-Shader.html). |
| **Distortion Vector Map**             | Make HDRP use the red and green channels of this Texture to calculate distortion for the light passing through the Material. HDRP also uses the blue channel to manage the blur intensity between 0 and 1. By default, a texture has values between 0 and 1. To be able to produce distortion in either direction, you must remap the distortion texture between -1 and 1. HDRP provides two values you can use to remap this distortion texture. It takes the original value from the map and multiplies it by the value on the left then adds the value on the right. For example, to remap the original values, from 0 to 1, to -1 to 1, enter 2 for the first value and -1 for the second value.<br />This property is available only for [Unlit Materials](Unlit-Shader.html). |
| **Distortion Scale**                  | A multiplier for the distortion effect on the light passing through the Material. Set this to a value higher than 1 to amplify the effect.<br />This property is available only for [Unlit Materials](Unlit-Shader.html). |
| **Distortion Blur Scale**             | A multiplier for the distortion blur. Set this to a value higher than 1 to amplify the blur.<br />This property is available only for [Unlit Materials](Unlit-Shader.html). |
| **Distortion Blur Remapping**         | Use this handle to clamp the values of the blue channel of the Distortion Vector Map. Use this to refine the blur setting.<br />This property is available only for [Unlit Materials](Unlit-Shader.html). |

