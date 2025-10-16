# Realistic smoke lighting with six-way lighting

Blend pre-baked lightmaps and achieve dynamic lighting for smoke and other visual effects with six-way lighting.

Six-way lighting stores lighting from six directions in textures. At runtime, Unity blends these textures based on scene lighting. Effects like smoke, explosions, and clouds thus respond to light sources.

Six-way lighting enables the following:

- Volumetric lighting for 2D or flipbook-based particle effects.
- Scaling for real-time visual effects (VFX).
- Adjustable color and emission.
- Compatibility with Unity's lighting system, including HDRP and URP.

You can use six-way lighting in such scenarios as the folllowing:

- Daytime smoke with sun and sky lighting.
- Nighttime effects with static or dynamic lights.
- Sandstorms and tornadoes.
- Glowing explosions.

## How six-way lighting works

Six-way lighting pre-bakes directional lighting into six sprite texture lightmaps. Each lightmap captures how the sprite is illuminated from a single direction: top, bottom, left, right, front, or back.

At runtime, Unity shaders blend these six lightmaps based on the current scene lighting. Particles then react to light from any direction.

## Supported lighting and Unity features

You can use six-way lighting with the built-in VFX Graph Six-Way Smoke Lit output in both URP and HDRP.
 
Six-way lighting supports indirect lighting from the following:
- Light Probes
- Adaptive Probe Volumes
- Ambient Probes
- Light Probe Proxy Volumes
- Dynamic point, spot, area, and directional lights

**Note:** Six-way lighting does not support specular contributions from reflection probes.

## Lightmap channel mapping

Unity stores the six directional lightmaps in two RGBA textures. The channel mapping is as follows:

- Lightmap A:
  - Red: right
  - Green: top
  - Blue: back
  - Alpha: transparency
- Lightmap B:
  - Red: left
  - Green: bottom
  - Blue: front
  - Alpha: emissive data

![Five versions of the same smoke sprite. The first is red and lit from the right. The second is green and lit from the top. The third is blue and lit from the back. The fourth appears white and stores transparency in the Alpha channel. The fifth is a multicolored combination of all the previous sprites and stores the RGBA data in Lightmap A.](Images/lightmap-a.jpg)


![Five versions of the same smoke sprite. The first is red and lit from the left. The second is green and lit from the bottom. The third is blue and lit from the front. The fourth appears black and does not store any information in the Alpha channel. The fifth is a multicolored combination of all the previous sprites and stores the RGBA data in Lightmap B.](Images/lightmap-b.jpg)

## Simulate fire and explosions

To create glowing effects such as fire or explosions, store emissive data in the Alpha channel of Lightmap B to enable dynamic control over the effect's glow and color at runtime.
