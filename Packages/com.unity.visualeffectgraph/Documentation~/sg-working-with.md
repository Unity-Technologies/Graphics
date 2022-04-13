# Working with Shader Graph in the Visual Effect Graph

Visual Effect Graphs can use compatible Shader Graphs to render particles. This enables you to visually build custom shaders for use in visual effects. This document explains:

1. How to make a Shader Graph compatible with Visual Effect Graphs.
2. How to set up a Visual Effect Graph to use a Shader Graph to render particles.

## Setting up your Shader Graph

To set up a Shader Graph so that it is compatible with the Visual Effect Graph, first, go to **Edit** > **Preferences** > **Visual Effects** and enable **Improved Shader Graph Generation**. Then, there are two methods you can use:

- Use the **Visual Effect** [Target](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Graph-Target.html). You can use this Target to make simple lit and unlit Shader Graphs.
- Use the **HDRP** Target and enable **Support VFX Graph** in the Shader Graph's [Graph Settings](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Graph-Settings-Menu.html) menu. You can use this Target to create Shader Graphs that support more complex, render pipeline-specific features.

### Using the Visual Effect Target

The Visual Effect Target is a Shader Graph [Target](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Graph-Target.html) specifically for visual effects. If a Shader Graph includes Visual Effect as an active Target, it is compatible with visual effects. To make a Shader Graph use the Visual Effect Target:

- If you already have a Shader Graph and want to make it use Visual Effect as an active Target:

- 1. Open your Shader Graph in the [Shader Graph window](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Shader-Graph-Window.html).
  2. In the [Graph Settings menu](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Graph-Settings-Menu.html), go to the **Active Targets** list.
  3. If the list does not include **Visual Effect**, click the **Add** button then select **Visual Effect**.

- If you want to create a new Shader Graph that uses the Visual Effect Target:

- 1. Create a new VFX Shader Graph (menu: **Assets** > **Create** > **Shader Graph** > **VFX Shader Graph**). This Shader Graph automatically includes Visual Effect as an active Target.

The Shader Graph is now compatible with the Visual Effect Graph. Also, the Graph Settings menu now contains a **Visual Effect** section which you can use to modify properties specific to rendering particles in a visual effect.

### Using the HDRP Target

The HDRP Target can be compatible with visual effects which enables you to render more complex-looking particles. For example, you can use the HDRP [Lit](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-lit.html), [Eye](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-eye.html), and [Hair](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-hair.html) Shader Graphs to render particles. Note that HDRP's [Decal Shader Graph](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-decal.html) does not support the Visual Effect Graph. To make a Shader Graph use the HDRP Target and be compatible with visual effects:

1. Open your Shader Graph in the [Shader Graph window](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Shader-Graph-Window.html). If you do not have a Shader Graph, create a new one (menu: **Assets** > **Create** > **Shader Graph** > **HDRP** then select the type of HDRP Shader Graph you want).
2. In the [Graph Settings menu](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Graph-Settings-Menu.html), go to the **Active Targets** list.
3. If the list does not include **HDRP**, click the **Add** button then select **HDRP**.
4. In the HDRP section of the Graph Settings menu, enable **Support VFX Graph**. The Shader Graph is now compatible with the Visual Effect Graph.

## Using a Shader Graph in a visual effect

After you set up a Shader Graph to be compatible with visual effects, Unity can use it to render particles. To make a visual effect use the Shader Graph:

1. Go to **Edit** > **Preferences** > **Visual Effects** and enable **Experimental Operators/Blocks**.

2. Open your Visual Effect Graph in the Visual Effect Graph window. If you do not have a Visual Effect Graph, create a new one (menu: **Assets** > **Create** > **Visual Effects** > **Visual Effect Graph**).

3. In the interface for output contexts, assign your compatible Shader Graph to the **Shader Graph** property. The following output contexts support Shader Graphs:

4. - [Particle Mesh](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest?subfolder=/manual/Context-OutputParticleMesh.html) (including Particle Lit Mesh)
   - [Particle Primitive](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest?subfolder=/manual/Context-OutputPrimitive.html) (including Particle Quad, Particle Triangle, Particle Octagon, Particle Lit Quad, Particle Lit Triangle, and Particle Lit Octagon)
   - Particle Strip Quad (including Particle Strip Lit Quad)

5. After you assign the Shader Graph, click on the output context and view it in the Inspector. Here, you can see all the Shader Graph's Surface Options. For the list of properties this contains, see the documentation for the type of Shader Graph you assigned. For example, if you assigned an HDRP Lit Shader Graph, see the documentation for the [Lit Shader Graph](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-lit.html). **Note**: Any edits you make here are local to the Visual Effect Graph and do not affect the Shader Graph asset itself.
