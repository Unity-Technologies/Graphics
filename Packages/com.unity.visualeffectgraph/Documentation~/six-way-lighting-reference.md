# Six-way smoke lit reference

Learn about the available properties to configure smoke rendering.

These properties let you adjust lighting, animation, color, and performance.

The **Smoke Shader UI** contains the following properties:

## Lightmap Remapping

The **Lightmap Remapping** section contains the following properties:

| **Property**         | Description                                                                                                                                                                                                                                          |
| :-------------------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Lightmap Remap Mode (dropdown)** | Select how to remap lightmap values for the smoke. The following options are available:<ul><li>**None**: Map lightmap values directly, with no remapping.</li><li>**Parametric Contrast**: Adjust lightmap contrast using parameter controls to boost or soften lighting transitions.</li><li>**Custom Curve**: Define a custom remap curve for precise control over lighting effects on the smoke.</li></ul> |
| **Lightmap Remap Range**     | Set the input/output range for lightmap remapping, clamping or stretching light values as needed.                                                                                                            |
| **Use Alpha Remap**          | Enable remapping of alpha values based on lighting to control transparency dynamically.                                                                                |

## Emissive

The **Emissive** section contains the following properties:

| **Property**       | Description                                                                                                                                                                                                                                            |
| :------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Emissive Mode (dropdown)** | Choose how the smoke emits light. The following options are available:<ul><li>**None**: Disable emissive contribution, so smoke is only lit by external lights.</li><li>**Single Channel**: Use one channel to define emissive intensity, typically as a grayscale value.</li><li>**Map**: Apply a texture map to control emissive values for spatially varying glow.</li></ul> |

## Color and Alpha

The **Color and Alpha** section contains the following properties:

| **Property**       | Description                                                                                                                                                                                                                                                                                                                                                                          |
| :------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Use Color Absorption**   | Enable simulation of color absorption to let smoke filter and tint passing light.                                                                                                                                                                                                                                                             |
| **Receive Shadows**        | Allow smoke to receive shadows from other objects for added realism.                                                                                                                                                                                                                                   |
| **Enable Cookie**          | Apply light cookies (patterned masks) to affect light interaction with the smoke.                                                                                                                                                                                                                                                            |
| **Color Mode (dropdown)**  | Control how color data is interpreted for rendering. The following options are available:<ul><li>**None**: Use base color directly with no color processing.</li><li>**Everything**: Apply color processing to all channels.</li><li>**Base Color**: Process only the base color channel.</li><li>**Emissive**: Process only the emissive channel.</li><li>**Base Color And Emissive**: Process both base color and emissive channels.</li></ul> |
| **Use Base Color Map**     | Enable a texture map for base color to allow detailed color variation within the smoke.                                                                                                                                                                                                                |
| **Base Color Map Mode (dropdown)** | Select which channels to use from the base color map. The following options are available:<ul><li>**None**: Apply no base color map.</li><li>**Everything**: Use all color channels from the base color map.</li><li>**Color**: Use only color channels (RGB) from the map.</li><li>**Alpha**: Use only the alpha channel from the map for transparency.</li><li>**Color And Alpha**: Use both color and alpha channels from the map.</li></ul> |

## Texture and UV

The **Texture and UV** section contains the following properties:

| **Property**       | Description                                                                                                                                                                                                                                            |
| :------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Uv Mode (dropdown)**     | Specify how UV coordinates are generated or used. The following options are available:<ul><li>**Default**: Apply standard UV mapping for the texture.</li><li>**Flipbook**: Use flipbook animation for texture UVs, ideal for animated smoke sequences.</li><li>**Scale And Bias**: Scale and offset UVs for texture manipulation.</li></ul> |
| **Flipbook Layout (dropdown)** | Define the layout of flipbook frames for animated textures. The following options are available:<ul><li>**Texture 2D**: Use a standard 2D texture for smoke rendering.</li><li>**Texture 2D Array**: Use an array of 2D textures for complex animations or variations.</li></ul> |
| **Base Color**             | Set the base color for the smoke, as a constant or via a texture.                                                                                                                                                                                    |
| **Flipbook**               | Specify the flipbook asset or parameters for animated texture sequences.                                                                                                                                                                              |
| **Flipbook Blend Frames**  | Control blending between flipbook frames for smooth animation.                                                                                                                                                                                        |

