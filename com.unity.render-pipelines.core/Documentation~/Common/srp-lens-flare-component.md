# SRP Lens Flare Override Component

The SRP (HDRP & URP) includes a component to describe Lens Flare, which consume an asset [SRP Lens Flare Data](srp-lens-flare-asset.md). This component can be attached to any game object, if it's attached to a light some extra option will be visible.

![](images/srp_lens_flare_comp.jpg)

### Properties

## General

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Lens Flare Data | A [SRP Lens Flare Data](srp-lens-flare-asset.md) used with this component. |
| Intensity     | Intensity multiplier. |
| Distance Attenuation Curve | Attenuation by distance, uses world space values. |
| Attenuation by Light Shape | If component attached to a light, attenuation the lens flare per light type. |
| Radial Screen Attenuation Curve | Attenuation used radially, which allow for instance to enable flare only on the edge of the screen. |

## Occlusion

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Occlusion Radius | Radius around the light used to occlude the flare (value in world space). |
| Sample Count | Random Samples Count used inside the disk with 'occlusionRadius'. |
| Occlusion Offset | Occlusion Offset allow us to offset the plane where the disc of occlusion is place (closer to camera), value on world space. Useful for instance to sample occlusion outside a light bulb if we place a flare inside the light bulb. |
| Allow Off Screen | If allowOffScreen is true then If the lens flare is outside the screen we still emit the flare on screen. |
