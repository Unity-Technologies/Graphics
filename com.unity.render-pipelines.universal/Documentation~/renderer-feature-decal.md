# Decal

The decal renderer feature enables support for decals, which includes rendering mesh with [Decal Shader Graph](decal-shader.md) and [Decal Projector](decal-projector.md). Decals are great for adding texture detail to existing surface at runtime.

## Setup

1. Add Decal [Renderer Feature](urp-renderer-feature-how-to-add.md).
2. Create [Decal Projector](decal-projector.md).

## Properties

### Techniques

URP supports different decal rendering techniques designed for specific platforms.

#### Automatic

Automatically selects technique based on build platform.

#### Screen Space

Renders decals after opaque objects with normal reconstructed from depth. The decals are simply rendered as mesh on top of opaque ones, as result does not support blending per single surface data (etc. normal blending only).

| __Propery__     | __Description__ |
| --------------- |---------------- |
| __Normal Blend__| Controls the quality of normal reconstruction. The higher the value the more accurate normal reconstruction and the cost on performance.|
| _Low_           | Low quality of normal reconstruction. Uses 1 depth sample.|
| _Medium_        | Medium quality of normal reconstruction. Uses 3 depth samples.|
| _High_          | High quality of normal reconstruction. Uses 5 depth samples.|
| __Use GBuffer__ | Uses traditional GBuffer decals, if renderer is set to deferred. Support only base color, normal and emission.|

#### DBuffer

Renders decals into DBuffer and then applied during opaque rendering. Requires DepthNormal prepass which makes not viable solution for tiled based renderers.

|__Propery__           | __Description__ |
| -------------------- |---------------- |
| __Surface Data__     | Allows specifying which decals surface data should be blended with surfaces. |
| _Albedo_             | Decals will affect only base color and emission.|
| _Albedo Normal_      | Decals will affect only base color, normal and emission|
| _Albedo Normal MAOS_ | Decals will affect only base color, normal, metallic, ambient occlusion, smoothness and emission.|

### Max Draw Distance

Maximum global draw distance of decals.
