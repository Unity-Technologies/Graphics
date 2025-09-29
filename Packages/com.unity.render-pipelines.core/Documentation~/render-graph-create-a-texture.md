# Create a texture in the render graph system in SRP Core

To create a texture in the render graph system, use the `CreateTexture` method of the render graph instance.

When you create a texture inside a render pass, the render graph system handles the creation and disposal of the texture. This process means the texture might not exist in the next frame, and other cameras might not be able to use it. To make sure a texture is available across frames and cameras, [import a texture](render-graph-import-a-texture.md) instead.

When the render graph system optimizes the render pipeline, it might not create a texture if the final frame doesn't use the texture, to reduce the memory and bandwidth the render passes use.

## Create a texture

To create a texture, in follow these steps:

1. Create a [`TextureDesc`](xref:UnityEngine.Rendering.RenderGraphModule.TextureDesc) object with the texture properties you need.

2. Use the [`CreateTexture`](xref:UnityEngine.Rendering.RenderGraphModule.RenderGraph.CreateTexture(UnityEngine.Rendering.RenderGraphModule.TextureDesc@)) method of the render graph instance to create a texture and return a texture handle.

For example, the following creates a texture the same size as the screen.

``` lang-cs
void RenderCamera(ScriptableRenderContext context, Camera cameraToRender)
{
    ...

    var textureProperties = new TextureDesc(Vector2.one)
    {
        colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
        width = cameraToRender.pixelWidth,
        height = cameraToRender.pixelHeight,
        clearBuffer = true,
        clearColor = Color.blue,
        name = "New render graph Texture",
    };

    TextureHandle tempTexture = renderGraph.CreateTexture(textureProperties);

    ...
}
```

You can then [use the texture](render-graph-read-write-texture.md) in the same custom render pass.

The render graph system manages the lifetime of textures you create with `CreateTexture`, so you don't need to manually release the memory they use when you're finished with them.

## Additional resources

- [Import a texture into the render graph system](render-graph-import-a-texture.md)
- [Textures](https://docs.unity3d.com/Manual/Textures.html)

