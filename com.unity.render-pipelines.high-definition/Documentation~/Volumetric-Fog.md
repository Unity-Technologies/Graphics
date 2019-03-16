# Volumetric Fog

Volumetric fog is the most advanced implementation of fog available in the High Definition Render Pipeline (HDRP). It realistically simulates the interaction of lights with fog, which allows for physically-plausible rendering of glow and crepuscular rays, which are beams of light that stream through gaps in objects like clouds and trees from a central point, like a God ray from the Sun..

## Adding Fog to your Scene

To use volumetric fog in your Scene, create a Scene Settings GameObect (menu: __GameObject > Rendering > Scene Settings__). This contains a Volume component that has a [Visual Environment](Visual-Environment.html) override that you can use to override HDRP’s default environment settings.

Configure the Visual Environment override so that it uses __Volumetric Fog__ as its __Fog Type__.

![](Images/VolumetricFog1.png)

Now add a __Volumetric Fog__ override to the Volume. Click __Add Override__ and then click __Volumetric Fog__.

Within the Scene, there is usually a single Volume set to __IsGlobal__ that contains a Visual Environment override. Having a single global Visual Environment means that Unity uses the same __Sky Type__ and __Fog Type__ everywhere in the Scene. You can still use local Volumes with different __Sky Types__ and __Fog Types__, but the transition between them is obvious and instantaneous. If you want to use multiple Visual Environments in different Volumes in your Scene, it is best to make the transition on Camera cuts.

At this point, the Scene contains global volumetric fog. However, the effect is not visible because the default global fog density is very low. To override the default property with your own chosen values, follow the steps in the Customizing Global Volumetric Fog section.

## Customizing Global Volumetric Fog

Use global fog, rather than local fog, because it provides the best performance and the best quality.

Global fog is a height fog. It has two logical components: the region at a distance closer to the Camera than the __Base Height__ is a constant (homogeneous) fog, and the region at a distance further than the __Base Height__ is the exponential fog.

The __Volumetric Fog__ component of the active Volume controls the appearance of the global fog.

![](Images/VolumetricFog2.png)

__Volumetric Fog__ properties:

| Property                 | Function                                                     |
| :----------------------- | :----------------------------------------------------------- |
| **Single Scattering Albedo** | Sets the fog color. Volumetric Fog tints lighting, so the fog scatters light to this color. It only tints lighting emitted by Lights behind or within the fog. This means that it does not tint lighting that reflects off GameObjects behind or within the fog - reflected lighting only gets dimmer (fades to black) as fog density increases. For example, if you shine a Light at a white wall behind fog with red Single Scattering Albedo, the fog looks red. If you shine a Light at a white wall and view it from the other side of the fog, the fog darkens the light but doesn’t tint it red. |
| **Base Fog Distance**    | Controls the density at the base of the fog and determines how far you can see through the fog in Unity units. At this distance, the fog has absorbed and out-scattered 63% of background light. |
| **Base Height**          | The height of the boundary between the constant (homogeneous) fog and the exponential fog. |
| **Mean Height**          | Controls the rate of falloff for the height fog in Unity units. Higher values stretch the fog vertically. At this height , the falloff reduces the initial base density by 63%. |
| **Global Anisotropy** | Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering. Note that non-zero values have a moderate performance impact. High values may have compatibility issues with the Enable Reprojection for Volumetrics Frame Setting. This is an experimental property that HDRP applies to both global and local fog. |
| **Global Light Probe Dimmer** | Reduces the intensity of the global Light Probe that the sky generates. |
| **Max Fog Distance** | Controls the distance (in Unity units) when applying fog to the skybox or background. Also determines the range of the Distant Fog. For optimal results, set this to be larger than the Camera’s Far value for its Clipping Plane. Otherwise, a discrepancy occurs between the fog on the Scene’s GameObjects and on the skybox. Note that the Camera’s Far Clipping Plane is flat whereas HDRP applies fog within a sphere surrounding the Camera. |
| **Distant Fog** | Activates the fog with precomputed lighting behind the volumetric section of the Camera’s frustum. The fog stretches from the maximum Distance Range in the Volumetric Lighting Controller to the Max Fog Distance. |
| **Color Mode**              | Provides two methods HDRP can use to set the lighting intensity of the distant fog. This property is only visible when you enable the Distant Fog checkbox. |
| **- Constant Color**    | HDRP illuminates distant fog uniformly using the provided Color property. |
| **- - Color**           | The color that HDRP uses to illuminate the distant fog.      |
| **- Sky Color**         | HDRP illuminates distant fog depending on the skybox, the view direction, and the distance. The cubemap of the sky provides all lighting. |
| **-- Mip Fog Near**     | Determines the distance (in Unity units) at which HDRP uses the least detailed MIP level. |
| **- - Mip Fog Far**     | Determines the distance (in Unity units) at which HDRP uses the most detailed MIP level. |
| **- - Mip Fog Max Mip** | Determines the most detailed MIP level HDRP uses for MIP fog. |



## Adding Local Fog

