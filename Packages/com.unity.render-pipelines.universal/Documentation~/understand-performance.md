# Understand performance

The performance of your project depends on the Universal Render Pipeline (URP) features you use or enable, what your scenes contain, and which platforms you target. 

You can use the [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html) or a GPU profiler such as [RenderDoc](https://docs.unity3d.com/Manual/RenderDocIntegration.html) or [Xcode](https://docs.unity3d.com/Manual/XcodeFrameDebuggerIntegration.html) to check how much URP uses memory, the CPU and the GPU in your project. You can then use the information to enable or disable the features and settings that have the largest performance impact.

URP usually performs better if you change settings that reduce the following:

- How much URP uses the CPU. For example, you can disable URP updating volumes every frame.
- How much memory URP uses to store textures. For example, you can disable High Dynamic Range (HDR) if you don't need it, to reduce the size of the color buffer. 
- How many render textures URP copies to and from memory, which has a large impact on mobile platforms. For example, you can disable URP creating a depth texture if you don't need it.
- The number of passes in the render pipeline. For example, you can disable the opaque texture if you don't need it, or disable additional lights casting shadows.
- The number of draw calls URP sends to the GPU. For example, you can enable the SRP Batcher.
- The number of pixels URP renders to the screen, which has a big effect on mobile platforms where the GPU is less powerful. For example, you can reduce the render scale.

Refer to the following for more information about which settings to disable or change to improve performance:

- [Configure for better performance](configure-for-better-performance.md)
- [Optimize for better performance](optimize-for-better-performance.md)
