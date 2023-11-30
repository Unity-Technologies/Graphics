## Set a shadow update mode

You can use **Update Mode** to specify the calculation method HDRP uses to update a [Light](Light-Component.md)'s shadow maps. The following Update Modes are available:

| **Update Mode** | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Every Frame** | HDRP updates the shadow maps for the light every frame.      |
| **On Enable**   | HDRP updates the shadow maps for the light whenever you enable the GameObject. |
| **On Demand**   | HDRP updates the shadow maps for the light every time you request them. To do this, call the RequestShadowMapRendering() method in the Light's HDAdditionalLightData component. |

The High Definition Render Pipeline (HDRP) uses shadow caching to increase performance by only updating the shadow maps for [Lights](Light-Component.md) when it's necessary. HDRP has shadow atlases for punctual, area, and directional Lights, and separate shadow atlases specifically for cached punctual and cached area Lights. For cached directional Lights, they use the same atlas as normal directional Lights.

When a Light that caches its shadows renders its shadow map for the first time, HDRP registers it with the cached shadow manager which assigns the shadow map to a cached shadow atlas. For directional Lights, HDRP uses the same shadow atlas for cached and non-cached directional Lights.

A Light's **Update Mode** determines whether HDRP caches its shadow map:

- If you set a Light's **Update Mode** to **OnEnable** or **OnDemand**, HDRP caches the Light's shadow map.
- If you set a Light's **Update Mode** to **Every Frame**, HDRP doesn't cache the Light's shadow map.

If you set the Light's **Update Mode** to **OnDemand**, you can manually request HDRP to update the Light's shadow map. To do this:

1. Select a Light in your scene to view it in the Inspector window.
2. Go to **HDAdditionalLightData** and open the More menu (&#8942;).
3. Select **Edit Script**.
4. Call the `RequestShadowMapRendering` function in the script.

If the Light has multiple shadows (for example, multiple cascades of a directional light), you can request the update of a specific sub-shadow. To do this, use the `RequestSubShadowMapRendering(shadowIndex)` function.

When you set **Update Mode** to **OnDemand** HDRP renders the shadow maps `OnEnable` for the first time, or when first registered with the system by default. You can change this using the `onDemandShadowRenderOnPlacement` property. If you set this property to false, HDRP doesn't render the shadows until you call `RequestShadowMapRendering` or `RequestSubShadowMapRendering(shadowIndex)`.

For a Light that caches its shadows, if you disable it or set its **Update Mode** to **Every Frame**, HDRP can preserve the Light's shadow map's place in the cached shadow atlas. This means that, if you enable the Light again, HDRP doesn't need to re-render the shadow map or place it into a shadow atlas. For information on how to make a Light preserve its shadow map's place in the cached shadow atlas, see [Preserving shadow atlas placement](Shadows-in-HDRP.md#preserve-shadow-atlas-placement).

As a shortcut for a common case, HDRP offers an option to automatically trigger an update when either the position or rotation of a light changes above a certain threshold. To enable this option:

1. Select a Light in your Scene to view it in the Inspector window.
2. Go to **Light** > **Shadows** and set **Update Mode** to **On Enable**
3. Enable **Update on light movement**.

You can customize the threshold that HDRP uses to determine how much a light needs to move or rotate to trigger an update. To do this, use the properties: `cachedShadowTranslationUpdateThreshold` and `cachedShadowAngleUpdateThreshold` properties on the Light's **HDAdditionalLightData** component.

**Note**: Point lights ignore the angle differences when determining if they need to perform an update in this mode.