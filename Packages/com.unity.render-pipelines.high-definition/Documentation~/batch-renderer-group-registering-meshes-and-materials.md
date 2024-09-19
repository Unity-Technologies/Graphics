---
uid: um-batch-renderer-group-registering-meshes-and-materials
---

# Register meshes and materials with the BatchRendererGroup API

[Mesh](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh) and [Material](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Material) are managed C# objects in Unity which means you can't use them from [Burst](https://docs.unity3d.com/Packages/com.unity.burst@latest) C# code. This means that to use them in BRG draw commands, you must pre-register with the BRG.

To register Mesh and Material objects, use [BatchRendererGroup.RegisterMesh](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchRendererGroup.RegisterMesh)  and [BatchRendererGroup.RegisterMaterial](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchRendererGroup.RegisterMaterial) respectively. These functions return a [BatchMeshID](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchMeshID) and a  [BatchMaterialID](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchMaterialID), respectively, which are plain data structs that contain a Burst-compatible handle. They are strongly typed to help prevent errors from accidentally using the wrong handle type.

You can register Mesh and Material objects at any time, including run time. The only requirements are:

* You need to register Mesh and Material objects before the BatchRendererGroup can use them to render.
* The Material must support DOTS Instancing.

You can also unregister Mesh and Material objects if you no longer need them. This is necessary if you want to unload any Mesh or Material objects. [BatchRendererGroup.Dispose](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchRendererGroup.Dispose) automatically unregisters all registered assets.

**Note**: You can't serialize a BatchMeshID or a BatchMaterialID. They are only valid with the BatchRendererGroup you register them with and become invalid if you unregister them, or if the BatchRendererGroup no longer exists. BatchMeshID and BatchMaterialID also become invalid if something forces Unity to unload the Mesh or Material objects, which happens when Unity unloads the Scene that the Mesh or Material objects are part of.

It is possible to register the same Mesh or Material object multiple times. This is useful in situations where you want to register Meshes or Materials without having to know which Meshes and Materials have been registered already. In this situation, the BatchRenderer keeps an internal count of the number of registrations in the following way:

* Each time you register a Mesh or Material object, the BatchRendererGroup increases its reference count by 1.
* Each time you unregister a Mesh or Material object, the BatchRendererGroup decreases its reference count by 1. If this causes the reference count to reach 0, the BatchRendererGroup unregisters the Mesh or Material. If you want to use the Mesh or Material in future draw commands, you must register it again.
* A RegisterMesh or RegisterMaterial call with an already registered Mesh or Material returns the same BatchMeshID or BatchMaterialID as the previous call. However, if the BatchRendererGroup completely unregistered the Mesh or Material, registering it again could return a different ID.

**Note**: BRG checks for modifications to Mesh or Material objects after the first OnPerformCulling callback method in a frame. This means that Unity takes any modification that occurs before that point into account. This includes changes you make in the first callback itself, but not changes that occur in any jobs scheduled by the callback. Modifying Mesh or Material objects after that point causes undefined behavior.

See the following code sample for an example of how to register meshes and materials with a BatchRendererGroup object. This code sample builds on the one in [Initializing a BatchRendererGroup object](batch-renderer-group-initializing.md).

```lang-csharp
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleBRGExample : MonoBehaviour
{
    public Mesh mesh;
    public Material material;

    private BatchRendererGroup m_BRG;

    private BatchMeshID m_MeshID;
    private BatchMaterialID m_MaterialID;

    private void Start()
    {
        m_BRG = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
        m_MeshID = m_BRG.RegisterMesh(mesh);
        m_MaterialID = m_BRG.RegisterMaterial(material);
    }

    private void OnDisable()
    {
        m_BRG.Dispose();
    }

    public unsafe JobHandle OnPerformCulling(
        BatchRendererGroup rendererGroup,
        BatchCullingContext cullingContext,
        BatchCullingOutput cullingOutput,
        IntPtr userContext)
    {
        // This simple example doesn't use jobs, so it can return an empty JobHandle.
        // Performance-sensitive applications should use Burst jobs to implement
        // culling and draw command output. In this case, this function would return a
        // handle that completes when the Burst jobs finish.
        return new JobHandle();
    }
}
```

Before you create any draw commands that use the registered Meshes and Materials, you need to provide data, like transform matrices, to use for the draw command instances. To provide data to use for each instance, BatchRendererGroup uses a concept called batches. For more information, see the next topic, [Creating batches](batch-renderer-group-creating-batches.md).