# Upgrading to version 12.0.x of the Universal Render Pipeline

This page describes how to upgrade from an older version of the Universal Render Pipeline (URP) to version 12.0.x.

## Upgrading from URP 11.0.x

### StandardRenderer

The Forward Renderer asset is renamed to the Standard Renderer asset. When you open an existing project in the Unity Editor containing URP 12, Unity updates the existing Forward Renderer assets to Standard Renderer assets.

The Standard Renderer asset contains the property **Rendering Path** that lets you select the Forward or the Deferred Rendering Path.
