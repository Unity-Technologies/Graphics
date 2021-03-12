# SRP Lens Flare Override Component

The SRP (HDRP & URP) includes a component to describe Lens Flare, which consume an asset [SRP Lens Flare Data](srp-lens-flare-asset.md). This component can be attached to any game object, if it's attached to a light some extra option will be visible.

![](images/LensFlareComp.png)

### Properties

## General

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Lens Flare Data | A [SRP Lens Flare Data](srp-lens-flare-asset.md) used with this component. |
| Intensity     | Intensity multiplier. |
| Attenuation by Light Shape | If component attached to a light, attenuation the lens flare per light type. |
| Attenuation Distance | Attenuation distance, this distance represent the end of the Attenuation Distance Curve (between 0 and 1), uses world space values. |
| Attenuation Distance Curve | Attenuation by distance (normalized between 0 and 1), useful to fade out far flare. |
| Scale Distance | Scale distance, this distance represent the end of the Scale Distance Curve (between 0 and 1), uses world space values. |
| Scale Distance Curve | Scale the flare relative to distance, useful to change size out far flare. |
| Screen Attenuation Curve | Attenuation based on distance to edge of screen, useful for instance to show flare only on edge of screen. |

## Occlusion

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Enable | True to enable occlusion based on depth buffer |
| Occlusion Radius | Radius around the light used to occlude the flare (value in world space). |
| Sample Count | Random Samples Count used inside the disk with 'occlusionRadius'. |
| Occlusion Offset | Occlusion Offset allow us to offset the plane where the disc of occlusion is place (closer to camera), value on world space. Useful for instance to sample occlusion outside a light bulb if we place a flare inside the light bulb. |
| Allow Off Screen | If allowOffScreen is true then If the lens flare is outside the screen we still emit the flare on screen. |
