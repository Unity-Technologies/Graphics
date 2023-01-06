using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe class BRGTransparent : MonoBehaviour
{
    public Mesh m_mesh;
    public Material m_material;
    public Vector3 m_center = new Vector3(0, 0, 0);
    public float m_spacingFactor = 1.0f;
    public bool m_unlitHdrp = false;
    public bool m_culling = false;

    private const int kTransparentCount = 16;
    private BatchRendererGroup m_BatchRendererGroup;
    private GraphicsBuffer m_GPUPersistentInstanceData;

    private BatchID[] m_batchIDs;
    private BatchMaterialID m_materialID;
    private BatchMeshID m_meshID;
    private bool m_initialized;

    private NativeArray<Vector4> m_sysmemBuffer;

    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;

    public struct SRPBatch
    {
        public uint rawBufferOffsetInFloat4;
        public uint itemCount;
    };

    private SRPBatch[] m_srpBatches;
    private uint m_batchCount;
    private uint m_maxItemPerBatch;


    public static T* Malloc<T>(uint count) where T : unmanaged
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

        drawCommands.drawRangeCount = 1;
        drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
        drawCommands.drawRanges[0] = new BatchDrawRange
        {
            drawCommandsBegin = 0,
            drawCommandsCount = kTransparentCount,
            filterSettings = new BatchFilterSettings
            {
                renderingLayerMask = 1,
                layer = 0,
                motionMode = MotionVectorGenerationMode.Camera,
                shadowCastingMode = ShadowCastingMode.On,
                receiveShadows = true,
                staticShadowCaster = false,
                allDepthSorted = true
            }
        };

        drawCommands.visibleInstances = Malloc<int>(kTransparentCount);

        drawCommands.drawCommandCount = (Int32)kTransparentCount;
        drawCommands.drawCommands = Malloc<BatchDrawCommand>(kTransparentCount);

        drawCommands.instanceSortingPositions = Malloc<float>(kTransparentCount*3);
        drawCommands.instanceSortingPositionFloatCount = kTransparentCount * 3;

        for (int i=0;i<kTransparentCount;i++)
        {
            drawCommands.instanceSortingPositions[i * 3 + 0] = 0.0f;    // x
            drawCommands.instanceSortingPositions[i * 3 + 1] = m_center.y - i*m_spacingFactor;    // y
            drawCommands.instanceSortingPositions[i * 3 + 2] = 0.0f;    // z
        }

        for (int b = 0; b < m_batchCount; b++)
        {
            for (uint i = 0; i < m_srpBatches[b].itemCount; i++)
            {
                int itemId = (int)(b * m_maxItemPerBatch + i);

                drawCommands.visibleInstances[itemId] = (Int32)itemId;

                drawCommands.drawCommands[itemId] = new BatchDrawCommand
                {
                    visibleOffset = (uint)itemId,
                    visibleCount = 1,
                    batchID = m_batchIDs[b],
                    materialID = m_materialID,
                    meshID = m_meshID,
                    submeshIndex = 0,
                    splitVisibilityMask = 0xff,
                    flags = BatchDrawCommandFlags.HasSortingPosition,
                    sortingPosition = itemId * 3
                };
            }
        }

        // shuffle drawCommands (to be sure BRG zsort will properly sort)
        for (int i=0;i<kTransparentCount;i++)
        {
            int rndId = UnityEngine.Random.Range(0, kTransparentCount);
            BatchDrawCommand old = drawCommands.drawCommands[i];
            drawCommands.drawCommands[i] = drawCommands.drawCommands[rndId];
            drawCommands.drawCommands[rndId] = old;
        }

        cullingOutput.drawCommands[0] = drawCommands;
        return new JobHandle();
    }


    // Start is called before the first frame update
    void Start()
    {
        m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        const int kFloat4Size = 16;

        uint kBRGBufferMaxWindowSize = 128 * 1024 * 1024;
        uint kBRGBufferAlignment = 16;
        if (UseConstantBuffer)
        {
            kBRGBufferMaxWindowSize = (uint)(BatchRendererGroup.GetConstantBufferMaxWindowSize());
            kBRGBufferAlignment = (uint)(BatchRendererGroup.GetConstantBufferOffsetAlignment());
        }

        // create one or several batches (regarding UBO size limit on UBO only platform such as GLES3.1)
        uint itemCount = (uint)kTransparentCount;

        const uint kItemSize = (3 * 3 + 1);  //  size in "float4" ( 3 * 4x3 matrices plus 1 color per item )
        m_maxItemPerBatch = ((kBRGBufferMaxWindowSize / kFloat4Size) - 4) / kItemSize;  // -4 "float4" for 64 first 0 bytes ( BRG contrainst )
        if (m_maxItemPerBatch > itemCount)
            m_maxItemPerBatch = itemCount;

        m_batchCount = (itemCount + m_maxItemPerBatch - 1) / m_maxItemPerBatch;

        uint batchAlignedSizeInBytes = (((4 + m_maxItemPerBatch * kItemSize)* kFloat4Size) + kBRGBufferAlignment - 1) & (~(kBRGBufferAlignment - 1));
        uint totalRawBufferSizeInBytes = m_batchCount * batchAlignedSizeInBytes;

        // compute offsets of each item ( according to several batches & alignment per batch )
        // also clear the first 64bytes of each batch
        var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // Create the large GPU raw buffer
        if (UseConstantBuffer)
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, (int)totalRawBufferSizeInBytes / kFloat4Size, kFloat4Size);
        else
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)totalRawBufferSizeInBytes / 4, 4);

        // Batch metadata buffer
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int matrixPreviousMID = Shader.PropertyToID("unity_MatrixPreviousM");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");
        if ( m_unlitHdrp )
            colorID = Shader.PropertyToID("_UnlitColor");

        // Create sysmem copy of big GUP raw buffer
        m_sysmemBuffer = new NativeArray<Vector4>((int)(totalRawBufferSizeInBytes/16), Allocator.Persistent, NativeArrayOptions.ClearMemory);
        m_srpBatches = new SRPBatch[m_batchCount];
        m_batchIDs = new BatchID[m_batchCount];
        uint left = itemCount;
        for (uint b=0;b< m_batchCount;b++)
        {
            uint offset = (b * batchAlignedSizeInBytes) / kFloat4Size;
            m_srpBatches[b].itemCount = left > m_maxItemPerBatch ? m_maxItemPerBatch : left;
            m_srpBatches[b].rawBufferOffsetInFloat4 = offset;
            m_sysmemBuffer[(int)offset+0] = new Vector4(0, 0, 0, 0);
            m_sysmemBuffer[(int)offset+1] = new Vector4(0, 0, 0, 0);
            m_sysmemBuffer[(int)offset+2] = new Vector4(0, 0, 0, 0);
            m_sysmemBuffer[(int)offset+3] = new Vector4(0, 0, 0, 0);

            batchMetadata[0] = CreateMetadataValue(objectToWorldID, 64, true);       // matrices
            batchMetadata[1] = CreateMetadataValue(matrixPreviousMID, 64 + (int)m_srpBatches[b].itemCount * kFloat4Size * 3, true); // previous matrices
            batchMetadata[2] = CreateMetadataValue(worldToObjectID, 64 + (int)m_srpBatches[b].itemCount * kFloat4Size * 3 * 2, true); // inverse matrices
            batchMetadata[3] = CreateMetadataValue(colorID, 64 + (int)m_srpBatches[b].itemCount * kFloat4Size * 3 * 3, true); // colors

            uint batchWindowSize = 0;
            if (UseConstantBuffer)
                batchWindowSize = (m_srpBatches[b].itemCount * kItemSize + 4) * kFloat4Size;   // +4 float4 because of the first 64bytes at 0

            m_batchIDs[b] = m_BatchRendererGroup.AddBatch(batchMetadata, m_GPUPersistentInstanceData.bufferHandle, m_srpBatches[b].rawBufferOffsetInFloat4 * kFloat4Size, batchWindowSize);

            left -= m_srpBatches[b].itemCount;
        }

        // Bounds
        UnityEngine.Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        m_BatchRendererGroup.SetGlobalBounds(bounds);

        // Register mesh and material
        if (m_mesh) m_meshID = m_BatchRendererGroup.RegisterMesh(m_mesh);
        if (m_material) m_materialID = m_BatchRendererGroup.RegisterMaterial(m_material);

        // Matrices
        UpdatePositions(m_center);

        // Colors
        int id = 0;
        for (uint b = 0; b < m_batchCount; b++)
        {
            uint batchOffset = m_srpBatches[b].rawBufferOffsetInFloat4 + 4 + m_srpBatches[b].itemCount * 3 * 3;
            for (uint i = 0; i < m_srpBatches[b].itemCount; i++)
            {
                Color col = Color.HSVToRGB(((float)(id) / (float)itemCount) % 1.0f, 1.0f, 1.0f);

                // write colors right after the 4x3 matrices
                m_sysmemBuffer[(int)(batchOffset + i)] = new Vector4(col.r, col.g, col.b, 0.5f);
                id++;
            }
        }

        m_GPUPersistentInstanceData.SetData(m_sysmemBuffer);
        m_initialized = true;
    }

    private void OnUpdate()
    {
        UpdatePositions(m_center);
    }

    void UpdatePositions(Vector3 pos)
    {

        for (uint b = 0; b < m_batchCount; b++)
        {
            uint strideInFloat4 = m_srpBatches[b].itemCount * 3;
            uint batchOffset = m_srpBatches[b].rawBufferOffsetInFloat4 + 4;
            for (uint i = 0; i < m_srpBatches[b].itemCount; i++)
            {
                int itemId = (int)(b * m_maxItemPerBatch + i);
                float px = 0.0f;
                float py = -itemId * m_spacingFactor;
                float pz = 0.0f;

                // copy old current matrix in previous matrix
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4*1 + i*3 + 0)] = m_sysmemBuffer[(int)(batchOffset +   strideInFloat4 * 0 + i * 3 + 0)];
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4*1 + i*3 + 1)] = m_sysmemBuffer[(int)(batchOffset +   strideInFloat4 * 0 + i * 3 + 1)];
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 1 + i*3 + 2)] = m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 0 + i * 3 + 2)];

                // compute the new current frame matrix
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 0 + i * 3 + 0)] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 0 + i * 3 + 1)] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 0 + i * 3 + 2)] = new Vector4(1, px + pos.x, pos.y+py, pz + pos.z);

                // compute the new inverse matrix
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 2 + i * 3 + 0)] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 2 + i * 3 + 1)] = new Vector4(1, 0, 0, 0);
                m_sysmemBuffer[(int)(batchOffset + strideInFloat4 * 2 + i * 3 + 2)] = new Vector4(1, -(px + pos.x), -(pos.y+py), -(pz + pos.z));

            }
        }

    }

    private void OnDestroy()
    {
        if (m_initialized)
        {
            for (uint b=0;b<m_batchCount;b++)
            {
                m_BatchRendererGroup.RemoveBatch(m_batchIDs[b]);
            }
            if (m_material) m_BatchRendererGroup.UnregisterMaterial(m_materialID);
            if (m_mesh) m_BatchRendererGroup.UnregisterMesh(m_meshID);

            m_BatchRendererGroup.Dispose();
            m_GPUPersistentInstanceData.Dispose();
            m_sysmemBuffer.Dispose();
        }
    }
}
