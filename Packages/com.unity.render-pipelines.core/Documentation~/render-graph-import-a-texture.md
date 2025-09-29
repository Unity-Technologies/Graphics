# Import a texture into the render graph system in SRP Core

To use a texture from outside the render graph system, or to make sure a texture is available across frames and cameras, import it into the render graph system using the `ImportTexture` API.

For example, you can import the back buffer, or create a texture that points to a texture in your project, such as a [texture asset](https://docs.unity3d.com/Manual/ImportingTextures.html).

The render graph system doesn't manage the lifetime of imported textures, so it can't optimize the render graph by removing unneeded render passes. Where possible, [Create a texture inside the render graph system](render-graph-create-a-texture) instead, so the render graph system manages its lifetime. 

## Import a texture

To import a texture, follow these steps:

1. Create a [RenderTextureDescriptor](https://docs.unity3d.com/ScriptReference/RenderTextureDescriptor.html) object with the texture properties you need.

    For example:

    ``` lang-cs
    RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
    ```

2. Use the `RenderTextureDescriptor` to instantiate a new render texture. For example:

    ```lang-cs
    RenderTexture renderTexture = new RenderTexture(textureProperties);
    ```

3. Create a handle to the render texture. For example:

    ``` lang-cs
    RTHandle renderTextureHandle = RTHandles.Alloc(renderTexture);
    ```

4. Import the texture, to convert the `RTHandle` object to a `TextureHandle` object the render graph system can use. 

    For example:
    
    ``` lang-cs
    TextureHandle texture = renderGraph.ImportTexture(renderTextureHandle);
    ```

You can then use the `TextureHandle` object to [read from or write to the render texture](render-graph-read-write-texture.md).

<a name="dispose-of-a-render-texture"></a>
### Dispose of the render texture

You must free the memory a render texture uses at the end of a render pass.

For example, you can create the following `Dispose` method:

``` lang-cs
public void Dispose()
{
    renderTexture.Release();
}
```

## Additional resources

* [Textures](https://docs.unity3d.com/Manual/Textures.html)
* [The RTHandle system](rthandle-system.md)
