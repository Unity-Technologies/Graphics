## Local Volumetric Fog

You may want to have fog effects in your Scene that global fog can not produce by itself. In these cases you can use local fog. To add localized fog, use a Local Volumetric Fog component. Local Volumetric Fog is a volume of fog represented as an oriented bounding box. By default, fog is constant (homogeneous), but you can alter it by assigning a Density Mask 3D texture to the **Texture** field under the **Density Mask Texture** section.

# Create a Local Volumetric Fog component 

You can create a Local Volumetric Fog component in one of the following ways:

1. In the menu bar, select **GameObject** > **Rendering** > **Local Volumetric Fog**.

2. Right-click in the Hierarchy window and select **Volume** > **Local Volumetric Fog**.

<a name="volumetric-fog-set-up"></a>

# Set up Volumetric fog
To use volumetric fog, enable it in the all of the following locations:
- [Project Settings](#enable-fog-project-settings)
- [HDRP Asset](#enable-fog-hdrp-asset)
- [Global Volume](#enable-fog-global-volume)

<a name="enable-fog-project-settings"></a>

## Enable Volumetric Fog in Project Settings

To enable Volumetric Fog in the Project Settings window, open the Project settings window (menu: **Edit > Project Settings)** and enable the following properties**:**

1. Go to **Quality** > **HDRP** > **Lighting** > **Volumetrics** and enable **Volumetric Fog**.
2. Go to **Graphics** > **HDRP Global Settings**. 
3. Under Frame Settings (Default Values), in the Camera section: 
   - Select **Lighting**.
   - Enable **Fog**.
   - Enable **Volumetrics**.

<a name="enable-fog-hdrp-asset"></a>

## Enable Volumetric Fog in the HDRP Asset

To enable Volumetric Fog in the [HDRP Asset](HDRP-Asset.md):

1. In the **Project** window, select the **HDRenderPipelineAsset** to open it in the Inspector**.**
2. Go to **Lighting** > **Volumetrics**.
3. Enable **Volumetric Fog**.

<a name="enable-fog-global-volume"></a>

## Enable Volumetric Fog in a Global Volume

To use volumetric fog in your scene, create a [Global volume](Volumes.md) with a [**Fog** component](Override-Fog.md). To do this: 

1. Create a new GameObject (menu: **GameObject** > **Create Empty**).
2. In the Inspector window, select **Add Component**.
3. In the search box that appears, enter “volume”.
4. Select the **Volume** Component.
5. Go to **Profile** and select **New** to create a volume profile.
6. Select **Add Override**.
7. In the search box that appears, select **Fog.**

To enable volumetric fog in the Fog component:

1. Select the **Fog** dropdown
2. Select the **Enable** toggle, and enable it.
3. Change the **State** setting to **Enabled**.
4. Select the **Volumetric Fog** toggle, and enable it.

To see more properties you can use to control Volumetric Fog, expose the hidden Fog settings: 

1. In the Volume component, go to the **Fog** override and select the **More** (**⋮**) dropdown.
2. Select **Show All Additional Properties** to open the Preferences window.
3. In the Preferences window, under Additional Properties, open the **Visibility** dropdown and select **All Visible.**

# Properties

| Property                     | Description                                                  |
| :--------------------------- | :----------------------------------------------------------- |
| **Single Scattering Albedo** | Sets the fog color.<br/> Volumetric Fog tints lighting as the light scatters to appear this color. It only tints lighting emitted by Lights behind or within the fog. This means that it does not tint lighting that reflects off GameObjects behind or within the fog. Reflected lighting gets dimmer (fades to black) as fog density increases.<br/>For example, if you shine a Light at a white wall behind fog with red Single Scattering Albedo, the fog looks red. If you shine a Light at a white wall and view it from the other side of the fog, the fog darkens the light but doesn’t tint it red. |
| **Fog Distance**             | Controls the density at the base of the fog and determines how far you can see through the fog in meters. At this distance, the fog has absorbed and out-scattered 63% of background light. |
| **Mask Mode**                | Select which kind of mask to apply to the fog. You can either choose a texture mask or a material mask. A texture mask will show a 3D texture applied in the fog volume. The material mask will be evaluated every frame allowing the creation of dynamic fog effects. |
| **Blending Mode**            | Select how the fog volume will be blended with the rest of the fog. The default value is Additive and adds the fog color and density to the scene fog. Overwrite completely replace the fog in the volume area. The Multiply mode multiplies the fog and can be used to do effects relative to a certain fog density. The Min and Max blending mode also do the min/max operation in the fog. |
| **Priority**                 | The priority is used to sort the volumes when blending them together. A higher priority means that the volume will be rendered after thus taking over the other fogs visually. |
| **Size**                     | Controls the dimensions of the Volume.                       |
| **Per Axis Control**         | Enable this to control blend distance per axis instead of globally. |
| **Blend Distance**           | Blend Distance creates a fade from the fog level in the Volume to the fog level outside it. <br/>This value indicates the absolute distance from the edge of the Volume bounds, defined by the Size property, where the fade starts.<br/>Unity clamps this value between 0 and half of the lowest axis value in the Size property.<br/>If you use the **Normal** tab, you can alter a single float value named Blend Distance, which gives a uniform fade in every direction. If you open the **Advanced** tab, you can use two fades per axis, one for each direction. For example, on the X-axis you could have one for left-to-right and one for right-to-left.<br/>A value of 0 hides the fade, and a value of 1 creates a fade. |
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

# Mask textures

A mask texture is a 3D texture that controls the shape and appearance of the local volumetric fog in your scene. Unity does not limit the size of this 3D texture, and its size doesn't affect the amount of memory a local volumetric fog uses.

## Use a built-in Mask Texture

HDRP includes 3D Density Mask Textures with different noise values and shapes that you can use in your scene. To use these Textures, import them from the High Definition RP package samples:

1. Open the Package Manager window (menu: **Window** > **Package Manager**).
2. Find the **High Definition RP** package.
3. Expand the **Samples** drop-down.
4. Find the **Local Volumetric Fog 3D Texture Samples** and click on the **Import** button.

## Create a Density Mask Texture

 To create a 3D texture to apply to a local volumetric fog component: 

1. In the image-editing software of your choice, create an RGBA flipbook texture and [import it as a 3D texture](https://docs.unity3d.com/2020.2/Documentation/Manual/class-Texture3D.html). For example, a texture of size 1024 x 32 describes a 3D Texture of size 32 x 32 x 32 with 32 slices laid out one after another.
2. Open a **Local Volumetric Fog** component and in its **Density Mask Texture** section assign the 3D Texture you imported to the **Texture** field .

## Limitations

HDRP voxelizes Local Volumetric Fog at 64 or 128 slices along the camera's focal axis to improve performance. This causes the following limitations:
- Local Volumetric Fog doesn't support volumetric shadowing. If you place Local Volumetric Fog between a Light and a surface, the Volume does not decrease the intensity of light that reaches the surface.
- Noticeable aliasing can appear at the boundary of the fog Volume. To hide aliasing artifacts, use Local Volumetric Fog with [global fog](Override-Fog.md). You can also use a Density Mask and a Blend Distance value above 0 to decrease the hardness of the edge.
