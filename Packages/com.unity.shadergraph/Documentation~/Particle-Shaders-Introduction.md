# Introduction to particle shaders

Understand how a shader graph reads per-particle data, and which prebuilt assets to start from when you create a shader for a particle effect.

Shader Graph provides [particle nodes](Particle-Nodes.md) that read per-particle data, such as transforms, color, and **Texture Sheet Animation** coordinates, from the [Built-in Particle System](https://docs.unity3d.com/Manual/Built-inParticleSystem.html). Use these nodes to author a shader that reacts to each particle individually, and to combine per-particle data with any other Shader Graph functionality.

How per-particle data reaches your shader depends on the render mode that the Built-in Particle System uses, and on whether you enable GPU Instancing. These two settings determine which nodes you need, and how to set up the effect that uses your shader.

The Shader Graph package also provides templates, Subgraph nodes, and a sample for particle shaders, so you rarely need to start from an empty graph. This page describes how per-particle data reaches a shader, the starting point that each prebuilt asset provides, and where to find an existing implementation of common particle effects.

## Read per-particle data in a graph

The Built-in Particle System supplies per-particle data to a shader in one of the following ways:

- Vertex data: the Built-in Particle System writes per-particle values into the vertex data of the particles it renders, and the shader reads those values like any other vertex attribute. This path requires you to enable the matching Custom Vertex Streams.

- Instance data: when the Built-in Particle System uses **Mesh** render mode with **GPU Instancing** enabled, it writes per-particle values into an instancing data buffer, and the shader reads those values per instance. This path requires no Custom Vertex Streams.

Each particle node reads instance data when GPU Instancing is active. Otherwise, the node passes its input values through unchanged, which lets the same graph work in the render modes that supply per-particle data as vertex data. As a result, you can author one shader and use it with effects that render in different modes. Each node page describes what the node outputs in each render mode, and lists the Custom Vertex Streams and mesh setup that it needs. For more information about how to configure vertex streams, refer to [Particle System Vertex Streams](https://docs.unity3d.com/Manual/PartSysVertexStreams.html) in the Unity Manual.

To read instance data, a graph must declare the setup function that Unity uses for procedural instancing. Add the following pragma to the **Preprocessor Directives** field in **Graph Settings**:

```
instancing_options procedural:ParticleInstancingSetup
```

If per-particle data doesn't reach your shader, check that this pragma is present.

### Custom per-particle data

Beyond the standard values that the particle nodes expose, you can pass your own data to a shader, such as the age of each particle. Custom data follows the same two paths as standard data. For the vertex data path, add a Custom Vertex Stream. For the instance data path, extend the particle instance data struct in [High Level Shader Language (HLSL)](https://docs.unity3d.com/Manual/shaders-reference.html).

The instance data path requires you to declare the struct layout yourself. As a result, custom data can require hand-coded shader code even when the rest of your shader doesn't. For more information about how to write HLSL functions that Shader Graph exposes as nodes, refer to [Create custom nodes with HLSL](Custom-nodes-hlsl-landing.md).

## Choose a starting point

Shader Graph provides prebuilt assets at three levels of granularity. Templates provide complete graphs that you can create and edit, Subgraph nodes implement a single effect, and individual nodes expose raw per-particle data. The following table describes what each starting point provides and when to choose it.

| **Starting point** | **What it provides** | **When to choose it** |
|:---|:---|:---|
| **Particle Unlit** and **Particle Lit** templates | Complete unlit and lit graphs, set up to work with **Billboard** particles and with **Mesh** particles that have **GPU Instancing** enabled. Both templates already declare the instancing pragma. The lit template also includes **BaseMap**, **NormalMap**, and **MaskMap** texture parameters. | You want a working particle shader to customize, and your project uses the Universal Render Pipeline (URP) or the High Definition Render Pipeline (HDRP). |
| Subgraph nodes | Prebuilt implementations of individual effects, such as soft particles and distortion, that you can add to any graph. | You already have a graph and want to add one effect to it. |
| Particle nodes | Direct access to per-particle transforms, color, and **Texture Sheet Animation** coordinates. | You want full control over how the shader uses per-particle data. |
| Particle Shaders sample | A scene for URP and a scene for HDRP that display worked examples side by side, each with its own graph and explanatory sticky notes. | You want to inspect a working example before you build your own. |

To create a graph from a template, use the [Shader Graph template browser](template-browser.md), which lists the templates available for the render pipeline that your project uses. For more information about the ways to create a graph, refer to [Create a shader graph asset](Create-Shader-Graph.md).

Subgraph nodes and particle nodes both appear in the **VFX** > **Particles** category of the Create Node menu. A Subgraph node contains a graph rather than shader code. This means you can open one to inspect how it works, or copy its contents into your own graph as a starting point. For more information, refer to [Sub Graphs](Sub-graphs.md).

To add the Particle Shaders sample to your project, refer to [Import Shader Graph samples](ShaderGraph-Samples-Import.md).

## Build common particle effects

Several effects that the URP particle shaders provide as built-in options, such as soft particles and distortion, have an equivalent implementation in the Shader Graph package. Use these implementations when you need the same effect in a custom shader, or when you migrate a project from the particle shaders of another render pipeline. The following table indicates where to find an implementation of each effect.

| **Effect** | **Where to find an implementation** |
|:---|:---|
| Blend between **Texture Sheet Animation** frames | The [Particle Flipbook Blending node](Particle-Flipbook-Blending-Node.md), or the [Particle Texcoords node](Particle-Texcoords-Node.md) if you want to sample and blend the texture yourself. |
| Fade particles near opaque geometry | The `SoftParticles` Subgraph node, and the **Soft Particles** example in the Particle Shaders sample. |
| Fade particles near the camera | The `Particle Camera Fade` Subgraph node, and the **Camera Fading** example in the Particle Shaders sample. |
| Distort the background behind particles | The `Particle Distortion` Subgraph node, and the **Distortion** example in the Particle Shaders sample. |
| Tint particles with per-particle color | The [Particle Color node](Particle-Color-Node.md), or the `Particle Vertex Color` Subgraph node. |
| Drive an effect with custom data, such as particle age | The **Custom Particle Data (Particle Age)** example in the Particle Shaders sample. |

## Additional resources

- [Shader Graph samples](ShaderGraph-Samples.md)
- [Reflected Function node](Reflected-Function-Node.md)
- [Changing how shaders work using keywords](Keywords.md)
- [Texture Sheet Animation module](https://docs.unity3d.com/Manual/PartSysTexSheetAnimModule.html) in the Unity Manual
