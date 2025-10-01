# Resources in the render graph system in SRP Core

Access and use textures and other resources in a custom scriptable render pipeline (SRP) that uses the render graph system.

**Note:** This page is about creating a custom render pipeline. To use the render graph system in a prebuilt Unity pipeline, refer to either [Render graph system in URP](https://docs.unity3d.com/Manual/urp/render-graph.html) or [Render graph system in HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/render-graph-introduction.html).

|**Topic**|**Description**|
|-|-|
|[Introduction to resources in the render graph system](render-graph-resources.md)|Learn about how to manage resources in the render graph system.|
|[Blit using the render graph system](render-graph-blit.md)|To blit from one texture to another in the render graph system, use the `AddBlitPass` API.|
|[Create a texture in the render graph system](render-graph-create-a-texture.md)|Create a texture in a render graph system render pass.|
|[Import a texture into the render graph system](render-graph-import-a-texture.md)|To create or use a render texture in a render graph system render pass, use the `RTHandle` API.|
|[Use a texture in a render pass](render-graph-read-write-texture.md)|To allow a render pass to read from or write to a texture, use the render graph system API to set the texture as an input or output.|
