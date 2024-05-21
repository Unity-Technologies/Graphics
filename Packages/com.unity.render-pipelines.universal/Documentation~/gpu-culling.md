# Use GPU occlusion culling

GPU occlusion culling means Unity uses the GPU instead of the CPU to exclude objects from rendering when they're occluded behind other objects. Unity uses this information to speed up rendering in scenes that have a lot of occlusion.

The GPU Resident Drawer works only with the following:

- The [Forward+](rendering/forward-plus-rendering-path.md) rendering path.
- [Graphics APIs](https://docs.unity3d.com/6000.0/Documentation/Manual/GraphicsAPIs.html) and platforms that support compute shaders.

## How GPU occlusion culling works

Unity generates depth textures from the perspective of cameras and lights in the scene.

The GPU then uses the depth textures from the current frame and the previous frame to cull objects. Unity renders only objects that are unoccluded in either frame. Unity culls the remaining objects, which are occluded in both frames.

Whether GPU occlusion culling speeds up rendering depends on your scene. GPU occlusion culling is most effective in the following setups:

- Multiple objects use the same mesh, so Unity can group them into a single draw call.
- The scene has a lot of occlusion, especially if the occluded objects have a high number of vertices.

If occlusion culling doesn't have a big effect on your scene, rendering time might increase because of the extra work the GPU does to set up GPU occlusion culling. 

## Enable GPU occlusion culling

1. Go to **Graphics**, select the **URP** tab, then in the **Render Graph** section make sure **Compatibility Mode (Render Graph Disabled)** is disabled. 
2. [Enable the GPU Resident Drawer](gpu-resident-drawer.md#enable-the-gpu-resident-drawer).
3. In the active [Universal Renderer](urp-universal-renderer.md), enable **GPU Occlusion**. 

## Analyze GPU occlusion culling

You can use the following to analyze GPU occlusion culling:

- [Rendering Statistics](https://docs.unity3d.com/Manual/RenderingStatistics.html) overlay to check rendering speed increases.
- [Rendering Debugger](features/rendering-debugger.md#gpu-resident-drawer) to troubleshoot issues.

## Additional resources

- [Reduce rendering work on the CPU](reduce-rendering-work-on-cpu.md)
- [Occlusion culling](https://docs.unity3d.com/Manual/OcclusionCulling.html)
