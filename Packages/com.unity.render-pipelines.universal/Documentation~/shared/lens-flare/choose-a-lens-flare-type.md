# Choose a lens flare type

You can add the following types of lens flares:

- [Lens flares](lens-flare-component.md) - use a **Lens Flare (SRP)** component to create lens flares for lights that have specific locations in your scene, for example the sun or bright bulbs.
- [Screen space lens flares](post-processing-screen-space-lens-flare.md) - use a **Screen Space Lens Flare** override to create lens flares for emissive surfaces, bright spots, and onscreen lights.

You can use both types in the same scene.

Use the following table to help you choose a lens flare type:

| Feature | Lens Flare (SRP) component | Screen Space Lens Flare override |
|-|-|-|
| Typical uses | Lens flares from the sun and specific lights, custom flare shapes, and cinematics | Lens flares on vehicles and water, first-person games, and science-fiction environments |
| Supported platforms | All platforms | All platforms |
| CPU and GPU use | CPU and GPU | GPU |
| Types of light | All Light objects, except Area Lights | All bright spots and visible lights |
| Placement | Attach to individual lights. Place lens flares manually | Generate inside a volume. Place all lens flares automatically with a single setting |
| Lens flares from offscreen lights | Yes | No |
| Light streaks | No, unless you create them manually | Yes |
| Configure flares | Configure per lens flare or per element | Configure for all lens flares together |
| Configure flare elements | Configure many settings for each element, per lens flare | Configure several settings for elements, for all lens flares together |
| Configure attenuation | Yes | No |
| Affected by the environment | Yes | Yes |
| Preserve aspect ratio | Yes | No |
| Chromatic aberration | No | Yes |
| Blend modes | Additive, Lerp, Premultiplied and Screen | Additive only |
| Occlusion | Screen space occlusion, and geometric occlusion for offscreen lights. Configurable. Occlusion might not always work at the edge of the screen. | Screen space occlusion, generated from the color buffer. Not configurable |
| Examples in [package samples](../../package-samples.md) | Yes | No |

## Additional resources

- [Lens Flare (SRP) reference](lens-flare-srp-reference.md)
- [Lens Flare (SRP) Data Asset reference](lens-flare-asset.md)
- [Screen Space Lens Flare override reference](post-processing-screen-space-lens-flare.md)
