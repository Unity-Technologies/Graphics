# Lit Tessellation material

The Lit Tessellation Shader allows you to create Materials that use tessellation to provide adaptive vertex density for meshes. This means that you can render more detailed geometry without the need to create a model that contains a lot of vertices. This Shader also includes options for effects like subsurface scattering, iridescence, vertex or pixel displacement, and decal compatibility. For more information about Materials, Shaders, and Textures, see the[ Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

![](Images/LitTessellationShader1.png)

**Tessellation Mode** set to **None** (off).

![](Images/LitTessellationShader2.png)

**Tessellation Mode** set to **Phong** (on).

## Creating a Lit Tessellation Material

To create a new Lit Tessellation Material:

1. Right-click in your Project's Asset window.
2. Select **Create** > **Material**. This adds a new Material to your Unity Project’s Asset folder.
3. Select the Material and, in the Inspector, select the **Shader** drop-down.
4. Select **HDRP** > **LitTessellation**.

Refer to [Lit Tessellation Material Inspector reference](lit-tessellation-material-inspector-reference.md) for more information.
