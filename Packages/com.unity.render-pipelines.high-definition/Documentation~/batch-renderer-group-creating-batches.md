---
uid: um-batch-renderer-group-creating-batches
---

# Create batches with the BatchRendererGroup API

BatchRendererGroup (BRG) doesn't automatically provide any instance data. Instance data includes many properties which are normally built in for GameObjects, such as transform matrices, light probe coefficients, and lightmap texture coordinates. This means features like ambient lighting only work if you provide instance data yourself. To do this, you add and configure batches. A batch is a collection of instances, where each instance corresponds to a single thing to render. What the instance actually represents depends on what you want to render. For example, in a prop object renderer, an instance could represent a single prop, and in a terrain renderer, it could represent a single terrain patch.

Each batch has a set of metadata values and a single [GraphicsBuffer](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GraphicsBuffer), which every instance in the batch shares. To load data for an instance, the typical process is to use the metadata values to load from the correct location in the GraphicsBuffer.  The `UNITY_ACCESS_DOTS_INSTANCED_PROP` family of shader macros work with this scheme (see [Accessing DOTS Instanced properties](dots-instancing-shaders.md#accessing-dots-instanced-properties)). However, you don't need to use this per-instance data loading scheme, and you are free to implement your own scheme if you want.

To create a batch, use [BatchRendererGroup.AddBatch](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchRendererGroup.AddBatch). The method receives an array of metadata values as well as a handle to a GraphicsBuffer. Unity passes the metadata values to the shader when it renders instances from the batch, and binds the GraphicsBuffer as `unity_DOTSInstanceData`. For metadata values that the shader uses but you don't pass in when you create a batch, Unity sets them to zero.

You can't modify batch metadata values after you create them, so any metadata values you pass to the batch are final. If you need to change any metadata values, create a new batch and remove the old one. You can modify the batch's GraphicsBuffer at any time. To do this, use [SetBatchBuffer](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchRendererGroup.SetBatchBuffer). This can be useful to resize buffers and allocate a larger buffer if the existing one runs out of space.

**Note**: You don't need to specify the size of a batch when you create one. Instead, you have to make sure that the shader can correctly handle the instance indices you pass to it. What this means depends on the shader. For Unity-provided SRP shaders, this means that there must be valid instance data in the buffer at the index you pass. 

See the following code sample for an example of how to create a batch with metadata values and a GraphicsBuffer of instance data. This code sample builds on the one in [Registering meshes and materials](batch-renderer-group-registering-meshes-and-materials.md).

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

    private GraphicsBuffer m_InstanceData;
    private BatchID m_BatchID;
    private BatchMeshID m_MeshID;
    private BatchMaterialID m_MaterialID;

    // Some helper constants to make calculations more convenient.
    private const int kSizeOfMatrix = sizeof(float) * 4 * 4;
    private const int kSizeOfPackedMatrix = sizeof(float) * 4 * 3;
    private const int kSizeOfFloat4 = sizeof(float) * 4;
    private const int kBytesPerInstance = (kSizeOfPackedMatrix * 2) + kSizeOfFloat4;
    private const int kExtraBytes = kSizeOfMatrix * 2;
    private const int kNumInstances = 3;

    // The PackedMatrix is a convenience type that converts matrices into
    // the format that Unity-provided SRP shaders expect.
    struct PackedMatrix
    {
        public float c0x;
        public float c0y;
        public float c0z;
        public float c1x;
        public float c1y;
        public float c1z;
        public float c2x;
        public float c2y;
        public float c2z;
        public float c3x;
        public float c3y;
        public float c3z;

        public PackedMatrix(Matrix4x4 m)
        {
            c0x = m.m00;
            c0y = m.m10;
            c0z = m.m20;
            c1x = m.m01;
            c1y = m.m11;
            c1z = m.m21;
            c2x = m.m02;
            c2y = m.m12;
            c2z = m.m22;
            c3x = m.m03;
            c3y = m.m13;
            c3z = m.m23;
        }
    }

    private void Start()
    {
        m_BRG = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
        m_MeshID = m_BRG.RegisterMesh(mesh);
        m_MaterialID = m_BRG.RegisterMaterial(material);

        AllocateInstanceDataBuffer();
        PopulateInstanceDataBuffer();
    }

    private void AllocateInstanceDataBuffer()
    {
        m_InstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
            BufferCountForInstances(kBytesPerInstance, kNumInstances, kExtraBytes),
            sizeof(int));
    }

    private void PopulateInstanceDataBuffer()
    {
        // Place a zero matrix at the start of the instance data buffer, so loads from address 0 return zero.
        var zero = new Matrix4x4[1] { Matrix4x4.zero };

        // Create transform matrices for three example instances.
        var matrices = new Matrix4x4[kNumInstances]
        {
            Matrix4x4.Translate(new Vector3(-2, 0, 0)),
            Matrix4x4.Translate(new Vector3(0, 0, 0)),
            Matrix4x4.Translate(new Vector3(2, 0, 0)),
        };

        // Convert the transform matrices into the packed format that the shader expects.
        var objectToWorld = new PackedMatrix[kNumInstances]
        {
            new PackedMatrix(matrices[0]),
            new PackedMatrix(matrices[1]),
            new PackedMatrix(matrices[2]),
        };

        // Also create packed inverse matrices.
        var worldToObject = new PackedMatrix[kNumInstances]
        {
            new PackedMatrix(matrices[0].inverse),
            new PackedMatrix(matrices[1].inverse),
            new PackedMatrix(matrices[2].inverse),
        };

        // Make all instances have unique colors.
        var colors = new Vector4[kNumInstances]
        {
            new Vector4(1, 0, 0, 1),
            new Vector4(0, 1, 0, 1),
            new Vector4(0, 0, 1, 1),
        };

        // In this simple example, the instance data is placed into the buffer like this:
        // Offset | Description
        //      0 | 64 bytes of zeroes, so loads from address 0 return zeroes
        //     64 | 32 uninitialized bytes to make working with SetData easier, otherwise unnecessary
        //     96 | unity_ObjectToWorld, three packed float3x4 matrices
        //    240 | unity_WorldToObject, three packed float3x4 matrices
        //    384 | _BaseColor, three float4s

        // Calculates start addresses for the different instanced properties. unity_ObjectToWorld starts
        // at address 96 instead of 64, because the computeBufferStartIndex parameter of SetData
        // is expressed as source array elements, so it is easier to work in multiples of sizeof(PackedMatrix).
        uint byteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
        uint byteAddressWorldToObject = byteAddressObjectToWorld + kSizeOfPackedMatrix * kNumInstances;
        uint byteAddressColor = byteAddressWorldToObject + kSizeOfPackedMatrix * kNumInstances;

        // Upload the instance data to the GraphicsBuffer so the shader can load them.
        m_InstanceData.SetData(zero, 0, 0, 1);
        m_InstanceData.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / kSizeOfPackedMatrix), objectToWorld.Length);
        m_InstanceData.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / kSizeOfPackedMatrix), worldToObject.Length);
        m_InstanceData.SetData(colors, 0, (int)(byteAddressColor / kSizeOfFloat4), colors.Length);

        // Set up metadata values to point to the instance data. Set the most significant bit 0x80000000 in each
        // which instructs the shader that the data is an array with one value per instance, indexed by the instance index.
        // Any metadata values that the shader uses that are not set here will be 0. When a value of 0 is used with
        // UNITY_ACCESS_DOTS_INSTANCED_PROP (i.e. without a default), the shader interprets the
        // 0x00000000 metadata value and loads from the start of the buffer. The start of the buffer is
        // a zero matrix so this sort of load is guaranteed to return zero, which is a reasonable default value.
        var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
        metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
        metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };
        metadata[2] = new MetadataValue { NameID = Shader.PropertyToID("_BaseColor"), Value = 0x80000000 | byteAddressColor, };

        // Finally, create a batch for the instances and make the batch use the GraphicsBuffer with the
        // instance data as well as the metadata values that specify where the properties are.
        m_BatchID = m_BRG.AddBatch(metadata, m_InstanceData.bufferHandle);
    }

    // Raw buffers are allocated in ints. This is a utility method that calculates
    // the required number of ints for the data.
    int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
    {
        // Round byte counts to int multiples
        bytesPerInstance = (bytesPerInstance + sizeof(int) - 1) / sizeof(int) * sizeof(int);
        extraBytes = (extraBytes + sizeof(int) - 1) / sizeof(int) * sizeof(int);
        int totalBytes = bytesPerInstance * numInstances + extraBytes;
        return totalBytes / sizeof(int);
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
        // This simple example doesn't use jobs, so it can just return an empty JobHandle.
        // Performance-sensitive applications should use Burst jobs to implement
        // culling and draw command output. In this case, this function would return a
        // handle here that completes when the Burst jobs finish.
        return new JobHandle();

    }
}
```

Now that you have registered all the required resources with a BatchRendererGroup object, you can create draw commands. For more information, see the next topic, [Creating draw commands](batch-renderer-group-creating-draw-commands.md).