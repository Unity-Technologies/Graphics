# Access Custom Render Texture shader properties

If you want to create shaders, in Shader Graph, for use with Custom Render Textures, you need to understand how to access specific texture coordinates and other shader properties.

## Accessing the texture coordinates 

Shader Graph provides access to local and global texture coordinates via the [UV Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/UV-Node.html):
- Channel 0 (localTexcoord): Provides the local texture coordinates.
- Channel 1 (globalTexcoord): Provides the global texture coordinates.

To access these channels, add a UV node to your Shader Graph and select the appropriate channel.

## Access the cubemap view direction

For shaders that interact with Cubemaps, use the [View Direction](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/View-Direction-Node.html) node to retrieve the direction for the Cubemap sampling . Make sure that the space is set to **World Space** to get the correct direction vector.

## Access the Update Zone Index

You can access the update zone index via the [UV Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/UV-Node.html) as well.
- Channel 2 (PrimitiveID): Provides the Primitive ID. The primitive ID corresponds to the index of the update zone being rendered.

## Additional resources
- [Custom Render Textures](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html)
- [Custom Render Texture Nodes](Custom-Render-Texture-Nodes.md)
- [UV node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/UV-Node.html)
- [View Direction node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/View-Direction-Node.html)
