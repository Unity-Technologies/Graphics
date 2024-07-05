---
uid: urp-render-graph-framebuffer-fetch
---
# Get the current framebuffer with framebuffer fetch

To speed up rendering, use framebuffer fetch to read the frame that Unity has rendered so far.

Using framebuffer fetch means the render pass can access the framebuffer from the on-chip memory of the GPU, instead of video memory. This speeds up rendering, because the GPU doesn't need to copy the texture to and from its video memory.

Framebuffer fetch is supported if you use one of the following graphics APIs:

- Vulkan
- Metal

Framebuffer fetch speeds up rendering a lot on mobile devices that use tile-based deferred rendering (TBDR). The GPU keeps the frame in its on-chip tile memory between render passes, using less memory bandwidth and processing time.

If you use framebuffer fetch, URP merges the render passes that write to the framebuffer and read from it. You can check this in the [Render Graph Viewer window](render-graph-viewer-reference.md).

## Use framebuffer fetch

To use framebuffer fetch in a render pass, use the following:

- The `SetInputAttachment` API to set the output of the previous render pass as the input of the new render pass.
- The `LOAD_FRAMEBUFFER_X_INPUT` macro in your fragment shader code to sample the output of the previous render pass, instead of a macro such as `SAMPLE_TEXTURE2D`.

The following steps assume you already have one render pass that writes to a `TextureHandle` called `sourceTextureHandle`.

1. Create a custom shader, then create a material from the shader. For more information about creating a custom shader, refer to [Writing custom shaders](writing-custom-shaders-urp.md).

2. Inside the HLSLPROGRAM of your custom shader, make sure you import the `Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl` file:

    ```hlsl
    HLSLPROGRAM
    ...
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ...
    ENDHLSL
    ```

3. Inside the HLSLPROGRAM, use one of the following to declare the texture from the previous render pass. For example:

    - `FRAMEBUFFER_INPUT_X_HALF`
    - `FRAMEBUFFER_INPUT_X_FLOAT`
    - `FRAMEBUFFER_INPUT_X_INT`
    - `FRAMEBUFFER_INPUT_X_UINT`

    For example:

    ```hlsl
    FRAMEBUFFER_INPUT_X_HALF(0);
    ```

4. In the fragment function, use `LOAD_FRAMEBUFFER_X_INPUT` to sample the texture using framebuffer fetch. For example:
    
    ```hlsl
    half4 frag() : SV_Target
    {
        half4 colorFromPreviousRenderPass = LOAD_FRAMEBUFFER_X_INPUT(0, input.positionCS.xy);
        return colorFromPreviousRenderPass;
    }
    ```

5. In a new render graph render pass, add the material you created to your `PassData`. For example:

    ```csharp
    class PassData
    {
        public Material frameBufferFetchMaterial;
    }
    ```

6. Use `builder.SetInputAttachment` to set the output of the previous render pass as the input of the new render pass. For example:

    ```csharp
    builder.SetInputAttachment(sourceTextureHandle, 0, AccessFlags.Read);
    ```

7. In your `SetRenderFunc` method, use a command such as `BlitTexture` to render using the material. For example:

    ```csharp
    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), frameBufferFetchMaterial, 1);
    }
    ```

## Example

For a full example, refer to the example called **FrameBufferFetch** in the [render graph system URP package samples](package-samples.md).

## Additional resources

- [Writing custom shaders](writing-custom-shaders-urp.md)
- [Write a render pass using the render graph system](render-graph-write-render-pass.md)
- [Transfer a texture between render passes](render-graph-pass-textures-between-passes.md)
