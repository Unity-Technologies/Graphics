# Layered Lit Tessellation material

The Layered Lit Tessellation Shader allows you to stack up to four tessellated Materials on the same GameObject in the High Definition Render Pipeline (HDRP).

The Materials that it uses for each layer are HDRP [Lit Tessellation Materials](lit-tessellation-material.md). This makes it easy to create layered Materials that provide adaptive vertex density for meshes. The **Main Layer** is the undermost layer and can influence upper layers with albedo, normals, and height. HDRP renders **Layer 1**, **Layer 2**, and **Layer 3** in that order on top of the **Main Layer**. For more information about Materials, Shaders, and Textures, see the[ Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

## Creating a Layered Lit Tessellation Material

To create a new Lit Tessellation Material:

1. Right-click in your Project's Asset window.
2. Select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.
3. Select the Material and, in the Inspector, select the **Shader** drop-down.
4. Select **HDRP > LayeredLitTessellation**.

Refer to [Layered Lit Tessellation Material Inspector reference](layered-lit-tessellation-material-inspector-reference.md) for more information.