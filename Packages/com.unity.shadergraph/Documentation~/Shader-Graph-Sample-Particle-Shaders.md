# Particle Shaders sample

Learn how to build shaders for particle systems with Shader Graph, and how to reproduce particle effects such as flipbook blending, soft particles, and distortion.

The Particle Shaders sample contains a set of shader graphs that demonstrate how to use Shader Graph with the [Built-in Particle System](https://docs.unity3d.com/Manual/Built-inParticleSystem.html). The shader graphs use [particle nodes](Particle-Nodes.md) to read standard per-particle data such as transforms, color, and Texture Sheet Animation coordinates, so the same graph supports both billboard and mesh render modes, with GPU Instancing enabled or disabled. Custom per-particle data, such as the age of a particle, reaches the shader through a different path in each render mode.

Several of the examples reproduce effects that the Universal Render Pipeline (URP) particle shaders provide as built-in options, such as soft particles, camera fading, and distortion. Use them as a starting point when you need the same effects in a custom shader, or when you migrate a project from the particle shaders of another render pipeline.

The sample includes one scene for URP and one scene for the High Definition Render Pipeline (HDRP). Both scenes display the examples side by side, so you can compare the effects and inspect the shader graph that each one uses.

## Examples in the sample

| **Example** | **Description** |
| :--- | :--- |
| **GPU Instancing** | Reads per-particle position, color, and Texture Sheet Animation coordinates with the [Particle Transforms](Particle-Transforms-Node.md), [Particle Color](Particle-Color-Node.md), and [Particle Texcoords](Particle-Texcoords-Node.md) nodes, so that one shader supports the **Billboard** and **Mesh** render modes, with GPU Instancing enabled or disabled. |
| **Flipbook Blending** | Blends between the current and next frame of a **Texture Sheet Animation** module with the [Particle Flipbook Blending](Particle-Flipbook-Blending-Node.md) node, to smooth the transition between frames. The example displays one particle system with blending enabled and one with blending disabled. |
| **Soft Particles** | Fades transparent particles as they approach opaque geometry, to avoid the hard edges that appear where particles intersect other GameObjects. The example displays one particle system with the effect applied and one without it. |
| **Camera Fading** | Fades transparent particles as they approach the camera, to avoid the hard edges that appear where particles cross the near clipping plane. |
| **Distortion** | Distorts the background behind transparent particles with a normal map. |
| **Custom Particle Data (Particle Age)** | Uses the age of each particle to dissolve it, and demonstrates the two ways a shader can read custom particle data. For billboard particles, the shader reads the age from a Custom Vertex Stream. For mesh particles with GPU Instancing enabled, it reads the age from a custom particle instance data struct that `CustomParticleData.hlsl` defines. |

## Open the sample scene

To open the scene that contains the examples, follow these steps:

1. [Import](ShaderGraph-Samples-Import.md) the Particle Shaders sample into your project.

1. In the **Project** window, open the `Assets/Samples/Shader Graph/<your version>/Particle Shaders` folder.

1. Open the scene that matches the render pipeline your project uses:

    * For URP, open `URP_Particles_Shaders`.
    * For HDRP, open `HDRP_Particles_Shaders`.

The scene displays each example on its own stand, with a panel that contains the name and description of the example.

## Display the examples

To view the examples in action and focus on each one in turn with the camera, follow these steps:

1. Enter Play mode to run all particle systems present in the scene.

1. In the **Hierarchy** window, select the `Particle Shader Graph Samples Showcase` GameObject.

1. In the **Inspector** window, set **Samples** to the example you want to display, or select the arrow controls to move to the next or previous example.

The camera moves to frame the selected example, and the panel beside it displays the description of the example, the aspects it covers, and links to the documentation for the nodes it uses.

## Inspect the particle system of an example

Each example uses a particle system to emit the particles that its shader renders. In the **Inspector** window, in the description of the example, select **Show Particle System**. Unity highlights the `Particle System` GameObject of the example in the **Hierarchy** window.

Select that GameObject to view the settings of the particle system in the **Inspector** window, and to control the simulation from the **Particles** overlay in the **Scene** view. For more information, refer to [Built-in Particle System](https://docs.unity3d.com/Manual/Built-inParticleSystem.html) in the Unity Manual.

## Open the shader graph of an example

Each example uses its own shader graph asset, which contains sticky notes that explain how the effect works.

To open it, in the **Inspector** window, in the description of the example, select **Open Shader Graph**. Unity opens the graph in the [Shader Graph window](Shader-Graph-Window.md).

Alternatively, open the graph assets directly from the `Shader Graphs` subfolder of the sample.

## Additional resources

* [Particle nodes](Particle-Nodes.md)
* [Import Shader Graph samples](ShaderGraph-Samples-Import.md)
* [Shader Graph samples](ShaderGraph-Samples.md)
* [Particle System Vertex Streams](https://docs.unity3d.com/Manual/PartSysVertexStreams.html) in the Unity Manual
* [Texture Sheet Animation module](https://docs.unity3d.com/Manual/PartSysTexSheetAnimModule.html) in the Unity Manual