## Particle Rendering

The **Particle Rendering** section contains the following properties:

| **Property**       | Description                                                                                                                                                                                                                                            |
| :------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Use Soft Particle**      | Enable soft particle rendering to fade smoke edges near intersecting geometry for seamless integration.                                                                                                                                               |
| **Sort (dropdown)**        | Control particle sorting for correct transparency rendering. The following options are available:<ul><li>**Auto**: Automatically determine the best sorting mode.</li><li>**Off**: Disable sorting, which might cause visual artifacts in transparency.</li><li>**On**: Enable sorting for correct draw order.</li></ul> |
| **Sort Mode (dropdown)**   | Specify sorting algorithm for particles. The following options are available:<ul><li>**Distance To Camera**: Sort by distance to the camera, common for transparency.</li><li>**Youngest In Front**: Sort by particle age, with newest particles in front.</li><li>**Camera Depth**: Sort by camera depth buffer.</li><li>**Custom**: Use a custom sorting function.</li></ul> |
| **Revert Sorting**         | Reverse the sorting order when needed.                                                                                                                                                                                                                |
| **Compute Culling**        | Enable GPU culling of particles outside the view frustum for performance.                                                                                                                                                                            |
| **Frustum Culling**        | Cull smoke particles outside the camera frustum to optimize rendering.                                                                                                                                                                                |
| **Cast Shadows**           | Enable smoke particles to cast shadows onto other geometry.                                                                                                                                                                                          |

## Render States

The **Render States** section contains the following properties:

| **Property**           | Description                                                                                                                                                                                                                                   |
| :---------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Render States**              | Control advanced render state overrides for the smoke material.                                                                                                                                                                              |
| **Blend Mode (dropdown)**      | Set how smoke blends with the background. The following options are available:<ul><li>**Additive**: Add smoke color to the background, ideal for light or glow effects.</li><li>**Alpha**: Use standard alpha blending for semi-transparent smoke.</li><li>**Alpha Premultiplied**: Use premultiplied alpha for improved compositing.</li><li>**Opaque**: Render smoke as fully opaque, with no blending.</li></ul> |
| **Cull Mode (dropdown)**       | Determine which faces are rendered. The following options are available:<ul><li>**Default**: Apply standard culling (usually back-face).</li><li>**Front**: Render only front faces.</li><li>**Back**: Render only back faces.</li><li>**Off**: Disable face culling to render all faces.</li></ul> |
| **Z Write Mode (dropdown)**    | Control writing to the depth buffer (Z). The following options are available:<ul><li>**Default**: Apply standard Z write behavior.</li><li>**Off**: Disable Z write, useful for transparency.</li><li>**On**: Enable Z write when needed for certain effects.</li></ul> |
| **Z Test Mode (dropdown)**     | Set the depth test function for rendering order. The following options are available:<ul><li>**Default**: Use the default depth test (usually Less).</li><li>**Less**: Pass if incoming depth is less than stored.</li><li>**Greater**: Pass if incoming depth is greater than stored.</li><li>**L Equal**: Pass if less than or equal to stored.</li><li>**G Equal**: Pass if greater than or equal to stored.</li><li>**Equal**: Pass if depths are equal to stored.</li><li>**Not Equal**: Pass if depths are not equal to stored.</li><li>**Always**: Always pass, with no depth test.</li></ul> |

## Alpha and Motion

The **Alpha and Motion** section contains the following properties:

| **Property**        | Description                                                                                                  |
| :------------------------- | :----------------------------------------------------------------------------------------------------------- |
| **Alpha**                  | Set a global alpha value for smoke to control overall transparency.                                          |
| **Use Alpha Clipping**     | Enable alpha clipping to discard pixels below a certain alpha threshold for hard edges.                      |
| **Generate Motion Vector** | Enable motion vector output for the smoke to support motion blur.                                            |
| **Exclude From TU And A**  | Exclude smoke from certain rendering passes, such as temporal upscaling or anti-aliasing.                    |
| **Sorting Priority**       | Set render queue priority to control draw order relative to other transparent objects.                        |
