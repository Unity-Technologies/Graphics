using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe class BRGSetup : MonoBehaviour
{
    public Mesh m_mesh;
    public Material m_material;
    public float m_y;

    private BatchRendererGroup m_BatchRendererGroup;
    private ComputeBuffer m_GPUPersistentInstanceData;
    private BatchBufferID m_GPUPersistanceBufferId;

    private BatchID m_batchID;
    private BatchMaterialID m_materialID;
    private BatchMeshID m_meshID;
    private int m_itemCount;
    private bool m_initialized;

    public static T* Malloc<T>(int count) where T : unmanaged
    {
        return (T*)UnsafeUtility.Malloc(
            UnsafeUtility.SizeOf<T>() * count,
            UnsafeUtility.AlignOf<T>(),
            Allocator.TempJob);
    }

    static MetadataValue CreateMetadataValue(int nameID, int gpuAddress, bool isOverridden)
    {
        const uint kIsOverriddenBit = 0x80000000;
        return new MetadataValue
        {
            NameID = nameID,
            Value = (uint)gpuAddress | (isOverridden ? kIsOverriddenBit : 0),
        };
    }

    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        if (!m_initialized)
        {
            return new JobHandle();
        }

        BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

        drawCommands.batchDrawRangeCount = 1;
        drawCommands.batchDrawRanges = Malloc<BatchDrawRange>(1);
        drawCommands.batchDrawRanges[0] = new BatchDrawRange
        {
            drawCommandsBegin = 0,
            drawCommandsCount = 1,
            filterSettings = new BatchFilterSettings
            {
                renderingLayerMask = 1,
                layer = 0,
                motionMode = MotionVectorGenerationMode.Camera,
                shadowCastingMode = ShadowCastingMode.On,
                receiveShadows = true,
                staticShadowCaster = false,
                allDepthSorted = false
            }
        };

        drawCommands.drawCommandCount = 1;
        drawCommands.drawCommands = Malloc<BatchDrawCommand>(1);
        drawCommands.drawCommands[0] = new BatchDrawCommand
        {
            visibleOffset = 0,
            visibleCount = (uint)m_itemCount,
            batchID = m_batchID,
            bufferID = m_GPUPersistanceBufferId,
            materialID = m_materialID,
            packedMeshSubmesh = new BatchPackedMeshSubmesh(m_meshID, 0),
            flags = BatchDrawCommandFlags.None,
            sortingPosition = 0
        };

        drawCommands.visibleInstanceCount = m_itemCount;
        drawCommands.visibleInstances = Malloc<int>(m_itemCount);
        for (int i = 0; i < m_itemCount; i++)
        {
            drawCommands.visibleInstances[i] = i;
        }

        drawCommands.instanceSortingPositions = null;
        drawCommands.instanceSortingPositionFloatCount = 0;

        cullingOutput.drawCommands[0] = drawCommands;
        return new JobHandle();
    }


    // Start is called before the first frame update
    void Start()
    {
        m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        int itemGridSize = 30;
        int itemCount = itemGridSize * itemGridSize;
        m_itemCount = itemCount;

        // Bounds
        UnityEngine.Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        m_BatchRendererGroup.SetGlobalBounds(bounds);

        // Register mesh and material
        if (m_mesh) m_meshID = m_BatchRendererGroup.RegisterMesh(m_mesh);
        if (m_material) m_materialID = m_BatchRendererGroup.RegisterMaterial(m_material);

        // Batch metadata buffer
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");

        var batchMetadata = new NativeArray<MetadataValue>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        batchMetadata[0] = CreateMetadataValue(objectToWorldID, 0, true);
        batchMetadata[1] = CreateMetadataValue(colorID, itemCount * UnsafeUtility.SizeOf<Vector4>() * 3, true);

        // Register batch
        m_batchID = m_BatchRendererGroup.AddBatch(batchMetadata);

        // Generate a grid of objects...
        int bigDataBufferVector4Count = itemCount * 3 + itemCount;      // mat4x3, colors
        var vectorBuffer = new NativeArray<Vector4>(bigDataBufferVector4Count, Allocator.Temp, NativeArrayOptions.ClearMemory);

        m_GPUPersistentInstanceData = new ComputeBuffer((int)bigDataBufferVector4Count * 16 / 4, 4, ComputeBufferType.Raw);

        m_GPUPersistanceBufferId = m_BatchRendererGroup.RegisterBuffer(m_GPUPersistentInstanceData);

        // Matrices
        /*
         *  mat4x3 packed like this:
         *
                float4x4(
                        p1.x, p1.w, p2.z, p3.y,
                        p1.y, p2.x, p2.w, p3.z,
                        p1.z, p2.y, p3.x, p3.w,
                        0.0, 0.0, 0.0, 1.0
                    );
        */
        for (int z = 0; z < itemGridSize; z++)
        {
            for (int x = 0; x < itemGridSize; x++)
            {
                float px = x - itemGridSize / 2;
                float pz = z - itemGridSize / 2;
                int i = z * itemGridSize + x;
                vectorBuffer[i * 3 + 0] = new Vector4(1, 0, 0, 0);      // hacky float3x4 layout
                vectorBuffer[i * 3 + 1] = new Vector4(1, 0, 0, 0);
                vectorBuffer[i * 3 + 2] = new Vector4(1, px, m_y, pz);
            }
        }

        // Colors
        int colorOffset = itemCount * 3;
        for (int i = 0; i < itemCount; i++)
        {
            Color col = Color.HSVToRGB(((float)(i) / (float)itemCount) % 1.0f, 1.0f, 1.0f);

            // write colors right after the 4x3 matrices
            vectorBuffer[colorOffset + i] = new Vector4(col.r, col.g, col.b, 1.0f);
        }

        m_GPUPersistentInstanceData.SetData(vectorBuffer);

        m_initialized = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDestroy()
    {
        if (m_initialized)
        {
            m_BatchRendererGroup.RemoveBatch(m_batchID);
            if (m_material) m_BatchRendererGroup.UnregisterMaterial(m_materialID);
            if (m_mesh) m_BatchRendererGroup.UnregisterMesh(m_meshID);

            m_BatchRendererGroup.UnregisterBuffer(m_GPUPersistanceBufferId);

            m_BatchRendererGroup.Dispose();
            m_GPUPersistentInstanceData.Dispose();
        }
    }
}
