## RTHandle system fundamentals

This document describes the main principles of the RTHandle (RTHandle) system.

The RTHandle system is an abstraction on top of Unity's [RenderTexture](https://docs.unity3d.com/ScriptReference/RenderTexture.html) API. It makes it trivial to reuse render textures across Cameras that use various resolutions. The following principles are the foundation of how the RTHandle system works:

- You no longer allocate render textures yourself with a fixed resolution. Instead, you declare a render texture using a scale related to the full screen at a given resolution. The RTHandle system allocates the texture only once for the whole render pipeline so that it can reuse it for different Cameras.
- There is now the concept of reference size. This is the resolution the application uses for rendering. It is your responsibility to declare it before the render pipeline renders every Camera at a particular resolution. For information on how to do this, see the [Updating the RTHandle system](#updating-the-rthandle-system) section.
- Internally, the RTHandle system tracks the largest reference size you declare. It uses this as the actual size of render textures. The largest reference size is the maximum size.
- Every time you declare a new reference size for rendering, the RTHandle system checks if it is larger than the current recorded largest reference size. If it is, the RTHandle system reallocates all render textures internally to fit the new size and replaces the largest reference size with the new size.

An example of this process is as follows. When you allocate the main color buffer, it uses a scale of **1** because it is a full-screen texture. You want to render it at the resolution of the screen. A downscaled buffer for a quarter-resolution transparency pass would use a scale of **0.5** for both the x-axis and y-axis. Internally the RTHandle system allocates render textures using the largest reference size multiplied by the scale you declare for the render texture. After that and before each Camera renders, you tell the system what the current reference size is. Based on that and the scaling factor for all textures, the RTHandle system determines if it needs to reallocate render textures. As mentioned above, if the new reference size is larger than the current largest reference size, the RTHandle system reallocates all render textures. By doing this, the RTHandle system ends up with a stable maximum resolution for all render textures, which is most likely the resolution of your main Camera.


The key takeaway of this is that the actual resolution of the render textures is not necessarily the same as the current viewport: it can be bigger. This has implications when you write a renderer using RTHandles, which the [Using the RTHandle system](rthandle-system-using.md) documentation explains.

The RTHandleSystem also allows you to allocate textures with a fixed size. In this case, the RTHandle system never reallocates the texture. This allows you to use the RTHandle API consistently for both automatically-resized textures that the RTHandle system manages and regular fixed size textures that you manage.