## Local Volumetric Fog Volume reference

The Local Volumetric Fog Volume component lets you configure a local fog effect.

Refer to [Create a local fog effect](create-a-local-fog-effect.md) for more information. 

## Properties

| Property                     | Description                                                  |
| :--------------------------- | :----------------------------------------------------------- |
| **Single Scattering Albedo** | Sets the fog color.<br/> Volumetric Fog tints lighting as the light scatters to appear this color. It only tints lighting emitted by Lights behind or within the fog. This means that it does not tint lighting that reflects off GameObjects behind or within the fog. Reflected lighting gets dimmer (fades to black) as fog density increases.<br/>For example, if you shine a Light at a white wall behind fog with red Single Scattering Albedo, the fog looks red. If you shine a Light at a white wall and view it from the other side of the fog, the fog darkens the light but doesn’t tint it red. |
| **Fog Distance**             | Controls the density at the base of the fog and determines how far you can see through the fog in meters. At this distance, the fog has absorbed and out-scattered 63% of background light. |
| **Mask Mode**                | Select a mask type to apply to the fog: <br/>&#8226;**Texture**: Applies a 3D texture to the fog volume.<br/>&#8226;**Material**: Applies a Material to the fog which updates every frame. You can use this to create dynamic fog effects. |
| **Blending Mode**            | Determines how this fog volume blends with existing fog in the scene:<br/>&#8226;**Overwrite**: Replaces existing fog in the volume area with this fog volume. <br/>&#8226;**Additive**: Adds the color and density of this fog volume to other fog in the scene. This is the default value.<br/>&#8226;**Multiply**: Multiplies the color and density of this fog volume with other fog in the scene. You can use this to create effects relative to a specific fog density.<br/>&#8226;**Min**: Determines the minimum density value of this fog volume and the scene fog inside its bounding box. For example, a value of 0 appears to remove fog in a certain area.<br/>&#8226;**Max**: Determines the maximum density value of this fog volume and the scene fog inside its bounding box.<br/> |
| **Priority**                 | Determined the order in which HDRP blends fog volumes. HDRP renders the lowest priority volume first and the highest priority last. |
| **Scale Mode**               | Specifies the scaling mode to apply to the Local Volumetric Fog Volume. Invariant uses only the size of the volume. Inherit From Hierarchy multiplies the size and the transform scale. |
| **Size**                     | Controls the dimensions of the Volume.                       |
| **Per Axis Control**         | Controls the blend distance based on each axis.              |
| **Blend Distance**           | Blend Distance fades from this fog volume's level to the fog level outside it. <br/>This value indicates the absolute distance from the edge of the Volume bounds, defined by the Size property, where the fade starts.<br/>Unity clamps this value between 0 and half of the lowest axis value in the Size property.<br/>If you use the **Normal** tab, you can alter a single float value named Blend Distance, which gives a uniform fade in every direction. If you open the **Advanced** tab, you can use two fades per axis, one for each direction. For example, on the X-axis you could have one for left-to-right and one for right-to-left.<br/>A value of 0 hides the fade, and a value of 1 creates a fade. |
| **Falloff Mode**             | Controls the falloff function applied to the blending of **Blend Distance**. By default the falloff is linear but you can change it to exponential for a more realistic look. |
| **Invert Blend**             | Reverses the direction of the fade. Setting the Blend Distances on each axis to its maximum possible value preserves the fog at the center of the Volume and fades the edges. Inverting the blend fades the center and preserves the edges instead. |
| **Distance Fade Start**      | Distance from the camera at which the Local Volumetric Fog starts to fade out. Use this property to optimize a scene with a lot of Local Volumetric Fog. |
| **Distance Fade End**        | Distance from the camera at which the Local Volumetric Fog completely fades out. Use this property to optimize a scene with a lot of Local Volumetric Fog. |
| **Density Mask Texture**     | Specifies a 3D texture mapped to the interior of the Volume. Local Volumetric Fog only uses the RGB channels of the texture for the fog color and A for the fog density multiplier. A value of 0 in the Texture alpha channel results in a Volume of 0 density, and the value of 1 results in the original constant (homogeneous) volume. |
| **Scroll Speed**             | Specifies the speed (per-axis) at which the Local Volumetric Fog scrolls the texture. If you set every axis to 0, the Local Volumetric Fog doesn't scroll the texture and the fog is static. |
| **Tiling**                   | Specifies the per-axis tiling rate of the texture. For example, setting the x-axis component to 2 means that the texture repeats 2 times on the x-axis within the interior of the volume. |
| **Material**                 | The volumetric material mask, this material needs to have a Shader Graph with the material type **Fog Volume**. |

## Volumetric Fog properties in the HDRP Asset

The [HDRP Asset](HDRP-Asset.md) contains the following properties that relate to Local Volumetric Fog (menu: **Project** > **Assets** > **HD Render Pipeline Asset** > **Lighting** > **Volumetrics)**:

| Property   | Description  |
|---|---|
| **Volumetric Fog** | Enable or disable volumetric fog. |
| **Max Local Volumetric Fog On Screen**  | Control how many Local Volumetric Fog components can appear on-screen at once. This setting has an impact on performance which increases at high values. |