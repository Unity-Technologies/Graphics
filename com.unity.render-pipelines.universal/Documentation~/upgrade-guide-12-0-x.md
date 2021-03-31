# Upgrading to version 12.0.x of the Universal Render Pipeline

This page describes how to upgrade from an older version of the Universal Render Pipeline (URP) to version 12.0.x.

## ClearFlag

ClearFlag.Depth does not implicitely clear stencil anymore. ClearFlag.Stencil added.

## Upgrading from URP 11.0.x

### UniversalRenderer

The Forward Renderer asset is renamed to the Universal Renderer asset. When you open an existing project in the Unity Editor containing URP 12, Unity updates the existing Forward Renderer assets to Universal Renderer assets.

The Universal Renderer asset contains the property **Rendering Path** that lets you select the Forward or the Deferred Rendering Path.
