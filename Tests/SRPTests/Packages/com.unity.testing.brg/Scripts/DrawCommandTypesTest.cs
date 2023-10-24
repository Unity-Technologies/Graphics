using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe class DrawCommandTypesTest: MonoBehaviour
{
    public Mesh mesh;
    public Material regularMaterial;
    public Material proceduralMaterial;
    public Material proceduralIndexedMaterial;

    private BatchRendererGroup _batchRendererGroup;
    private GraphicsBuffer _gpuPersistentInstanceData;
    private GraphicsBuffer _gpuVisibleInstances;
    private GraphicsBuffer _gpuIndirectBuffer;
    private GraphicsBuffer _gpuIndexedIndirectBuffer;
    private uint _gpuVisibleInstancesWindow;

    private GraphicsBuffer[] _ib;
    private GraphicsBuffer _gpuPositions;
    private GraphicsBuffer _gpuNormals;
    private GraphicsBuffer _gpuTangents;
    private uint _elementsPerDraw;

    private BatchID _batchID;
    private BatchMaterialID _regularMaterialID;
    private BatchMaterialID _proceduralMaterialID;
    private BatchMaterialID _proceduralIndexedMaterialID;
    private BatchMeshID _meshID;
    private int _itemCount;
    private bool _initialized;

    private NativeArray<Vector4> _sysmemBuffer;
    private NativeArray<int> _sysmemVisibleInstances;

    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;

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
        if (!_initialized)
        {
            return new JobHandle();
        }

        BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

        var filterSettings = new BatchFilterSettings
        {
            renderingLayerMask = 1,
            layer = 0,
            motionMode = MotionVectorGenerationMode.ForceNoMotion,
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true,
            staticShadowCaster = false,
            allDepthSorted = false
        };
        drawCommands.drawRangeCount = 4;
        drawCommands.drawRanges = Malloc<BatchDrawRange>(4);

        // Regular draw range
        drawCommands.drawRanges[0] = new BatchDrawRange
        {
            drawCommandsType = BatchDrawCommandType.Direct,
            drawCommandsBegin = 0,
            drawCommandsCount = 5,
            filterSettings = filterSettings,
        };

        // Indirect draw range
        drawCommands.drawRanges[1] = new BatchDrawRange
        {
            drawCommandsType = BatchDrawCommandType.Indirect,
            drawCommandsBegin = 0,
            drawCommandsCount = 5,
            filterSettings = filterSettings,
        };

        // Procedural draw range
        drawCommands.drawRanges[2] = new BatchDrawRange
        {
            drawCommandsType = BatchDrawCommandType.Procedural,
            drawCommandsBegin = 0,
            drawCommandsCount = 10,
            filterSettings = filterSettings,
        };

        // ProceduralIndirect draw range
        drawCommands.drawRanges[3] = new BatchDrawRange
        {
            drawCommandsType = BatchDrawCommandType.ProceduralIndirect,
            drawCommandsBegin = 0,
            drawCommandsCount = 10,
            filterSettings = filterSettings,
        };

        drawCommands.visibleInstances = Malloc<int>(_itemCount);
        for (int i = 0; i < _itemCount; ++i)
        {
            drawCommands.visibleInstances[i] = _sysmemVisibleInstances[i];
        }

        drawCommands.visibleInstanceCount = _itemCount;

        // Regular draw command
        drawCommands.drawCommandCount = 5;
        drawCommands.drawCommands = Malloc<BatchDrawCommand>(5);
        for (uint i = 0; i < 5; ++i)
        {
            drawCommands.drawCommands[i] = new BatchDrawCommand
            {
                flags = BatchDrawCommandFlags.None,
                batchID = _batchID,
                materialID = _regularMaterialID,
                splitVisibilityMask = 0xff,
                sortingPosition = 0,
                visibleOffset = i * 2,
                visibleCount = 2,
                meshID = _meshID,
                submeshIndex = 0,
            };
        }

        // Indirect draw command
        drawCommands.indirectDrawCommandCount = 5;
        drawCommands.indirectDrawCommands = Malloc<BatchDrawCommandIndirect>(5);
        for (uint i = 0; i < 5; ++i)
        {
            drawCommands.indirectDrawCommands[i] = new BatchDrawCommandIndirect
            {
                flags = BatchDrawCommandFlags.None,
                batchID = _batchID,
                materialID = _regularMaterialID,
                splitVisibilityMask = 0xff,
                sortingPosition = 0,
                visibleOffset = 10 + i * 2,
                meshID = _meshID,
                topology = MeshTopology.Triangles,
                visibleInstancesBufferHandle = _gpuVisibleInstances.bufferHandle,
                visibleInstancesBufferWindowOffset = 0,
                visibleInstancesBufferWindowSizeBytes = _gpuVisibleInstancesWindow,
                indirectArgsBufferHandle = _gpuIndexedIndirectBuffer.bufferHandle,
                indirectArgsBufferOffset = i * 20,
            };
        }

        // Procedural draw command
        drawCommands.proceduralDrawCommandCount = 10;
        drawCommands.proceduralDrawCommands = Malloc<BatchDrawCommandProcedural>(10);
        for (uint i = 0; i < 5; ++i)
        {
            drawCommands.proceduralDrawCommands[i] = new BatchDrawCommandProcedural
            {
                flags = BatchDrawCommandFlags.None,
                batchID = _batchID,
                materialID = _proceduralIndexedMaterialID,
                splitVisibilityMask = 0xff,
                sortingPosition = 0,
                visibleOffset = 20 + i * 2,
                visibleCount = 2,
                topology = MeshTopology.Triangles,
                indexBufferHandle = _ib[i].bufferHandle,
                baseVertex = 0,
                indexOffsetBytes = 0,
                elementCount = (uint)_ib[i].count,
            };
        }

        for (uint i = 0; i < 5; ++i)
        {
            drawCommands.proceduralDrawCommands[5 + i] = new BatchDrawCommandProcedural
            {
                flags = BatchDrawCommandFlags.None,
                batchID = _batchID,
                materialID = _proceduralMaterialID,
                splitVisibilityMask = 0xff,
                sortingPosition = 0,
                visibleOffset = 30 + i * 2,
                visibleCount = 2,
                topology = MeshTopology.Triangles,
                indexBufferHandle = default,
                baseVertex = 0,
                indexOffsetBytes = 0,
                elementCount = _elementsPerDraw,
            };
        }

        // ProceduralIndirect draw command
        drawCommands.proceduralIndirectDrawCommandCount = 10;
        drawCommands.proceduralIndirectDrawCommands = Malloc<BatchDrawCommandProceduralIndirect>(10);
        for (uint i = 0; i < 5; ++i)
        {
            drawCommands.proceduralIndirectDrawCommands[i] = new BatchDrawCommandProceduralIndirect
            {
                flags = BatchDrawCommandFlags.None,
                batchID = _batchID,
                materialID = _proceduralIndexedMaterialID,
                splitVisibilityMask = 0xff,
                sortingPosition = 0,
                visibleOffset = 40 + i * 2,
                topology = MeshTopology.Triangles,
                indexBufferHandle = _ib[i].bufferHandle,
                visibleInstancesBufferHandle = _gpuVisibleInstances.bufferHandle,
                visibleInstancesBufferWindowOffset = 0,
                visibleInstancesBufferWindowSizeBytes = _gpuVisibleInstancesWindow,
                indirectArgsBufferHandle = _gpuIndexedIndirectBuffer.bufferHandle,
                indirectArgsBufferOffset = 100 + i * 20,
            };
        }
        for (uint i = 0; i < 5; ++i)
        {
            drawCommands.proceduralIndirectDrawCommands[5 + i] = new BatchDrawCommandProceduralIndirect
            {
                flags = BatchDrawCommandFlags.None,
                batchID = _batchID,
                materialID = _proceduralMaterialID,
                splitVisibilityMask = 0xff,
                sortingPosition = 0,
                visibleOffset = 50 + i * 2,
                topology = MeshTopology.Triangles,
                indexBufferHandle = default,
                visibleInstancesBufferHandle = _gpuVisibleInstances.bufferHandle,
                visibleInstancesBufferWindowOffset = 0,
                visibleInstancesBufferWindowSizeBytes = _gpuVisibleInstancesWindow,
                indirectArgsBufferHandle = _gpuIndirectBuffer.bufferHandle,
                indirectArgsBufferOffset = i * 16,
            };
        }

        drawCommands.instanceSortingPositions = null;
        drawCommands.instanceSortingPositionFloatCount = 0;

        cullingOutput.drawCommands[0] = drawCommands;
        return new JobHandle();
    }


    // Start is called before the first frame update
    void Start()
    {
        uint kBRGBufferMaxWindowSize = 128 * 1024 * 1024;
        uint kBRGBufferAlignment = 16;
        if (UseConstantBuffer)
        {
            kBRGBufferMaxWindowSize = (uint)(BatchRendererGroup.GetConstantBufferMaxWindowSize());
            kBRGBufferAlignment = (uint)(BatchRendererGroup.GetConstantBufferOffsetAlignment());
        }

        _batchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        _itemCount = 6 * 10;

        // Bounds
        Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        _batchRendererGroup.SetGlobalBounds(bounds);

        // Register mesh and material
        if (mesh) _meshID = _batchRendererGroup.RegisterMesh(mesh);
        if (regularMaterial) _regularMaterialID = _batchRendererGroup.RegisterMaterial(regularMaterial);
        if (proceduralMaterial) _proceduralMaterialID = _batchRendererGroup.RegisterMaterial(proceduralMaterial);
        if (proceduralIndexedMaterial) _proceduralIndexedMaterialID = _batchRendererGroup.RegisterMaterial(proceduralIndexedMaterial);

        // Batch metadata buffer
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int matrixPreviousID = Shader.PropertyToID("unity_MatrixPreviousM");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");
        int positionsID = Shader.PropertyToID("_Positions");
        int normalsID = Shader.PropertyToID("_Normals");
        int tangentsID = Shader.PropertyToID("_Tangents");
        int baseIndexID = Shader.PropertyToID("_BaseIndex");

        // Generate a grid of objects...
        int bigDataBufferVector4Count = 4 + _itemCount * (3 * 3 + 1);      // 4xfloat4 zero + per instance = { 3x mat4x3, 1x float4 color }
        uint brgWindowSize = 0;
        _sysmemBuffer = new NativeArray<Vector4>(bigDataBufferVector4Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        if (UseConstantBuffer)
        {
            _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, (int)bigDataBufferVector4Count * 16 / (4 * 4), 4 * 4);
            brgWindowSize = (uint)bigDataBufferVector4Count * 16;
        }
        else
        {
            _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)bigDataBufferVector4Count * 16 / 4, 4);
        }

        // 64 bytes of zeroes, so loads from address 0 return zeroes. This is a BatchRendererGroup convention.
        int positionOffset = 4;
        _sysmemBuffer[0] = new Vector4(0, 0, 0, 0);
        _sysmemBuffer[1] = new Vector4(0, 0, 0, 0);
        _sysmemBuffer[2] = new Vector4(0, 0, 0, 0);
        _sysmemBuffer[3] = new Vector4(0, 0, 0, 0);

        // Matrices
        var itemCountOffset = _itemCount * 3; // one packed matrix
        for (int i = 0; i < _itemCount; ++i)
        {
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

            float px = (i % 10) * 2.0f;
            float py = -(i / 10) * 2.0f;
            float pz = 0.0f;

            // compute the new current frame matrix
            _sysmemBuffer[positionOffset + i * 3 + 0] = new Vector4(1, 0, 0, 0);
            _sysmemBuffer[positionOffset + i * 3 + 1] = new Vector4(1, 0, 0, 0);
            _sysmemBuffer[positionOffset + i * 3 + 2] = new Vector4(1, px, py, pz);

            // we set the same matrix for the previous matrix
            _sysmemBuffer[positionOffset + i * 3 + 0 + itemCountOffset] = _sysmemBuffer[positionOffset + i * 3 + 0];
            _sysmemBuffer[positionOffset + i * 3 + 1 + itemCountOffset] = _sysmemBuffer[positionOffset + i * 3 + 1];
            _sysmemBuffer[positionOffset + i * 3 + 2 + itemCountOffset] = _sysmemBuffer[positionOffset + i * 3 + 2];

            // compute the new inverse matrix
            _sysmemBuffer[positionOffset + i * 3 + 0 + itemCountOffset * 2] = new Vector4(1, 0, 0, 0);
            _sysmemBuffer[positionOffset + i * 3 + 1 + itemCountOffset * 2] = new Vector4(1, 0, 0, 0);
            _sysmemBuffer[positionOffset + i * 3 + 2 + itemCountOffset * 2] = new Vector4(1, -px, -py, -pz);
        }

        // Colors
        int colorOffset = positionOffset + itemCountOffset * 3;
        for (int i = 0; i < _itemCount; i++)
        {
            Color col = Color.HSVToRGB(((float)(i / 10) / (float)(_itemCount / 10)) % 1.0f, 1.0f, 1.0f);

            // write colors right after the 4x3 matrices
            _sysmemBuffer[colorOffset + i] = new Vector4(col.r, col.g, col.b, 1.0f);
        }
        _gpuPersistentInstanceData.SetData(_sysmemBuffer);

        // GPU side visible instances
        _sysmemVisibleInstances = new NativeArray<int>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        if (UseConstantBuffer)
        {
            _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Constant, sizeof(int) * _itemCount / 4, sizeof(int) * 4);
            _gpuVisibleInstancesWindow = (uint)(sizeof(int) * _itemCount);
        }
        else
        {
            _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Raw, sizeof(int) * _itemCount, sizeof(int));
        }

        for (int i = 0; i < _itemCount; ++i)
        {
            _sysmemVisibleInstances[i] = i;
        }
        _gpuVisibleInstances.SetData(_sysmemVisibleInstances);

        // Set up procedural mesh
        var indices = mesh.GetIndices(0);
        _ib = new GraphicsBuffer[5];
        for (uint i = 0; i < 5; ++i)
        {
            _ib[i] = new GraphicsBuffer(GraphicsBuffer.Target.Index, indices.Length, 4);
            _ib[i].SetData(indices);
        }

        var indexedPositions = mesh.vertices;
        var indexedNormals = mesh.normals;
        var indexedTangents = mesh.tangents;

        var indexedPositionsLength = indexedPositions.Length;
        var nonIndexedPositionsLength = indices.Length;
        var length = indexedPositionsLength + nonIndexedPositionsLength;
        var target = UseConstantBuffer ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Structured;

        _gpuPositions = new GraphicsBuffer(target, length, 4 * 4);
        _gpuNormals = new GraphicsBuffer(target, length, 4 * 4);
        _gpuTangents = new GraphicsBuffer(target, length, 4 * 4);

        var positions = new Vector4[length];
        var normals = new Vector4[length];
        var tangents = new Vector4[length];

        for (int i = 0; i < indexedPositionsLength; ++i)
        {
            positions[i] = indexedPositions[i];
            normals[i] = indexedNormals[i];
            tangents[i] = indexedTangents[i];
        }

        for (int i = 0; i < indices.Length; ++i)
        {
            var idx = indices[i];
            positions[i + indexedPositionsLength] = indexedPositions[idx];
            normals[i + indexedPositionsLength] = indexedNormals[idx];
            tangents[i + indexedPositionsLength] = indexedTangents[idx];
        }

        _gpuPositions.SetData(positions);
        _gpuNormals.SetData(normals);
        _gpuTangents.SetData(tangents);

        if (UseConstantBuffer)
        {
            Shader.SetGlobalConstantBuffer(positionsID, _gpuPositions, 0, positions.Length * 4 * 4);
            Shader.SetGlobalConstantBuffer(normalsID, _gpuNormals, 0, positions.Length * 4 * 4);
            Shader.SetGlobalConstantBuffer(tangentsID, _gpuTangents, 0, positions.Length * 4 * 4);
        }
        else
        {
            Shader.SetGlobalBuffer(positionsID, _gpuPositions);
            Shader.SetGlobalBuffer(normalsID, _gpuNormals);
            Shader.SetGlobalBuffer(tangentsID, _gpuTangents);
        }
        proceduralMaterial.SetInt(baseIndexID, indexedPositionsLength);
        proceduralIndexedMaterial.SetInt(baseIndexID, 0);

        _elementsPerDraw = (uint)indices.Length;

        // Indexed Indirect buffer
        _gpuIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 5, GraphicsBuffer.IndirectDrawArgs.size);
        var indirectData = new GraphicsBuffer.IndirectDrawArgs[5];
        for (uint i = 0; i < 5; ++i)
        {
            indirectData[i] = new GraphicsBuffer.IndirectDrawArgs
            {
                vertexCountPerInstance = _elementsPerDraw,
                instanceCount = 2,
                startVertex = 0,
                startInstance = 0,
            };
        }
        _gpuIndirectBuffer.SetData(indirectData);

        // Indexed Indirect buffer
        _gpuIndexedIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 10, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        var indexedIndirectData = new GraphicsBuffer.IndirectDrawIndexedArgs[10];
        for (uint i = 0; i < 5; ++i)
        {
            indexedIndirectData[i] = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = _elementsPerDraw,
                instanceCount = 2,
                startIndex = 0,
                baseVertexIndex = 0,
                startInstance = 0,
            };
        }
        for (uint i = 0; i < 5; ++i)
        {
            indexedIndirectData[5 + i] = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = _elementsPerDraw,
                instanceCount = 2,
                startIndex = 0,
                baseVertexIndex = 0,
                startInstance = 0,
            };
        }
        _gpuIndexedIndirectBuffer.SetData(indexedIndirectData);


        var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        batchMetadata[0] = CreateMetadataValue(objectToWorldID, 64, true);       // matrices
        batchMetadata[1] = CreateMetadataValue(matrixPreviousID, 64 + _itemCount * UnsafeUtility.SizeOf<Vector4>() * 3, true); // previous matrices
        batchMetadata[2] = CreateMetadataValue(worldToObjectID, 64 + _itemCount * UnsafeUtility.SizeOf<Vector4>() * 3 * 2, true); // inverse matrices
        batchMetadata[3] = CreateMetadataValue(colorID, 64 + _itemCount * UnsafeUtility.SizeOf<Vector4>() * 3 * 3, true); // colors

        // Register batch
        _batchID = _batchRendererGroup.AddBatch(batchMetadata, _gpuPersistentInstanceData.bufferHandle, 0, brgWindowSize);

        _initialized = true;
    }

    private void OnDestroy()
    {
        if (_initialized)
        {
            _batchRendererGroup.RemoveBatch(_batchID);
            if (regularMaterial) _batchRendererGroup.UnregisterMaterial(_regularMaterialID);
            if (proceduralMaterial) _batchRendererGroup.UnregisterMaterial(_proceduralMaterialID);
            if (proceduralIndexedMaterial) _batchRendererGroup.UnregisterMaterial(_proceduralIndexedMaterialID);
            if (mesh) _batchRendererGroup.UnregisterMesh(_meshID);

            _batchRendererGroup.Dispose();
            _gpuPersistentInstanceData.Dispose();
            _gpuVisibleInstances.Dispose();
            _gpuIndirectBuffer.Dispose();
            _gpuIndexedIndirectBuffer.Dispose();
            for(int i = 0; i < 5; ++i)
                _ib[i].Dispose();
            _gpuPositions.Dispose();
            _gpuNormals.Dispose();
            _gpuTangents.Dispose();
            _sysmemBuffer.Dispose();
            _sysmemVisibleInstances.Dispose();
        }
    }
}