You may want to have fog effects in your Scene that global fog can not produce by itself. In these cases you can use local fog. To add localized fog, use a Density Volume. A Density Volume is a an additive Volume of fog represented as an oriented bounding box. By default, fog is constant (homogeneous), but you can alter it by assigning a Density Mask 3D texture to the __Texture__ field under the __Density Mask Texture__ section. Currently, HDRP supports 3D textures at a resolution of 32x32x32.

HDRP voxelizes Density Volumes to enhance performance. This results in two limitations:

- Density Volumes do not support volumetric shadowing. If you place a Density Volume between a Light and a surface, the Volume does not decrease the intensity of light that reaches the surface.
- Density Volumes are voxelized at a very coarse rate, with typically only 64 or 128 slices along the camera's focal axis. This can cause noticeable aliasing at the boundary of the Volume. You can hide the aliasing by using Density Volumes in conjunction with some global fog, if possible. You can also use a Density Mask and a non-zero Blend Distance to decrease the hardness of the edge.

To create a Density Volume, right click in the Hierarchy and select __Rendering > Density Volume__. Alternatively, you can use the menu bar at the top of the screen and navigate to __GameObject > Rendering > Density Volume__.

![](Images/VolumetricFog2.png)

### Properties

| Property                 | Description                                                  |
| :----------------------- | :----------------------------------------------------------- |
| **Single Scattering Albedo** | Sets the fog color. Volumetric Fog tints lighting, so the fog scatters light to this color. It only tints lighting emitted by Lights behind or within the fog. This means that it does not tint lighting that reflects off GameObjects behind or within the fog - reflected lighting only gets dimmer (fades to black) as fog density increases. For example, if you shine a Light at a white wall behind fog with red Single Scattering Albedo, the fog looks red. If you shine a Light at a white wall and view it from the other side of the fog, the fog darkens the light but doesn’t tint it red. |
| **Fog Distance**         | Controls the density at the base of the fog and determines how far you can see through the fog in Unity units. At this distance, the fog has absorbed and out-scattered 63% of background light. |
| **Size** | Controls the dimensions of the Volume.|
| **Blend Distance** | Blend Distance creates a linear fade from the fog level in the Volume to the fog level outside it. This is not a percentage, it is the absolute distance from the edge of the Volume bounds, defined by the Size property, where the fade starts. Unity clamps this value between 0 and half of the lowest axis value in the Size property. If you use the Normal tab, you can alter a single float value named Blend Distance, which gives a uniform fade in every direction. If you open the Advanced tab, you can use two fades per axis, one for each direction. For example, on the x-axis you could have one for left-to-right and one for right-to-left. Setting the distance to 0 hides the fade, while setting the distance to 1 creates a fade. |
| **Invert Blend** | Reverses the direction of the fade. Setting the Blend Distances on each axis to its maximum possible value preserves the fog at the center of the Volume and fades the edges. Inverting the blend fades the center and preserves the edges instead. |
| **Density Mask Texture** | Specifies a 3D texture mapped to the interior of the Volume. The Density Volume only uses the alpha channel of the texture. The value of the texture acts as a density multiplier. A value of 0 in the Texture results in a Volume of 0 density, and the texture value of 1 results in the original constant (homogeneous) volume. |
| **Scroll Speed** | Specifies the speed (per-axis) at which the Density Volume scrolls the texture. If you set every axis to 0, the Density Volume does not scroll the texture and the fog is static. |
| **Tiling** | Specifies the per-axis tiling rate of the texture. For example, setting the x-axis component to 2 means that the texture repeats 2 times on the x-axis within the interior of the volume. |



## Creating a Density Mask Texture

1. In image-editing software of your choice, prepare a grayscale 2D texture with values between 0 and 1 and of size 1024x32. This size describes a 3D texture of size 32x32x32 with 32 slices laid out one after another.
2. Import this texture into the Unity Editor and set the Texture Import Settings:

2.1. Set the __Texture Type__ to __Single Channel__.

2.2. Set the __Channel__ to __Alpha__.

2.3. Enable the __Read/Write Enabled__ checkbox.

3. Navigate to __Window > Rendering > Density Volume Texture Tool__.
4. Drag and drop your texture into the __Slice Texture__ field and set the __Texture Slice Size__ to 32.
3. Click __Create 3D Texture__ and the tool generates another texture with a different format that you can use as a Density Mask.
4. Open a Density Volume component and assign the texture you just generated to the __Texture__ field in the __Density Mask Texture__ section.

## Light-specific Properties

The [Light component](Light-Component.html) has several properties that are useful for volumetric lighting:

- __Emission Radius__ is useful to simulate fill lighting. It acts by virtually "pushing" the light away from the Scene. As a result, it softens the core of punctual lights. Always use a non-zero value to reduce ghosting artifacts resulting from reprojection.
- __Volumetric Dimmer__ only affects the fog and replaces the Light Dimmer that HDRP uses for surfaces.
- __Shadow Dimmer__ only affects the fog and replaces the Shadow Dimmer that HDRP uses for surfaces.

