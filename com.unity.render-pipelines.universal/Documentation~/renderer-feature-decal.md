# Decal

The Decal Renderer Feature renders GameObjects with the [Decal Projector](decal-projector.md) components.

## Setup

1. Add the Decal [Renderer Feature](urp-renderer-feature-how-to-add.md) to the URP Renderer.

2. Create a GameObject with the [Decal Projector](decal-projector.md) component.

## Decal Renderer Feature properties

This section describes the properties of the Decal Renderer Feature.

### Techniques

Select the rendering techniques that suits your project.

#### Automatic

Unity selects the technique automatically based on the build platform.

#### DBuffer

Renders decals into DBuffer and then applied during opaque rendering. Requires DepthNormal prepass which makes not viable solution for tiled based renderers.
Does not work on particles and terrain details.

|__Propery__           | __Description__ |
| -------------------- |---------------- |
| __Surface Data__     | Allows specifying which decals surface data should be blended with surfaces. |
| _Albedo_             | Decals will affect only base color and emission.|
| _Albedo Normal_      | Decals will affect only base color, normal and emission|
| _Albedo Normal MAOS_ | Decals will affect only base color, normal, metallic, ambient occlusion, smoothness and emission.|


#### Screen Space

Renders decals after opaque objects with normal reconstructed from depth. The decals are simply rendered as mesh on top of opaque ones, as result does not support blending per single surface data (etc. normal blending only).

| __Propery__     | __Description__ |
| --------------- |---------------- |
| __Normal Blend__| Controls the quality of normal reconstruction. The higher the value the more accurate normal reconstruction and the cost on performance.|
| _Low_           | Low quality of normal reconstruction. Uses 1 depth sample.|
| _Medium_        | Medium quality of normal reconstruction. Uses 3 depth samples.|
| _High_          | High quality of normal reconstruction. Uses 5 depth samples.|
| __Use GBuffer__ | Uses traditional GBuffer decals, if renderer is set to deferred. Support only base color, normal and emission.|


### Max Draw Distance

The maximum distance from the Camera at which Unity renders decals.
