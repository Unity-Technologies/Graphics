## Density Volume

You may want to have fog effects in your Scene that global fog can not produce by itself. In these cases you can use local fog. To add localized fog, use a Density Volume. A Density Volume is a an additive Volume of fog represented as an oriented bounding box. By default, fog is constant (homogeneous), but you can alter it by assigning a Density Mask 3D texture to the __Texture__ field under the __Density Mask Texture__ section. Currently, HDRP supports 3D textures at a resolution of 32x32x32.

HDRP voxelizes Density Volumes to enhance performance. This results in two limitations:

- Density Volumes do not support volumetric shadowing. If you place a Density Volume between a Light and a surface, the Volume does not decrease the intensity of light that reaches the surface.
- Density Volumes are voxelized at a very coarse rate, with typically only 64 or 128 slices along the camera's focal axis. This can cause noticeable aliasing at the boundary of the Volume. You can hide the aliasing by using Density Volumes in conjunction with some global fog, if possible. You can also use a Density Mask and a non-zero Blend Distance to decrease the hardness of the edge.

To create a Density Volume, right click in the Hierarchy and select __Rendering > Density Volume__. Alternatively, you can use the menu bar at the top of the screen and navigate to __GameObject > Rendering > Density Volume__.


# Properties

![](Images/DensityVolume1.png)



| Property                     | Description                                                  |
| :--------------------------- | :----------------------------------------------------------- |
| **Single Scattering Albedo** | Sets the fog color. Volumetric Fog tints lighting, so the fog scatters light to this color. It only tints lighting emitted by Lights behind or within the fog. This means that it does not tint lighting that reflects off GameObjects behind or within the fog - reflected lighting only gets dimmer (fades to black) as fog density increases. For example, if you shine a Light at a white wall behind fog with red Single Scattering Albedo, the fog looks red. If you shine a Light at a white wall and view it from the other side of the fog, the fog darkens the light but doesn’t tint it red. |
| **Fog Distance**             | Controls the density at the base of the fog and determines how far you can see through the fog in Unity units. At this distance, the fog has absorbed and out-scattered 63% of background light. |
| **Size**                     | Controls the dimensions of the Volume.                       |
| **Blend Distance**           | Blend Distance creates a linear fade from the fog level in the Volume to the fog level outside it. This is not a percentage, it is the absolute distance from the edge of the Volume bounds, defined by the Size property, where the fade starts. Unity clamps this value between 0 and half of the lowest axis value in the Size property. If you use the Normal tab, you can alter a single float value named Blend Distance, which gives a uniform fade in every direction. If you open the Advanced tab, you can use two fades per axis, one for each direction. For example, on the x-axis you could have one for left-to-right and one for right-to-left. Setting the distance to 0 hides the fade, while setting the distance to 1 creates a fade. |
| **Invert Blend**             | Reverses the direction of the fade. Setting the Blend Distances on each axis to its maximum possible value preserves the fog at the center of the Volume and fades the edges. Inverting the blend fades the center and preserves the edges instead. |
| **Density Mask Texture**     | Specifies a 3D texture mapped to the interior of the Volume. The Density Volume only uses the alpha channel of the texture. The value of the texture acts as a density multiplier. A value of 0 in the Texture results in a Volume of 0 density, and the texture value of 1 results in the original constant (homogeneous) volume. |
| **Scroll Speed**             | Specifies the speed (per-axis) at which the Density Volume scrolls the texture. If you set every axis to 0, the Density Volume does not scroll the texture and the fog is static. |
| **Tiling**                   | Specifies the per-axis tiling rate of the texture. For example, setting the x-axis component to 2 means that the texture repeats 2 times on the x-axis within the interior of the volume. |



## Creating a Density Mask Texture

1. In image-editing software of your choice, prepare a grayscale 2D texture with values between 0 and 1 and of size 1024x32. This size describes a 3D texture of size 32x32x32 with 32 slices laid out one after another.
2. Import this texture into the Unity Editor and set the Texture Import Settings:

2.1. Set the __Texture Type__ to __Single Channel__.

2.2. Set the __Channel__ to __Alpha__.

2.3. Enable the __Read/Write Enabled__ checkbox.

3. Navigate to __Window > Rendering > Density Volume Texture Tool__.
4. Drag and drop your texture into the __Slice Texture__ field and set the __Texture Slice Size__ to 32.
5. Click __Create 3D Texture__ and the tool generates another texture with a different format that you can use as a Density Mask.
6. Open a Density Volume component and assign the texture you just generated to the __Texture__ field in the __Density Mask Texture__ section.
