# Contexts and Blocks

This section contains reference documentation for the Shader Graph [Contexts](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Master-Stack.html%23contexts) available in the High Definition Render Pipeline (HDRP). Each page describes the Context itself, the shader stage the Context represents, and the list of compatible HDRP-specific [Blocks](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Block-Node.html).

In HDRP, many Blocks exist solely to set properties relevant for particular settings in the [Graph Settings menu](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Graph-Settings-Menu.html). If you enable one of these settings, Shader Graph automatically adds any Block relevant for that setting to the Context. If you disable the setting, Shader Graph automatically removes the relevant Blocks from the Context.

This relationship between settings and Blocks has the following consequences:

- If you enable a setting and delete any of its relevant Blocks, the Context uses the deleted Block's **Default Value** when Shader Graph builds the final shader.
- If you add a Block relevant to a particular setting and don't enable that setting, Shader Graph ignores the Block and any connected Nodes when it builds the final Shader.

The list of Contexts is as follows:

- [Vertex Context](ss-context-vertex.md)
- [Fragment Context](ss-context-fragment.md)
