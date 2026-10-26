# Particle Anim Frame node

## Description

The Particle Anim Frame node provides the current Texture Sheet Animation frame index for a mesh particle.

When the Particle System uses **Mesh** render mode with GPU Instancing enabled, the node outputs the animation frame that the Particle System supplies through GPU instancing, instead of the value connected to its input.

This node is a [Reflected Function node](Reflected-Function-Node.md). Its ports and behavior come from the `GetParticleAnimFrame` HLSL function in `Packages/com.unity.render-pipelines.core/ShaderLibrary/Particles.hlsl`.

This node relies on the particle instancing data buffer. To use it, add the following pragma to the **Preprocessor Directives** field in **Graph Settings**:

```
instancing_options procedural:ParticleInstancingSetup
```

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:---|:---|:---|:---|:---|
| **Anim Frame** | Input | Float | None | The animation frame to use when GPU Instancing is disabled, typically supplied by the AnimFrame Custom Vertex Stream. |
| **Anim Frame** | Output | Float | None | The current particle's animation frame index. |

## Create Node menu category

The Particle Anim Frame node is under the **VFX** &gt; **Particles** category in the Create Node menu.

## Additional resources

- [Reflected Function node](Reflected-Function-Node.md)
- [Particle Texcoords node](Particle-Texcoords-Node.md)
[!include[nodes-example-samples](./snippets/nodes-example-samples.md)]
