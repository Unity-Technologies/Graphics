# Particle Transforms node

## Description

The Particle Transforms node transforms a mesh particle's position, normal, and tangent from Object (particle) space to Object (system) space, using the per-particle transform matrix that the Particle System supplies through GPU instancing.

Use this node when a Particle System uses **Mesh** render mode with **GPU Instancing** enabled. When GPU Instancing is disabled, or the render mode isn't **Mesh**, the node passes each value through unchanged.

This node is a [Reflected Function node](Reflected-Function-Node.md). Its ports and behavior come from the `TransformParticleMesh` HLSL function in `Packages/com.unity.render-pipelines.core/ShaderLibrary/ParticlesInstancing.hlsl`.

This node relies on the particle instancing data buffer. To use it, add the following pragma to the **Preprocessor Directives** field in **Graph Settings**:

```
instancing_options procedural:ParticleInstancingSetup
```

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:---|:---|:---|:---|:---|
| **Position** | Input | Vector 3 | Position (Object) | The vertex or fragment position to transform. When not connected, defaults to the Object space output of the [Position node](Position-Node.md). |
| **Normal** | Input | Vector 3 | Normal (Object) | The normal to transform. When not connected, defaults to the Object space output of the [Normal Vector node](Normal-Vector-Node.md).|
| **Tangent** | Input | Vector 3 | Tangent (Object) | The tangent to transform. When not connected, defaults to the Object space output of the [Tangent Vector node](Tangent-Vector-Node.md). |
| **Position** | Output | Vector 3 | None | The position, transformed to Object (system) space when GPU Instancing is active. |
| **Normal** | Output | Vector 3 | None | The normal, transformed to Object (system) space when GPU Instancing is active. |
| **Tangent** | Output | Vector 3 | None | The tangent, transformed to Object (system) space when GPU Instancing is active. |

## Create Node menu category

The Particle Transforms node is under the **VFX** &gt; **Particles** category in the Create Node menu.

## Additional resources

- [Reflected Function node](Reflected-Function-Node.md)
- [Particle Color node](Particle-Color-Node.md)
- [Particle Texcoords node](Particle-Texcoords-Node.md)
[!include[nodes-example-samples](./snippets/nodes-example-samples.md)]
