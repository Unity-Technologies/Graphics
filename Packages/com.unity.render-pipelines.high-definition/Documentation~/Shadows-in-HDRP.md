# Shadows in the High Definition Render Pipeline

The High Definition Render Pipeline’s [Lights](Light-Component.md) can cast shadows from one GameObject onto another. They emphasize the position and scale of GameObjects, which adds a degree of depth and realism to a Scene that could otherwise look flat.

![](Images/HDRPFeatures-Shadows.png)

## Shadow map resolution

The resolution of a Light’s shadow map determines the size of its shadow maps. The larger the shadow map, the more precise the shadows can be, and the better the High Definition Render Pipeline (HDRP) can capture small details in the shadow casting geometry. Rendering shadow maps at higher resolutions make them look sharper.

Set the resolution of a specific Light’s shadow map in the **Shadows** section of the Light component.

The number of shadow maps HDRP renders per Light depends on the **Type** of the Light:

- A Spot Light renders one shadow map.
- A Point Light renders six shadow maps (the number of faces in a cubemap).
- An Area Light renders one shadow map.
- A Directional Light renders one shadow map per cascade. Set the cascade count of Directional Lights from the [HD Shadow Settings](Override-Shadows.md) of your Scene’s [Volumes](Volumes.md). The default value is four cascades.

HDRP can perform a dynamic rescale of shadow maps to maximize space usage in shadow atlases, but also to reduce the performance impact of lights that occupy a small part of the screen. To do this, HDRP scales down a light's shadow map resolution depending on the size of the screen area the light covers. The smaller the area on the screen, the more HDRP scales the resolution down from the value set on the [Light component](Light-Component.md).

To enable this feature:

1. Open your Unity Project’s [HDRP Asset](HDRP-Asset.md) in the Inspector window.
2. Go to **Lighting** > **Shadows**.
3. Go to either **Punctual Light Shadows** or **Area Light Shadows**.
4. Go to **Light Atlas** and enable **Dynamic Rescale**.

**Note**: HDRP doesn't support dynamic rescale for cached shadow maps.

## Shadow atlases

