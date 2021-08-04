# Upgrading to version 12.0.x of the Universal Render Pipeline

This page describes how to upgrade from an older version of the Universal Render Pipeline (URP) to version 12.0.x.

## ClearFlag

ClearFlag.Depth does not implicitely clear stencil anymore. ClearFlag.Stencil added.

## Upgrading from URP 11.0.x

### UniversalRenderer

The Forward Renderer asset is renamed to the Universal Renderer asset. When you open an existing project in the Unity Editor containing URP 12, Unity updates the existing Forward Renderer assets to Universal Renderer assets.

The Universal Renderer asset contains the property **Rendering Path** that lets you select the Forward or the Deferred Rendering Path.

### Intermediate Texture

Previously, URP would force rendering to go through an intermediate renderer if the Renderer had any Renderer Features active. On some platforms, this has significant performance implications. Due to that, Renderer Features are now expected to declare their inputs using `ScriptableRenderPass.ConfigureInput`. This information is used to decide automatically whether rendering via an intermediate texture is necessary.

For compatibility reasons, a new property **Intermediate Texture** has been added to the Universal Renderer. This allows for either using the new behaviour, or to force the use of an intermediate texture. The latter should only be used if a Renderer Feature does not declare its inputs using `ScriptableRenderPass.ConfigureInput`.

All existing Universal Renderer assets that were using any Renderer Features (excluding those included with URP) are upgraded to force the use of an intermediate texture, such that existing setups will continue to work correctly. Any newly created Universal Renderer assets will default to the new behaviour.
