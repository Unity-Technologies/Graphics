# Output Particle Forward Decal

Menu Path : **Context > Output Particle Forward Decal**

The **Output Particle Forward Decal** Context renders a particle system using a decal. A decal is a box into which the Visual Effect Graph projects a texture. Unity then renders the texture on any intersecting geometry along its xy plane. This means decal particles that don’t intersect any geometry are not visible. Note that although they are not visible, they still contribute to the resource intensity required to simulate and render the system.

This output implements the simplest form of decals. It is limited to blending a single albedo texture and is not lit.

More decal features are planned for future versions of the Visual Effect Graph.

Below is a list of settings and properties specific to the Output Particle Forward Decal Context. For information about the generic output settings this Context shares with all other Contexts, see [Global Output Settings and Properties](Context-OutputSharedSettings.md).


## Context properties

| Input            | Type       | Description                            |
| ---------------- | ---------- | -------------------------------------- |
| **Main Texture** | Texture 2D | The decal texture to use per particle. |