HDRP renders all real-time shadows for a frame using a shadow map atlas for all [punctual light](Glossary.md#PunctualLight) shadows, an atlas for area lights and another one for Directional Light shadows.

Set the size of these atlases in your Unity Project’s [HDRP Asset](HDRP-Asset.md). The atlas size determines the maximum resolution of shadows in your Scene.

For example, the default size of an atlas is 4096 x 4096, which can fit:

- Sixteen shadow maps of 1024 x 1024 pixels.
- Two shadow maps of 2048 x 2048 plus four shadow maps of 1024 x 1024 plus eight shadow maps of 512 x 512 plus 32 shadow maps of 256 x 256.

## Controlling the maximum number of shadows on screen

In addition to the atlases, you can also set the maximum number of shadow maps HDRP can render in a single frame. To do this, open your Unity Project’s HDRP Asset, navigate to the **Shadows** section, and enter a **Max Shadows on Screen** value. If the number of shadow maps on screen is higher than this limit, HDRP doesn't render them.

## Shadow Bias

Shadow maps are essentially textures projected from the point of view of the Light. HDRP uses a bias in the projection so that the shadow casting geometry doesn't self-shadow itself.

In HDRP, each individual Light component controls its own shadow biasing using the following parameters:

- **Near Plane**
- **Slope-Scale Depth Bias**
- **Normal Bias**

Find these settings under the **Shadows** section. If some property fields are missing, enable [additional properties](More-Options.md) to display them. For details on how each property controls the shadow biasing, see the [Light documentation](Light-Component.md).

![](Images/Shadows1.png)

Using high shadow bias values may result in light "leaking" through Meshes. This is where there is a visible gap between the shadow and its caster and leads to shadow shapes that don't accurately represent their casters.

<a name="ShadowFiltering"></a>

## Shadow filtering

After HDRP captures a shadow map, it processes filtering on the map to decrease the aliasing effect that occurs on low resolution shadow maps. Different filters affect the perceived sharpness of shadows.

To change which shadow filter quality to use, change the **Filtering Quality** property in your Unity Project’s [HDRP Asset](HDRP-Asset.md). Higher quality have impact on GPU performance.
There are currently three filter quality presets for directional and punctual lights. For information on the available filter qualities, see the [Filtering Qualities table](HDRP-Asset.md#filtering-qualities).

## Shadowmasks

HDRP supports two [Mixed Lighting Modes](https://docs.unity3d.com/Manual/LightMode-Mixed.html):

* [Baked indirect](https://docs.unity3d.com/Manual/LightMode-Mixed-BakedIndirect.html)
* [Shadowmask](https://docs.unity3d.com/Manual/LightMode-Mixed-ShadowmaskMode.html).

### Enabling shadowmasks

To use shadowmasks in HDRP, you must enable shadowmask support in your Unity Project’s HDRP Asset and then make your Cameras use shadowmasks in their [Frame Settings](Frame-Settings.md) :

1. Under **Render Pipeline Supported Features**, enable the **Shadowmask** checkbox. This enables support for shadowmasks in your Unity Project.
2. Next, you must make your Cameras use shadowmasks. In your HDRP Asset, navigate to **Default Frame Settings > Lighting** and enable the **Shadowmask** checkbox to make Cameras use shadowmasks by default.

### Specific settings in HDRP

For flexible lighting setups, HDRP allows you to choose how you want the shadowmasks to behave for each individual Light. You can change the behavior of the shadowmask by changing a Light’s **Shadowmask Mode**. Set the Light’s **Mode** to **Mixed** to expose **Shadowmask Mode** in the **Shadow Map** drop-down of the **Shadows** section.

<a name="ShadowmaskModes"></a>

| **Shadowmask Mode**     | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Distance Shadowmask** | Makes the Light cast real-time shadows for all GameObjects when the distance between the Camera and the Light is less than a punctual light’s **Fade Distance**. See [below](#DirectionalLightEquivalentProperty) for the alternative distance property that Directional Lights use. When the distance between the Light and the Camera is greater than the **Fade Distance**, HDRP stops calculating real-time shadows for the Light. Instead, it uses shadowmasks for static GameObjects, and non-static GameObjects don't cast shadows. |
| **Shadowmask**          | Makes the Light cast real-time shadows for non-static GameObjects only. It then combines these shadows with shadowmasks for static GameObjects when the distance between the Camera and the Light is less than the **Fade Distance**. When the distance between the Light and the Camera is greater than the **Fade Distance**, HDRP stops calculating real-time shadows for the Light. Instead, it uses shadowmasks for static GameObjects and non-static GameObjects don't cast shadows. |

<a name="DirectionalLightEquivalentProperty"></a>

Directional Lights don't use **Fade Distance**. Instead they use the **Max Distance** property located in the [HD Shadow Settings](Override-Shadows.md) of your Scene’s Volumes.

**Distance Shadowmask** is more GPU intensive, but looks more realistic because real-time lighting that's closer to the Light is more accurate than shadowmask textures with a low resolution chosen to represent areas further away.

**Shadowmask** is more memory intensive because the Camera uses shadowmask textures for static GameObjects close to the Camera, requiring a larger resolution shadowmask texture.

<a name="ShadowUpdateMode"></a>

## Shadow Update Mode

You can use **Update Mode** to specify the calculation method HDRP uses to update a [Light](Light-Component.md)'s shadow maps. The following Update Modes are available:

| **Update Mode** | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Every Frame** | HDRP updates the shadow maps for the light every frame.      |
| **On Enable**   | HDRP updates the shadow maps for the light whenever you enable the GameObject. |
| **On Demand**   | HDRP updates the shadow maps for the light every time you request them. To do this, call the RequestShadowMapRendering() method in the Light's HDAdditionalLightData component. |

The High Definition Render Pipeline (HDRP) uses shadow caching to increase performance by only updating the shadow maps for [Lights](Light-Component.md) when it's necessary. HDRP has shadow atlases for punctual, area, and directional Lights, and separate shadow atlases specifically for cached punctual and cached area Lights. For cached directional Lights, they use the same atlas as normal directional Lights.

When a Light that caches its shadows renders its shadow map for the first time, HDRP registers it with the cached shadow manager which assigns the shadow map to a cached shadow atlas. For directional Lights, HDRP uses the same shadow atlas for cached and non-cached directional Lights.

A Light's **Update Mode** determines whether HDRP caches its shadow map:

* If you set a Light's **Update Mode** to **OnEnable** or **OnDemand**, HDRP caches the Light's shadow map.
* If you set a Light's **Update Mode** to **Every Frame**, HDRP doesn't cache the Light's shadow map.

If you set the Light's **Update Mode** to **OnDemand**, you can manually request HDRP to update the Light's shadow map. To do this:

1. Select a Light in your scene to view it in the Inspector window.
2. Go to **HDAdditionalLightData** and open the More menu (&#8942;).
3. Select **Edit Script**.
2. Call the `RequestShadowMapRendering` function in the script.

If the Light has multiple shadows (for example, multiple cascades of a directional light), you can request the update of a specific sub-shadow. To do this, use the `RequestSubShadowMapRendering(shadowIndex)` function.

When you set **Update Mode** to **OnDemand** HDRP renders the shadow maps `OnEnable` for the first time, or when first registered with the system by default. You can change this using the `onDemandShadowRenderOnPlacement` property. If you set this property to false, HDRP doesn't render the shadows until you call `RequestShadowMapRendering` or `RequestSubShadowMapRendering(shadowIndex)`.

For a Light that caches its shadows, if you disable it or set its **Update Mode** to **Every Frame**, HDRP can preserve the Light's shadow map's place in the cached shadow atlas. This means that, if you enable the Light again, HDRP doesn't need to re-render the shadow map or place it into a shadow atlas. For information on how to make a Light preserve its shadow map's place in the cached shadow atlas, see [Preserving shadow atlas placement](#preserving-shadow-atlas-placement).

As a shortcut for a common case, HDRP offers an option to automatically trigger an update when either the position or rotation of a light changes above a certain threshold. To enable this option:

1. Select a Light in your Scene to view it in the Inspector window.
2. Go to **Light** > **Shadows** and set **Update Mode** to **On Enable**
3. Enable **Update on light movement**.

You can customize the threshold that HDRP uses to determine how much a light needs to move or rotate to trigger an update. To do this, use the properties: `cachedShadowTranslationUpdateThreshold` and `cachedShadowAngleUpdateThreshold` properties on the Light's **HDAdditionalLightData** component.

**Note**: Point lights ignore the angle differences when determining if they need to perform an update in this mode.

### Customising shadow caching
HDRP caches shadow maps for Punctual Lights into one atlas, Area Lights into another, and Directional Lights into the same atlas as non-cached Directional Lights. You can change the resolution of the first two cached shadow atlases independently of one another. To do this:

1. Open your Unity Project’s [HDRP Asset](HDRP-Asset.md) in the Inspector window.
2. For Punctual lights, go to **Lighting > Shadows > Punctual Light Shadows**. For Area lights, go to **Lighting > Shadows > Area Light Shadows**.
3. Set **Cached Shadow Atlas Resolution** to the value you want. To help with shadow atlas organisation, try to keep the resolution of individual shadow maps as a multiple of 64. For the most optimal organisation, set the same resolution to as many shadow maps as possible.

If the shadow atlas is full when a Light requests a spot, the cached shadow manager doesn't add the Light's shadow map and so the Light doesn't cast shadows. This means that it's important to manage the space you have available. To check if a Light can fit in the shadow atlas, you can use the `HDCachedShadowManager.instance.WouldFitInAtlas` helper function. To see if a Light already has a place in the atlas or if it's waiting for one, go to **Window** > **Rendering** > **Light Explorer** > **Shadows Fit Atlas**.


After a Scene loads with the Lights you have already placed, if you add a new Light with cached shadows to the Scene, HDRP tries to place it to fill the holes in the atlas. However, depending on the order of insertion, the atlas may be fragmented and the holes available aren't enough to place the Light's shadow map in. In this case, you can defragment the atlas to allow for additional Lights. To do this, pass the target atlas into the following function: `HDCachedShadowManager.instance.DefragAtlas`

**Note**: This causes HDRP to mark all the shadow maps in the atlas as dirty which means HDRP renders them the moment their parent Light becomes visible.

It's possible to check if a Light's shadow maps have a place in the cached shadow atlas by using `HDCachedShadowManager.instance.LightHasBeenPlacedInAtlas`. It's also possible to check if a Light's shadow maps have been placed and rendered at least once with `HDCachedShadowManager.instance.LightHasBeenPlaceAndRenderedAtLeastOnce`.

### Preserving shadow atlas placement

If you disable the Light or change its **Update Mode** to **Every Frame**, the cached shadow manager unreserves the Light's shadow map's space in the cached shadow atlas and HDRP begins to render the Light's shadow map to the normal shadow atlases every frame. If the cached shadow manager needs to allocate space on the atlas for another Light, it can overwrite the space currently taken up by the original Light's shadow map.

If you want to temporarily set a Light's **Update Mode** to **Every Frame** and want to set it back to **On Enable** or **On Demand** later, you can preserve the Light's shadow map placement in its atlas. This is useful, for example, if you want HDRP to cache a far away Light's shadow map, but update it every frame when it gets close to the [Camera](HDRP-Camera.md). To do this:

1. Select a Light in your scene to view it in the Inspector window.
2. Go to **HDAdditionalLightData** and open the More menu (&#8942;)
3. Select **Edit Script**
4. Enable **preserveCachedShadow** and set it to **True**. HDRP preserves the Light's shadow map's space in its shadow atlas.

**Note**: Even if you enable **preserveCachedShadow**, if you destroy the Light, it loses its placement in the shadow atlas.

### Mixed Cached Shadow Maps

In HDRP it's possible to cache only some of the shadow map for non-directional lights. To do this:

1. Select a Light in your scene to view it in the Inspector window.
2. Go to **Light** > **Shadows**
3. Enable **Always draw dynamic**.
4. Enable **Static Shadow Caster** for all Renderers to cache shadows for.

With this set up, HDRP renders static shadow casters into the shadow map depending on the Light's Update Mode, but it renders dynamic shadow casters into their respective shadow maps each frame. If you set the Update Mode to **OnEnable**, HDRP only renders static shadow casters when you enable the Light component. If the Update Mode is to **OnDemand**, HDRP only renders static shadow casters when you explicitly request an update.

This setup is particularly useful if your environment consists of mostly static GameObjects and the lights don't move, but there are few dynamic GameObjects that you want the static lights to cast shadows for. In such scenarios, setting the light to have a mixed cached shadow map greatly improves performance both on the CPU and GPU.

**Note**: If you set up a shadow to be mixed cached, HDRP performs a blit from the cached shadow map to the dynamic atlas. This is important to keep in mind both for the extra runtime cost of the blit in itself, but also that, in terms of memory, a single shadow map requires space in both atlases.

**Note**: If a Light with mixed cached shadows moves and you don't update the cached counterpart, the result looks wrong. In such cases either enable the Light's **Update on light movement** option or set the Light's update mode to **OnDemand** and make sure to trigger an update when you move the Light.

### Notes

While you are in the Unity Editor, HDRP updates shadow maps whenever you modify the Light that casts them. In a built application, HDRP refreshes cached shadow maps when you change different properties on the Light or when you call one of the following functions:

- `SetShadowResolution()`
- `SetShadowResolutionLevel()`
- `SetShadowResolutionOverride()`
- `SetShadowUpdateMode()` or `shadowUpdateMode`. In this case, HDRP only refreshes the cached shadow maps if the mode changes between Every Frame and not Every Frame.

Anything that's view-dependent is likely to create problems with cached shadow maps because HDRP doesn't automatically update them as the main view moves. An example of this is tessellation. Tessellation factor is view-dependent, so the geometry that the main camera sees might not match the geometry that HDRP rendered into the cached shadow map. If this visibly occurs, trigger a request for HDRP to update the Light's shadow map. To do this, make sure you set the Light's **Update Mode** to **On Demand** and call `RequestShadowMapRendering`.

Another example is when multiple views are available. The light will be updated only for a single view therefore causing the other views to have incorrect results. To avoid a common scenario in which the described artifact will occur, HDRP won't mark a shadow request as completed when performed from reflection probes with view dependent shadows and waiting until a non-reflection camera triggers a shadow update.

## Contact Shadows

Contact Shadows are shadows that HDRP [ray marches](Glossary.md#RayMarching) in screen space, inside the depth buffer, at a close range. They provide small, detailed, shadows for details in geometry that shadow maps can't usually capture.

Only one Light can cast Contact Shadows at a time. This means that, if you have more than one Light that casts Contact Shadows visible on the screen, only the dominant Light renders Contact Shadows. HDRP chooses the dominant Light using the screen space size of the Light’s bounding box. A Direction Light that casts Contact Shadows is always the dominant Light.

For details on how to enable and customize Contact Shadows, see the [Contact Shadows override documentation](Override-Contact-Shadows.md).
