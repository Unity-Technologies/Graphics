# Volumetric Fog

Volumetric fog is the most advanced implementation of fog available in the High Definition Render Pipeline (HDRP). It realistically simulates the interaction of lights with fog, which allows for physically-plausible rendering of glow and crepuscular rays, which are beams of light that stream through gaps in objects like clouds and trees from a central point, like a God ray from the Sun..

## Using Volumetric Fog

**Volumetric Fog** uses the [Volume](Volumes.html) framework, so to enable and modify **Volumetric Fog** properties, you must add an **Volumetric Fog** override to a [Volume](Volumes.html) in your Scene. To add **Volumetric Fog** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Fog** and click on **Volumetric Fog**.

After you add an **Volumetric  Fog** override, you must set the Volume to use **Volumetric Fog**. The [Visual Environment](Override-Visual-Environment.html) override controls which type of fog the Volume uses. In the **Visual Environment** override, navigate to the **Fog** section and set the **Type** to **Volumetric Fog**. HDRP now renders **Volumetric Fog** for any Camera this Volume affects.

Within the Scene, there is usually a single Volume set to __IsGlobal__ that contains a Visual Environment override. Having a single global Visual Environment means that Unity uses the same __Sky Type__ and __Fog Type__ everywhere in the Scene. You can still use local Volumes with different __Sky Types__ and __Fog Types__, but the transition between them is obvious and instantaneous. If you want to use multiple Visual Environments in different Volumes in your Scene, it is best to make the transition on Camera cuts.

At this point, the Scene contains global volumetric fog. However, the effect is not visible because the default global fog density is very low. To override the default property with your own chosen values, follow the steps in the [Customizing Global Volumetric Fog](#CustomizingGlobalVolumetricFog) section.

<a name="CustomizingGlobalVolumetricFog"></a>

## Customizing Global Volumetric Fog

Use global fog, rather than local fog, because it provides the best performance and the best quality.

Global fog is a height fog. It has two logical components: the region at a distance closer to the Camera than the __Base Height__ is a constant (homogeneous) fog, and the region at a distance further than the __Base Height__ is the exponential fog.

The __Volumetric Fog__ component of the active Volume controls the appearance of the global fog.

## Properties

![](Images/Override-VolumetricFog2.png)

| Property                 | Function                                                     |
| :----------------------- | :----------------------------------------------------------- |
| **Single Scattering Albedo** | Sets the fog color. Volumetric Fog tints lighting, so the fog scatters light to this color. It only tints lighting emitted by Lights behind or within the fog. This means that it does not tint lighting that reflects off GameObjects behind or within the fog - reflected lighting only gets dimmer (fades to black) as fog density increases. For example, if you shine a Light at a white wall behind fog with red Single Scattering Albedo, the fog looks red. If you shine a Light at a white wall and view it from the other side of the fog, the fog darkens the light but doesn’t tint it red. |
| **Base Fog Distance**    | Controls the density at the base of the fog and determines how far you can see through the fog in Unity units. At this distance, the fog has absorbed and out-scattered 63% of background light. |
| **Base Height**          | The height of the boundary between the constant (homogeneous) fog and the exponential fog. |
| **Mean Height**          | Controls the rate of falloff for the height fog in Unity units. Higher values stretch the fog vertically. At this height , the falloff reduces the initial base density by 63%. |
| **Global Anisotropy** | Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering. Note that non-zero values have a moderate performance impact. High values may have compatibility issues with the Enable Reprojection for Volumetrics Frame Setting. This is an experimental property that HDRP applies to both global and local fog. |
| **Global Light Probe Dimmer** | Reduces the intensity of the global Light Probe that the sky generates. |
| **Max Fog Distance** | Controls the distance (in Unity units) when applying fog to the skybox or background. Also determines the range of the Distant Fog. For optimal results, set this to be larger than the Camera’s Far value for its Clipping Plane. Otherwise, a discrepancy occurs between the fog on the Scene’s GameObjects and on the skybox. Note that the Camera’s Far Clipping Plane is flat whereas HDRP applies fog within a sphere surrounding the Camera. |
| **Distant Fog** | <a name="DistantFog"></a>Activates the fog with precomputed lighting behind the volumetric section of the Camera’s frustum. The fog stretches from the maximum Distance Range in the Volumetric Lighting Controller to the Max Fog Distance. |
| **Color Mode** | Use the drop-down to select the mode HDRP uses to calculate the color of the fog.<br />&#8226; **Sky Color**: HDRP shades the fog with a color it samples from the sky cubemap and its mipmaps.<br />&#8226; **Constant Color**: HDRP shades the fog with the color you set manually in the **Constant Color** field that appears when you select this option. |
| **- Mip Fog Near**    | The distance (in meters) from the Camera that HDRP stops sampling the lowest resolution mipmap for the fog color.<br />This property only appears when you select **Sky Color** from the **Color Mode** drop-down. |
| **- Mip Fog Far**     | The distance (in meters) from the Camera that HDRP starts sampling the highest resolution mipmap for the fog color.<br />This property only appears when you select **Sky Color** from the **Color Mode** drop-down. |
| **- Mip Fog Max Mip** | Use the slider to set the maximum mipmap that HDRP uses for the mip fog. This defines the mipmap that HDRP samples for distances greater than **Mip Fog Far**.<br />This property only appears when you select **Sky Color** from the **Color Mode** drop-down. |
| **- Constant Color**  | Use the color picker to select the color of the fog.<br />This property only appears when you select **Constant Color** from the **Color Mode** drop-down. |




## Light-specific Properties

The [Light component](Light-Component.html) has several properties that are useful for volumetric lighting:

- __Emission Radius__ is useful to simulate fill lighting. It acts by virtually "pushing" the light away from the Scene. As a result, it softens the core of punctual lights. Always use a non-zero value to reduce ghosting artifacts resulting from reprojection.
- __Volumetric Dimmer__ only affects the fog and replaces the Light Dimmer that HDRP uses for surfaces.
- __Shadow Dimmer__ only affects the fog and replaces the Shadow Dimmer that HDRP uses for surfaces.

