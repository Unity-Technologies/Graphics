# Particle Texcoords node

## Description

The Particle Texcoords node provides per-particle texture coordinates for a Particle System that uses a **Texture Sheet Animation** module, so you can sample the correct frame of a flipbook texture.

Use the **Mode** dropdown to switch between the node's two variants: **Flipbook** and **Flipbook Blending**.

This node is a [Reflected Function node](Reflected-Function-Node.md). Its ports and behavior come from the `GetParticleTexcoords` HLSL functions in `Packages/com.unity.render-pipelines.core/ShaderLibrary/Particles.hlsl`.

**Note**: This node is intended for advanced use cases where you need direct access to the flipbook UVs and blend factor. For the common case of sampling a texture with flipbook blending, use the **Particle Flipbook Blending** subgraph instead. Refer to [Particle Flipbook Blending node](Particle-Flipbook-Blending-Node.md).

## Controls

| **Name** | **Type** | **Options** | **Description** |
|:---|:---|:---|:---|
| **Mode** | Dropdown | Flipbook, Flipbook Blending | Selects which of the node's two variants to use. |

## Flipbook mode

Provides the texture coordinates for the current particle animation frame.

### Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:---|:---|:---|:---|:---|
| **UV** | Input | Vector 2 | UV0 | The source UV coordinates. Defaults to UV0 when not connected. |
| **UV** | Output | Vector 2 | None | The UV coordinates for the current particle animation frame. |

## Flipbook Blending mode

Provides the texture coordinates for the current and next particle animation frames, and the blend factor between the two.

When GPU instancing is enabled, this mode also requires the `_FLIPBOOKBLENDING_ON` keyword to be defined and enabled; otherwise **UV2** and **Blend** output the source UV coordinates and `0.5` rather than next-frame values.

### Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:---|:---|:---|:---|:---|
| **UV** | Input | Vector 4 | None | The current frame's source UV coordinates (xy) and the next frame's source UV coordinates (zw). |
| **Blend Vertex Stream** | Input | Float | None | The value used to compute the blend factor between the two animation frames. |
| **UV** | Output | Vector 2 | None | The UV coordinates for the current particle animation frame. |
| **UV2** | Output | Vector 2 | None | The UV coordinates for the next particle animation frame. |
| **Blend** | Output | Float | None | The blend factor between the current and next frame. |

### Vertex streams and mesh requirements

What you need to feed the **UV** and **Blend Vertex Stream** inputs depends on the render mode of the particle system and whether GPU Instancing is enabled:

| **Render mode** | **Required Custom Vertex Streams** | **Mesh requirements** |
|:---|:---|:---|
| Billboard, or Mesh with GPU Instancing disabled | **UV**, **UV2**, and **AnimBlend**. Connect **UV** and **UV2** to the **UV** input's xy and zw components, and **AnimBlend** to the **Blend Vertex Stream** input. | For Mesh render mode, the UV1 channel of the mesh must match its UV0 channel. |
| Mesh with GPU Instancing enabled | None. Use the [Particle Anim Frame node](Particle-Anim-Frame-Node.md) (**AnimFrame**) instead of the **UV2** and **AnimBlend** streams. | The UV2 channel of the mesh must match its UV0 channel. |

## Create Node menu category

The Particle Texcoords node is under the **VFX** &gt; **Particles** category in the Create Node menu.

## Additional resources

- [Reflected Function node](Reflected-Function-Node.md)
- [Particle Flipbook Blending node](Particle-Flipbook-Blending-Node.md)
- [Particle Anim Frame node](Particle-Anim-Frame-Node.md)
[!include[nodes-example-samples](./snippets/nodes-example-samples.md)]
