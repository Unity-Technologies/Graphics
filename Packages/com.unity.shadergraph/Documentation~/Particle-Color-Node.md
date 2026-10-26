# Particle Color node

## Description

The Particle Color node outputs the particle color of the current mesh particle.

- If the Particle System uses **Billboard** render mode, or **Mesh** render mode with GPU Instancing disabled, the node outputs its **Color** input unchanged.
- If the Built-in Particle System uses **Mesh** render mode with GPU instancing enabled, and the mesh has vertex colors, the node multiplies its **Color** input by the per-particle color. If the mesh has no vertex colors, the node ignores its **Color** input and outputs the per-particle color.

This node is a [Reflected Function node](Reflected-Function-Node.md). Its ports and behavior come from the `GetParticleColor` HLSL function in `Packages/com.unity.render-pipelines.core/ShaderLibrary/Particles.hlsl`.

This node relies on the particle instancing data buffer. To use it, add the following pragma to the **Preprocessor Directives** field in **Graph Settings**:

```
instancing_options procedural:ParticleInstancingSetup
```

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:---|:---|:---|:---|:---|
| **Color** | Input | Vector 4 | Vertex Color | The base color. Defaults to the mesh's vertex color when not connected. |
| **Color** | Output | Vector 4 | None | The input color, multiplied by the per-particle color when GPU Instancing is active. |

## Create Node menu category

The Particle Color node is under the **VFX** &gt; **Particles** category in the Create Node menu.

## Additional resources

- [Reflected Function node](Reflected-Function-Node.md)
- [Particle Transforms node](Particle-Transforms-Node.md)
- [Vertex Color node](Vertex-Color-Node.md)
[!include[nodes-example-samples](./snippets/nodes-example-samples.md)]
