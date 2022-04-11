# Master Stacks in HDRP

Shader Graphs in the High Definition Render Pipelines (HDRP) have an HDRP-specific section in the [Graph Settings menu](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Graph-Settings-Menu.html). In the **HDRP** section, you can edit settings relevant for building the final HDRP shader. The **Material** property specifies the type of shader to build, and the settings available in the Graph Settings menu change depending on the option you select. When the available settings change, Shader Graph adds relevant [Blocks](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Block-Node.html) to the [Contexts](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Master-Stack.html%23contexts) in the graph.

This documentation begins with an introduction to the Shader Graph Contexts and Blocks available in HDRP:

* [Contexts and Blocks](ss-contexts-and-blocks.md)

It then describes the HDRP-specific material types you can create with Shader Graph. Each material type page contains information on which settings the material type adds to the Graph Settings menu, and which Blocks the material type adds to the Master Stack by default. The list of material type pages is as follows:

* [Decal Master Stack](master-stack-decal.md)
* [Eye Master Stack](master-stack-eye.md)
* [Fabric Master Stack](master-stack-fabric.md)
* [Hair Master Stack](master-stack-hair.md)
* [Lit Master Stack](master-stack-lit.md)
* [StackLit Master Stack](master-stack-stacklit.md)
* [Unlit Master Stack](master-stack-unlit.md)
