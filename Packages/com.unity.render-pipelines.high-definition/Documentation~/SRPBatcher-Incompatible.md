# Remove SRP Batcher compatibility for GameObjects

In some rare cases, you might want to intentionally make particular GameObjects incompatible with the SRP Batcher. For example, if you want to use [GPU instancing](https://docs.unity3d.com/6000.0/Documentation/Manual/GPUInstancing), which isn't compatible with the SRP Batcher. If you want to render many identical meshes with the exact same material, GPU instancing can be more efficient than the SRP Batcher. To use GPU instancing, you must either:

* Use [Graphics.RenderMeshInstanced](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Graphics.RenderMeshInstanced).
* Manually remove SRP Batcher compatibility and enable GPU instancing for the material.

There are two ways to remove compatibility with the SRP Batcher from a GameObject:

* Make the shader incompatible.
* Make the renderer incompatible.

**Tip**: If you use GPU instancing instead of the SRP Batcher, use the [Profiler](https://docs.unity3d.com/6000.0/Documentation/Manual/Profiler) to make sure that GPU instancing is  more efficient for your application than the SRP Batcher.

## Removing shader compatibility

You can make both hand-written and Shader Graph shaders incompatible with the SRP Batcher. However, for Shader Graph shaders, if you change and recompile the Shader Graph often, it's simpler to make the [renderer incompatible](#removing-renderer-compatibility) instead.

To make a Unity shader incompatible with the SRP Batcher, you need to make changes to the shader source file:

1. For hand-written shaders, open the shader source file. For Shader Graph shaders, copy the Shader Graph's compiled shader source code into a new shader source file. Use the new shader source file in your application instead of the Shader Graph.
2. Add a new [material property](https://docs.unity3d.com/6000.0/Documentation/Manual/SL-Properties) declaration into the shader’s `Properties` block. Don't declare the new material property in the `UnityPerMaterial` constant buffer.

The material property doesn't need to do anything; just having a material property that doesn't exist in the `UnityPerMaterial` constant buffer  makes the shader incompatible with the SRP Batcher.

**Warning**: If you use a Shader Graph, be aware that every time you edit and recompile the Shader Graph, you must repeat this process.

<a name="removing-renderer-compatibility"></a>

## Removing renderer compatibility

You can make individual renderers incompatible with the SRP Batcher. To do this, add a `MaterialPropertyBlock` to the renderer.
