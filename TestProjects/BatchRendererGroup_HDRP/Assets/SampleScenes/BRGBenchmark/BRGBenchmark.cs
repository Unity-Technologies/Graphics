using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe class BRGBenchmark : MonoBehaviour
{
    public Mesh m_mesh;
    public Mesh m_mesh2;
    public Material m_material;
    public Material m_material2;
    public Vector3 m_center = new Vector3(0, 0, 0);
    public float m_spacingFactor = 1.0f;

    public int itemGridSize = 30;

    public int m_itemsPerDraw = 10;
    public bool m_changeMaterial = true;
    public bool m_changeMesh = true;

    private BatchRendererGroup m_BatchRendererGroup;
    private GraphicsBuffer m_GPUPersistentInstanceData;

    private BatchID m_batchID;
    private BatchMaterialID m_materialID;
    private BatchMaterialID m_material2ID;
    private BatchMeshID m_meshID;
    private BatchMeshID m_mesh2ID;
    private int m_itemCount;
    private bool m_initialized;
    private float m_phase;

    private NativeArray<Vector4> m_sysmemBuffer;

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

    static int DivRoundUp(int a, int b)
    {
        return (a + b - 1) / b;
    }

    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        if (!m_initialized)
        {
            return new JobHandle();
        }

        BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

        drawCommands.visibleInstances = Malloc<int>(m_itemCount);
        for (int i = 0; i < m_itemCount; i++)
        {
            drawCommands.visibleInstances[i] = i;
        }
        drawCommands.visibleInstanceCount = m_itemCount;

        var numDraws = DivRoundUp(m_itemCount, m_itemsPerDraw);

        drawCommands.drawCommandCount = numDraws;
        drawCommands.drawCommands = Malloc<BatchDrawCommand>(numDraws);
        int itemCounter = 0;
        for (int i = 0; i < numDraws; i++)
        {
            int itemCountClamped = Math.Min(m_itemsPerDraw, m_itemCount - itemCounter);
            bool even = (i & 1) == 0;
            var materialID = m_changeMaterial ? (even ? m_materialID : m_material2ID ) : m_materialID;
            var meshID = m_changeMesh ? (even ? m_meshID : m_mesh2ID) : m_meshID;
            drawCommands.drawCommands[i] = new BatchDrawCommand
            {
                visibleOffset = (uint)itemCounter,
                visibleCount = (uint)itemCountClamped,
                batchID = m_batchID,
                materialID = materialID,
                meshID = meshID,
                submeshIndex = 0,
                splitVisibilityMask = 0xff,
                flags = BatchDrawCommandFlags.None,
                sortingPosition = 0
            };
            itemCounter += m_itemsPerDraw;
        }

        drawCommands.drawRangeCount = 1;
        drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
        drawCommands.drawRanges[0] = new BatchDrawRange
        {
            drawCommandsBegin = 0,
            drawCommandsCount = (uint)numDraws,
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

        drawCommands.instanceSortingPositions = null;
        drawCommands.instanceSortingPositionFloatCount = 0;

        cullingOutput.drawCommands[0] = drawCommands;
        return new JobHandle();
    }


    // Start is called before the first frame update
    void Start()
    {
        m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        int itemCount = itemGridSize * itemGridSize;
        m_itemCount = itemCount;

        // Bounds
        UnityEngine.Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        m_BatchRendererGroup.SetGlobalBounds(bounds);

        // Register mesh and material
        if (m_mesh) m_meshID = m_BatchRendererGroup.RegisterMesh(m_mesh);
        if (m_mesh2) m_mesh2ID = m_BatchRendererGroup.RegisterMesh(m_mesh2);
        if (m_material) m_materialID = m_BatchRendererGroup.RegisterMaterial(m_material);
        if (m_material2) m_material2ID = m_BatchRendererGroup.RegisterMaterial(m_material2);

        // Batch metadata buffer
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int matrixPreviousMID = Shader.PropertyToID("unity_MatrixPreviousM");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");

        // Generate a grid of objects...
        int bigDataBufferVector4Count = 4 + itemCount * (3 * 3 + 1);      // 4xfloat4 zero + per instance = { 3x mat4x3, 1x float4 color }
        m_sysmemBuffer = new NativeArray<Vector4>(bigDataBufferVector4Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)bigDataBufferVector4Count * 16 / 4, 4);

        // 64 bytes of zeroes, so loads from address 0 return zeroes. This is a BatchRendererGroup convention.
        int positionOffset = 4;
        m_sysmemBuffer[0] = new Vector4(0, 0, 0, 0);
        m_sysmemBuffer[1] = new Vector4(0, 0, 0, 0);
        m_sysmemBuffer[2] = new Vector4(0, 0, 0, 0);
        m_sysmemBuffer[3] = new Vector4(0, 0, 0, 0);

        // Matrices
        UpdatePositions(m_center);

        // Colors
        int colorOffset = positionOffset + itemCount * 3 * 3;
        for (int i = 0; i < itemCount; i++)
        {
            Color col = Color.HSVToRGB(((float)(i) / (float)itemCount) % 1.0f, 1.0f, 1.0f);

            // write colors right after the 4x3 matrices
            m_sysmemBuffer[colorOffset + i] = new Vector4(col.r, col.g, col.b, 1.0f);
        }
        m_GPUPersistentInstanceData.SetData(m_sysmemBuffer);

        var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        batchMetadata[0] = CreateMetadataValue(objectToWorldID, 64, true);       // matrices
        batchMetadata[1] = CreateMetadataValue(matrixPreviousMID, 64 + itemCount * UnsafeUtility.SizeOf<Vector4>() * 3, true); // previous matrices
        batchMetadata[2] = CreateMetadataValue(worldToObjectID, 64 + itemCount * UnsafeUtility.SizeOf<Vector4>() * 3 * 2, true); // inverse matrices
        batchMetadata[3] = CreateMetadataValue(colorID, 64 + itemCount * UnsafeUtility.SizeOf<Vector4>() * 3 * 3, true); // colors

        // Register batch
        m_batchID = m_BatchRendererGroup.AddBatch(batchMetadata, m_GPUPersistentInstanceData.bufferHandle);

        m_initialized = true;
    }

    void UpdatePositions(Vector3 pos)
    {
        int positionOffset = 4;
        int itemCountOffset = itemGridSize * itemGridSize * 3;      // 3xfloat4 per matrix
        for (int z = 0; z < itemGridSize; z++)
        {
            for (int x = 0; x < itemGridSize; x++)
            {
                float px = (x - itemGridSize / 2) * m_spacingFactor;
                float pz = (z - itemGridSize / 2) * m_spacingFactor;
                int i = z * itemGridSize + x;

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

                // update previous matrix with previous frame current matrix
                m_sysmemBuffer[positionOffset + i * 3 + 0 + itemCountOffset] = m_sysmemBuffer[positionOffset + i * 3 + 0];
                m_sysmemBuffer[positionOffset + i * 3 + 1 + itemCountOffset] = m_sysmemBuffer[positionOffset + i * 3 + 1];
                m_sysmemBuffer[positionOffset + i * 3 + 2 + itemCountOffset] = m_sysmemBuffer[positionOffset + i * 3 + 2];

                // compute the new current frame matrix
                m_sysmemBuffer[positionOffset + i * 3 + 0] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[positionOffset + i * 3 + 1] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[positionOffset + i * 3 + 2] = new Vector4(1, px + pos.x, pos.y, pz + pos.z);

                // compute the new inverse matrix
                m_sysmemBuffer[positionOffset + i * 3 + 0 + itemCountOffset * 2] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[positionOffset + i * 3 + 1 + itemCountOffset * 2] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[positionOffset + i * 3 + 2 + itemCountOffset * 2] = new Vector4(1, -(px + pos.x), -pos.y, -(pz + pos.z));
            }
        }

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
            if (m_material2) m_BatchRendererGroup.UnregisterMaterial(m_material2ID);
            if (m_mesh) m_BatchRendererGroup.UnregisterMesh(m_meshID);
            if (m_mesh2) m_BatchRendererGroup.UnregisterMesh(m_mesh2ID);

            m_BatchRendererGroup.Dispose();
            m_GPUPersistentInstanceData.Dispose();
            m_sysmemBuffer.Dispose();
        }
    }
}
