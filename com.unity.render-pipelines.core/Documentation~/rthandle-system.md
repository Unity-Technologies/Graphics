# The RTHandle system

Render target management is an important part of any render pipeline. In a complicated render pipeline where there are many interdependent render passes that use many different render textures, it is important to have a maintainable and extendable system that allows for easy memory management.


One of the biggest issues occurs when a render pipeline uses many different cameras, each with their own resolution. For example, off-screen cameras or real-time reflection probes. In this scenario, if the system allocated render textures independently for each camera, the total amount of memory would increase to unmanageable levels. This is particularly bad for complex render pipelines that use many intermediate render textures. Unity can use [temporary render textures](https://docs.unity3d.com/ScriptReference/RenderTexture.GetTemporary.html), but unfortunately, they do not suit this kind of use case because temporary render textures can only reuse memory if a new render texture uses the exact same properties and resolution. This means that when rendering with two different resolutions, the total amount of memory Unity uses is the sum of all resolutions.

To solve these issues with render texture memory allocation, Unity's Scriptable Render Pipeline includes the RTHandle (RTHandle) system. This system is an abstraction layer over Unity's [RenderTexture](https://docs.unity3d.com/ScriptReference/RenderTexture.html) API that handles render texture management automatically.

This section contains the following pages:

- [RTHandle system fundamentals](rthandle-system-fundamentals.md)
- [Using the RTHandle system](rthandle-system-using.md)