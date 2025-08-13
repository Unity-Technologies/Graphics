using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using PackedMatrix = BRGUtils.PackedMatrix;

public class SortingPositionsSplit : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public ComputeShader memcpy;

    private BatchRendererGroup m_brg;
    private GraphicsBuffer m_instanceData;
    private GraphicsBuffer m_copySrc;
    private BatchID m_batchID;
    private BatchMeshID m_meshID;
    private BatchMaterialID m_materialID;

    private const uint kBytesPerInstance = BRGUtils.kSizeOfPackedMatrix * 2 + BRGUtils.kSizeOfFloat4;
    private const uint kExtraBytes = BRGUtils.kSizeOfMatrix * 2; // ?????
    private const uint kNumInstancesX = 10;
    private const uint kNumInstancesY = 10;
    private const uint kNumInstancesZ = 10;
    private const uint kNumInstances = kNumInstancesX * kNumInstancesY * kNumInstancesZ;

    private void OnEnable()
    {
        m_brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
        m_meshID = m_brg.RegisterMesh(mesh);
        m_materialID = m_brg.RegisterMaterial(material);

        var target = BRGUtils.GetPreferredBufferTarget();

        var bufferCount = BRGUtils.BufferCountForInstances(kBytesPerInstance, kNumInstances, kExtraBytes);

        m_copySrc = new GraphicsBuffer(target, (int)bufferCount, sizeof(int));
        m_instanceData = new GraphicsBuffer(target, (int)bufferCount, sizeof(int));

        var zero = new Matrix4x4[1] { Matrix4x4.zero };
        var objectToWorld = new PackedMatrix[kNumInstances];
        var worldToObject = new PackedMatrix[kNumInstances];
        var colors = new Vector4[kNumInstances];

        for (int x = 0; x < kNumInstancesX; ++x)
        {
            for (int y = 0; y < kNumInstancesY; ++y)
            {
                for (int z = 0; z < kNumInstancesZ; ++z)
                {
                    var index = x + y * kNumInstancesX + z * kNumInstancesX * kNumInstancesY;
                    var position = new Vector3(x*1.5f, y*2, z *1.5f);
                    var matrix = Matrix4x4.Translate(position);
                    objectToWorld[index] = new PackedMatrix(matrix);
                    worldToObject[index] = new PackedMatrix(matrix.inverse);
                    colors[index] = new Vector4(x / (float)kNumInstancesX, y / (float)kNumInstancesY, z / (float)kNumInstancesZ, 0.9f);
                }
            }
        }

        uint byteAddressObjectToWorld = BRGUtils.kSizeOfPackedMatrix * 2;
        uint byteAddressWorldToObject = byteAddressObjectToWorld + BRGUtils.kSizeOfPackedMatrix * kNumInstances;
        uint byteAddressColor = byteAddressWorldToObject + BRGUtils.kSizeOfPackedMatrix * kNumInstances;

        m_copySrc.SetData(zero, 0, 0, 1);
        m_copySrc.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / BRGUtils.kSizeOfPackedMatrix), (int)kNumInstances);
        m_copySrc.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / BRGUtils.kSizeOfPackedMatrix), (int)kNumInstances);
        m_copySrc.SetData(colors, 0, (int)(byteAddressColor / BRGUtils.kSizeOfFloat4), (int)kNumInstances);

        int dstSize = m_copySrc.count * m_copySrc.stride;
        memcpy.SetBuffer(0, "src", m_copySrc);
        memcpy.SetBuffer(0, "dest", m_instanceData);
        memcpy.SetInt("dstOffset", 0);
        memcpy.SetInt("dstSize", dstSize);
        memcpy.Dispatch(0,dstSize/ (64*4) +1, 1, 1);

        var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
        metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
        metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };
        metadata[2] = new MetadataValue { NameID = Shader.PropertyToID("_BaseColor"), Value = 0x80000000 | byteAddressColor, };

        m_batchID = m_brg.AddBatch(metadata, m_instanceData.bufferHandle, 0, BRGUtils.BufferWindowSize);
    }

    private void OnDisable()
    {
        m_brg.Dispose();
        m_instanceData.Dispose();
        m_copySrc.Dispose();
    }

    public unsafe JobHandle OnPerformCulling(BatchRendererGroup brg, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        int alignment = UnsafeUtility.AlignOf<long>();

        var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

        drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>(),
            alignment, Allocator.TempJob);
        drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(),
            alignment, Allocator.TempJob);
        drawCommands->visibleInstances =
            (int*)UnsafeUtility.Malloc(kNumInstances * sizeof(int), alignment, Allocator.TempJob);
        drawCommands->drawCommandPickingEntityIds = null;

        drawCommands->drawCommandCount = 1;
        drawCommands->drawRangeCount = 1;
        drawCommands->visibleInstanceCount = (int)kNumInstances;

        drawCommands->instanceSortingPositions = (float*)UnsafeUtility.Malloc(kNumInstances * sizeof(float)*3, alignment, Allocator.TempJob);
        drawCommands->instanceSortingPositionFloatCount = (int)kNumInstances * 3;
        for (var x = 0; x < kNumInstancesX; ++x)
        {
            for (var y = 0; y < kNumInstancesY; ++y)
            {
                for (var z = 0; z < kNumInstancesZ; ++z)
                {
                    var index = x + y * kNumInstancesX + z * kNumInstancesX * kNumInstancesY;
                    var position = new Vector3(x * 1.5f, y * 2, z * 1.5f);
                    drawCommands->instanceSortingPositions[index * 3 + 0] = position.x;
                    drawCommands->instanceSortingPositions[index * 3 + 1] = position.y;
                    drawCommands->instanceSortingPositions[index * 3 + 2] = position.z;
                }
            }
        }

        drawCommands->drawCommands[0].visibleOffset = 0;
        drawCommands->drawCommands[0].visibleCount = kNumInstances;
        drawCommands->drawCommands[0].batchID = m_batchID;
        drawCommands->drawCommands[0].materialID = m_materialID;
        drawCommands->drawCommands[0].meshID = m_meshID;
        drawCommands->drawCommands[0].submeshIndex = 0;
        drawCommands->drawCommands[0].splitVisibilityMask = 0xff;
        drawCommands->drawCommands[0].flags = BatchDrawCommandFlags.HasSortingPosition;
        drawCommands->drawCommands[0].sortingPosition = 0;

        drawCommands->drawRanges[0].drawCommandsBegin = 0;
        drawCommands->drawRanges[0].drawCommandsCount = 1;
        drawCommands->drawRanges[0].drawCommandsType = BatchDrawCommandType.Direct;

        drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };

        for (int i = 0; i < kNumInstances; ++i)
            drawCommands->visibleInstances[i] = i;

        return new JobHandle();
    }
}
