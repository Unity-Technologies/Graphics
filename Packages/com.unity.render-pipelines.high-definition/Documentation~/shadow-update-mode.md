# Update shadows less frequently

By default, the High Definition Render Pipline (HDRP) calculates the shadow map of a Light every frame. To improve performance, reduce how often HDRP updates the shadow map.

## Reduce shadow map updates

Follow these steps:

1. Select a Light in your Scene.
1. In the Inspector window, in the **Shadows** section, set **Update Mode** to **On Enable** or **On Demand**.

   - **On Enable**: Updates the shadow map only when the Light is enabled.
   - **On Demand**: Updates the shadow map only when you use an API to update the shadows manually.

In these modes, HDRP caches the shadow map when the shadows update, and uses the cached version between updates.

Point Lights and Area Lights have their own shadow atlas for cached shadows. Directional Lights store cached shadows in the same shadow atlas as non-cached Directional Lights. For more information about shadow atlases, refer to [Control shadow resolution and quality](Shadows-in-HDRP.md). 

## Updates shadows manually

If you set the **Update Mode** of the Light to **On Demand**, follow these steps to update the shadows:

1. In the Inspector window for the Light, go to **HDAdditionalLightData** and open the **More** (&#8942;) menu.
1. Select **Edit Script**.
1. Call the `RequestShadowMapRendering` API in the script when you want to update the shadows.

HDRP also updates the shadows when you first enable [Contact Shadows](Override-Contact-Shadows.md).

If you set a Directional Light to **On Demand**, update shadows frequently so they stay up-to-date with the camera position. Otherwise you might see visual artifacts.

For more information about customizing which shadows HDRP updates and when, refer to the [`HDAdditionalLightData`](xref:UnityEngine.Rendering.HighDefinition.HDAdditionalLightData) API.

## Update shadows when the Light moves

To update the shadow map only when the position or rotation of the Light changes, follow these steps:

1. Set **Update Mode** to **On Enable**.
1. Enable **Update on light movement**.

To customize how much a light needs to move or rotate to trigger an update, use the [`cachedShadowAngleUpdateThreshold`](xref:UnityEngine.Rendering.HighDefinition.HDAdditionalLightData.cachedShadowAngleUpdateThreshold) and [`cachedShadowTranslationUpdateThreshold`](xref:UnityEngine.Rendering.HighDefinition.HDAdditionalLightData.cachedShadowTranslationUpdateThreshold) APIs.

**Note**: Point Lights ignore `cachedShadowAngleUpdateThreshold`.

## Preserve cached shadows

To preserve a cached shadow map when you disable a Light or set its **Update Mode** back to **Every Frame**, edit your script to set the [`UnityEngine.Rendering.HighDefinition.HDAdditionalLightData.preserveCachedShadow`](HighDefinition.HDAdditionalLightData.preserveCachedShadow) property to `true`. 

HDRP keeps the shadow map in the shadow atlas, so it doesn't need to re-render the shadow map or place it into a shadow atlas again. This is useful if, for example, you want HDRP to cache the shadow map of a distant Light, but update the shadow map every frame when the Light gets closer to the camera.

**Note**: If you destroy the Light, HDRP no longer preserves its shadow map in the shadow atlas.

## Additional resources

- [Realtime shadows](Realtime-Shadows.md)
- [Contact Shadows](Override-Contact-Shadows.md)
