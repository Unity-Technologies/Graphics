# What's new in SRP Core version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the Core Render Pipeline package, embedded in Unity 2021.2.

## Improvements

### RTHandle System and MSAA

The RTHandle System no longer requires you to specify the number of MSAA samples at initialization time. This means that you can now set the number of samples on a per texture basis, rather than for the whole system.

In practice, this means that the initialization APIs no longer require MSAA related parameters. The `Alloc` functions have replaced the `enableMSAA` parameter and enables you to explicitly set the number of samples.

### New API to disable runtime Rendering Debugger UI

It is now possible to disable the Rendering Debugger UI at runtime by using [DebugManager.enableRuntimeUI](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/api/UnityEngine.Rendering.DebugManager.html#UnityEngine_Rendering_DebugManager_enableRuntimeUI).
