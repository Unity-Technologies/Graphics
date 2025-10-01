# Creating a custom render pipeline using the render graph system

The render graph system is a set of APIs you can use to write a custom scriptable render pipeline in a modular, maintainable way.

**Note:** This section is about creating a custom render pipeline. To use the render graph system in a prebuilt Unity pipeline, refer to either [Render graph system in URP](https://docs.unity3d.com/Manual/urp/render-graph.html) or [Render graph system in HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/render-graph-introduction.html).

|**Topic**|**Description**|
|-|-|
|[Introduction to the render graph system](render-graph-system.md)|What the render graph system is, and how it optimizes rendering.|
|[Write a render pipeline with render graph](render-graph-writing-a-render-pipeline.md)|Write the outline of a render pipeline that uses the render graph APIs.|
|[Write a render pass using the render graph system](render-graph-write-render-pass.md)|Record and execute a render pass in the render graph system.|
|[Resources in the render graph system](render-graph-resources-landing.md)|Import and use textures and other resources in a render pass.|
|[Use the CommandBuffer interface in a render graph](render-graph-unsafe-pass.md)|Use `CommandBuffer` interface APIs such as `SetRenderTarget` in render graph system render passes.|

## Additional resources

- [Creating a custom render pipeline](srp-custom.md)
- [RTHandle system](rthandle-system.md)
