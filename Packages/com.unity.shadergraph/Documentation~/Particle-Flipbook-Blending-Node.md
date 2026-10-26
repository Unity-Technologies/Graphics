# Particle Flipbook Blending node

The Particle Flipbook Blending node samples a texture twice — once for the current particle animation frame and once for the next — and blends the two samples together. Use it with a Particle System that has a **Texture Sheet Animation** module enabled, to get smooth transitions between animation frames.

[!include[nodes-subgraph-node](./snippets/nodes-subgraph-node.md)]

This node adds a `_FLIPBOOKBLENDING_ON` shader feature keyword to your graph. When the keyword is disabled (the default), the node only samples the texture with the UVs of the current animation frame.

## Vertex streams and mesh requirements

What Custom Vertex Streams and mesh setup this node needs depends on the render mode of the particle system and whether GPU Instancing is enabled:

| **Render mode** | **Required Custom Vertex Streams** | **Mesh requirements** |
|:---|:---|:---|
| Billboard, or Mesh with GPU Instancing disabled | **Color**, **UV**, **UV2**, and **AnimBlend**.<br>**Normal** is also required for Lit shaders, and **Tangent** if the shader uses normal maps. | For Mesh render mode, the mesh's UV1 channel must match its UV0 channel. |
| Mesh with GPU Instancing enabled | None. | None. |

For more information on setting up Custom Vertex Streams, refer to [Particle System Vertex Streams](https://docs.unity3d.com/Manual/PartSysVertexStreams.html) in the Unity Manual.

## Create Node menu category

The Particle Flipbook Blending node is under the **VFX** &gt; **Particles** category in the Create Node menu.

## Inputs

[!include[nodes-inputs](./snippets/nodes-inputs.md)]

| **Name** | **Type** | **Description** |
|:---|:---|:---|
| **Texture** | Texture 2D | The texture to sample. |
| **UV** | Vector 2 | The source UV coordinates, typically from the **UV** Custom Vertex Stream (see [Vertex streams and mesh requirements](#vertex-streams-and-mesh-requirements)). |
| **Blend Vertex Stream** | Float | The value used to compute the blend factor between the current and next animation frame, typically from the **AnimBlend** Custom Vertex Stream (see [Vertex streams and mesh requirements](#vertex-streams-and-mesh-requirements)). |

## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
|:---|:---|:---|
| **RGBA** | Vector 4 | The blended color sample. |

## Additional resources

- [Particle Texcoords node](Particle-Texcoords-Node.md)
- [Sample Texture 2D node](Sample-Texture-2D-Node.md)
[!include[nodes-example-samples](./snippets/nodes-example-samples.md)]
