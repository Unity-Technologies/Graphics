#define USE_INDIRECT_DRAWS

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    public class DeferredMaterialBRG
    {
        BatchRendererGroup m_BatchRendererGroup;
        GeometryPool m_GeometryPool;

#if USE_INDIRECT_DRAWS
        GraphicsBuffer m_TileMeshIndices;
        GraphicsBufferHandle m_TileMeshIndicesID;
        GraphicsBuffer m_IndirectArguments;
        GraphicsBufferHandle m_IndirectArgumentsID;
#else
        Mesh m_TileMesh;
        BatchMeshID m_TileMeshID;
#endif

        bool m_MaterialDrawListDirty;
        Dictionary<int, BRGMaterialInfo> m_MaterialsContainer;
        NativeList<BRGMaterialDrawInfo> m_MaterialDrawList;

        BatchSlot[] m_Batches;
        NativeList<GeometryPoolBatchHandle> m_BatchHandles;

        UploadBufferPool m_visibleInstancesBufferPool;
        int m_frame;

        //hacks until we can have proper indirect dispatch.
        int m_MaxNumberOfTiles;

        public const uint RenderLayerMask = 1u << 31;
        public const int MaterialTileSize = 64;

        public Mesh globalGeoMesh => m_GeometryPool.globalMesh;

        private struct BRGMaterialInfo
        {
            public Material material;
            public int refCount;
            public BatchMaterialID batchMaterialID;
            public uint materialGPUKey;
            NativeArray<Int64> m_BatchUsageBits;

            public void Initialize(int maxBatchCount, Material mat, BatchMaterialID batchMatID, uint gpuKey)
            {
                m_BatchUsageBits = new NativeArray<Int64>((maxBatchCount + 63) / 64, Allocator.Persistent);
                material = mat;
                batchMaterialID = batchMatID;
                materialGPUKey = gpuKey;
                refCount = 1;
            }

            public void Dispose()
            {
                m_BatchUsageBits.Dispose();
            }

            public bool IsUsedInBatch(int batchIndex)
            {
                return (m_BatchUsageBits[batchIndex / 64] & (1 << (batchIndex & 0x3f))) != 0;
            }

            public void SetBatchUsage(int batchIndex, bool value)
            {
                int slotIdx = batchIndex / 64;
                Int64 v = m_BatchUsageBits[slotIdx];
                v |= (1u << (batchIndex & 0x3f));
                m_BatchUsageBits[slotIdx] = v;
            }
        }

        private struct BRGMaterialDrawInfo
        {
            public BatchMaterialID batchMaterialID;
            public GeometryPoolBatchHandle batchHandle;
            public BatchID BRGBatchID;
            public uint materialGPUKey;
        }

        private struct BatchSlot
        {
            public bool valid;
            public bool ready;
            public GeometryPoolBatchHandle batchHandle;
            public GeometryPoolBatchInstanceBuffer instanceBuffer;
            public BatchID BRGBatchID;

            public static BatchSlot NewDefault()
            {
                return new BatchSlot()
                {
                    valid = false,
                    ready = false,
                    batchHandle = GeometryPoolBatchHandle.Invalid,
                    BRGBatchID = BatchID.Null,
                    instanceBuffer = new GeometryPoolBatchInstanceBuffer()
                };
            }
        }

        public GeometryPool geometryPool { get { return m_GeometryPool; } }

        //TODO: promote this function to API, as well in SceneBRG
        public unsafe static T* Malloc<T>(int count) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                Allocator.TempJob);
        }

        [BurstCompile]
        private unsafe struct DrawCommandProducerJob : IJob
        {
#if USE_INDIRECT_DRAWS
            public GraphicsBufferHandle tileMeshIndicesID;
            public GraphicsBufferHandle indirectArgumentsID;
#else
            public BatchMeshID tileMeshID;
#endif

            [ReadOnly] public NativeList<BRGMaterialDrawInfo> materialDrawInfos;
            [ReadOnly] public GraphicsBufferHandle visibleInstancesBufferHandle;
            [WriteOnly] public NativeArray<UInt32> visibleInstancesGPU;

            public NativeArray<BatchCullingOutputDrawCommands> drawCommands;

            public void Execute()
            {
                drawCommands[0].drawRanges[0] = new BatchDrawRange()
                {
                    drawCommandsBegin = 0u,
                    drawCommandsCount = (uint)materialDrawInfos.Length,
                    visibleInstancesBufferHandle = visibleInstancesBufferHandle,
                    filterSettings = new BatchFilterSettings()
                    {
                        renderingLayerMask = DeferredMaterialBRG.RenderLayerMask,
                        layer = 0x1,
                        motionMode = MotionVectorGenerationMode.Camera,
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = false,
                        staticShadowCaster = false,
                        allDepthSorted = false
                    }
                };

                for (int drawIndex = 0; drawIndex < (uint)materialDrawInfos.Length; ++drawIndex)
                {
                    BRGMaterialDrawInfo drawInfo = materialDrawInfos[drawIndex];
                    drawCommands[0].drawCommands[drawIndex] = new BatchDrawCommand
                    {
                        visibleOffset = (uint)drawIndex,
                        visibleCount = 1u,
                        batchID = drawInfo.BRGBatchID,
                        materialID = drawInfo.batchMaterialID,
                        splitVisibilityMask = (ushort)0xfful,
                        sortingPosition = 0,
#if USE_INDIRECT_DRAWS
                        flags = BatchDrawCommandFlags.Procedural | BatchDrawCommandFlags.Indirect | BatchDrawCommandFlags.Indexed,
                        proceduralIndirect = new BatchDrawCommandProceduralIndirect
                        {
                            indexBufferHandle = tileMeshIndicesID,
                            indirectBufferHandle = indirectArgumentsID,
                            indirectBufferOffset = GraphicsBuffer.IndirectDrawIndexedArgs.size * (uint)drawIndex,
                            indirectCommandCount = 1,
                            topology = MeshTopology.Triangles,
                        },
#else
                        flags = BatchDrawCommandFlags.None,
                        regular = new BatchDrawCommandRegular
                        {
                            meshID = tileMeshID,
                            submeshIndex = 0,
                        },
#endif
                    };

                    //drawCommands[0].visibleInstances[drawIndex] = (int)((uint)(drawInfo.materialGPUKey << 8) | ((uint)drawInfo.batchHandle.index & 0xFF));
                    visibleInstancesGPU[drawIndex] = (drawInfo.materialGPUKey << 8) | ((uint)drawInfo.batchHandle.index & 0xFF);
                }
            }
        }

        private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            //bake draws if they are dirty
            UpdateDraws();

            //Early quit if there is nothing to draw.
            if (m_MaterialsContainer.Count == 0 || m_MaterialDrawList.Length == 0)
                return new JobHandle();

            var visibleInstancesUploadBuffer = m_visibleInstancesBufferPool.StartBufferWrite();

            JobHandle jobHandle;
            unsafe
            {
                BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();
                drawCommands.drawCommands = Malloc<BatchDrawCommand>(m_MaterialDrawList.Length);

                //drawCommands.visibleInstances = Malloc<int>(m_MaterialDrawList.Length);
                drawCommands.visibleInstances = null;
                drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
                drawCommands.instanceSortingPositions = null;
                drawCommands.drawCommandCount = m_MaterialDrawList.Length;
                drawCommands.visibleInstanceCount = m_MaterialDrawList.Length;
                drawCommands.drawRangeCount = 1;
                drawCommands.instanceSortingPositionFloatCount = 0;

                cullingOutput.drawCommands[0] = drawCommands;

                var drawCmdProducerJob = new DrawCommandProducerJob()
                {
#if USE_INDIRECT_DRAWS
                    tileMeshIndicesID = m_TileMeshIndicesID,
                    indirectArgumentsID = m_IndirectArgumentsID,
#else
                    tileMeshID = m_TileMeshID,
#endif
                    materialDrawInfos = m_MaterialDrawList,
                    drawCommands = cullingOutput.drawCommands,
                    visibleInstancesBufferHandle = visibleInstancesUploadBuffer.bufferHandle,
                    visibleInstancesGPU = visibleInstancesUploadBuffer.gpuData,
                };

                //TODO: this can be nicely parallelized and we can write material info in batches
                jobHandle = drawCmdProducerJob.Schedule();
            }

            // TODO: WAITING FOR THE JOB HERE! THIS IS SLOW! NEED THE MULTITHREADED FENCE VERSION!
            jobHandle.Complete();
            m_visibleInstancesBufferPool.EndBufferWrite(visibleInstancesUploadBuffer);
            //m_visibleInstancesBufferPool.EndBufferWriteAfterJob(visibleInstancesUploadBuffer, jobHandleOutput);

            return jobHandle;
        }

        public DeferredMaterialBRG()
        {
            m_MaterialsContainer = new Dictionary<int, BRGMaterialInfo>();
            m_MaterialDrawListDirty = false;
            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
            m_GeometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());
            m_MaterialDrawList = new NativeList<BRGMaterialDrawInfo>(1024, Allocator.Persistent);

            m_Batches = new BatchSlot[m_GeometryPool.maxBatchCount];
            for (int i = 0; i < m_GeometryPool.maxBatchCount; ++i)
                m_Batches[i] = BatchSlot.NewDefault();

            m_BatchHandles = new NativeList<GeometryPoolBatchHandle>(m_GeometryPool.maxBatchCount, Allocator.Persistent);

            int tilesX = ((3 * 3840) + MaterialTileSize - 1) / MaterialTileSize;
            int tilesY = ((3 * 2160) + MaterialTileSize - 1) / MaterialTileSize;
            m_MaxNumberOfTiles = tilesX * tilesY;

            int indexCount = m_MaxNumberOfTiles * 6;

            var indices = new int[m_MaxNumberOfTiles * 6];
            for (int tileId = 0; tileId < m_MaxNumberOfTiles; ++tileId)
            {
                indices[tileId * 6 + 0] = 0 + tileId * 4;
                indices[tileId * 6 + 1] = 1 + tileId * 4;
                indices[tileId * 6 + 2] = 3 + tileId * 4;
                indices[tileId * 6 + 3] = 3 + tileId * 4;
                indices[tileId * 6 + 4] = 2 + tileId * 4;
                indices[tileId * 6 + 5] = 0 + tileId * 4;
            }

#if USE_INDIRECT_DRAWS
            m_TileMeshIndices = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, indexCount, 4);
            m_TileMeshIndices.SetData(indices);
            m_TileMeshIndicesID = m_TileMeshIndices.bufferHandle;
#else
            m_TileMesh = new Mesh();
            m_TileMesh.vertices = new Vector3[m_MaxNumberOfTiles * 4];
            m_TileMesh.vertexBufferTarget = GraphicsBuffer.Target.Raw;
            m_TileMesh.indexBufferTarget = GraphicsBuffer.Target.Raw;
            m_TileMesh.subMeshCount = 1;
            m_TileMesh.SetIndexBufferParams(m_MaxNumberOfTiles * 6, IndexFormat.UInt32);
            m_TileMesh.SetSubMesh(0, new SubMeshDescriptor(0, m_MaxNumberOfTiles * 6), MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);

            m_TileMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            m_TileMesh.UploadMeshData(false);
            m_TileMeshID = m_BatchRendererGroup.RegisterMesh(m_TileMesh);
#endif

            m_visibleInstancesBufferPool = new UploadBufferPool(10 * 3, 4096 * 1024);   // HACKS: Max 10 callbacks/frame, 3 frame hard coded reuse. 4MB maximum buffer size (1 million visible indices).
        }

        public bool CreateBatch(int numberOfInstances, out GeometryPoolBatchHandle outHandle)
        {
            var batchSlot = BatchSlot.NewDefault();
            outHandle = GeometryPoolBatchHandle.Invalid;

            if (batchSlot.ready)
                return false;

            if (!m_GeometryPool.CreateBatch(numberOfInstances, out batchSlot.batchHandle))
                return false;

            batchSlot.instanceBuffer = m_GeometryPool.CreateGeometryPoolBatchInstanceBuffer(batchSlot.batchHandle, isPersistant: true);
            batchSlot.valid = true;

            outHandle = batchSlot.batchHandle;
            Assertions.Assert.IsTrue(!m_Batches[outHandle.index].valid);

            m_Batches[outHandle.index] = batchSlot;
            m_BatchHandles.Add(outHandle);
            return true;
        }

        public void DestroyBatch(GeometryPoolBatchHandle batchHandle)
        {
            if (!batchHandle.valid)
                return;

            Assertions.Assert.IsTrue(m_Batches[batchHandle.index].valid);
            Assertions.Assert.IsTrue(m_Batches[batchHandle.index].batchHandle.index == batchHandle.index);
            var batchSlot = m_Batches[batchHandle.index];

            if (batchSlot.instanceBuffer.valid)
            {
                foreach (var instanceHandleIdx in batchSlot.instanceBuffer.instanceValues)
                    UnregisterInstanceAndMaterials(batchHandle, new GeometryPoolHandle { index = instanceHandleIdx });
                batchSlot.instanceBuffer.Dispose();
            }

            if (batchSlot.batchHandle.valid)
                m_GeometryPool.DestroyBatch(batchSlot.batchHandle);

            m_Batches[batchHandle.index] = BatchSlot.NewDefault();
            for (int i = 0; i < m_BatchHandles.Length; ++i)
            {
                if (m_BatchHandles[i].index == batchHandle.index)
                {
                    m_BatchHandles.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        public void UnregisterInstanceAndMaterials(GeometryPoolBatchHandle batchHandle, GeometryPoolHandle handle)
        {
            var entryInfo = m_GeometryPool.GetEntryInfo(handle);
            m_MaterialDrawListDirty = true;
            if (entryInfo.materialHashes.IsCreated)
            {
                foreach (var materialHash in entryInfo.materialHashes)
                {
                    if (!m_MaterialsContainer.TryGetValue(materialHash, out var matInfo))
                        continue;

                    --matInfo.refCount;
                    matInfo.SetBatchUsage(batchHandle.index, false);
                    if (matInfo.refCount == 0)
                    {
                        m_BatchRendererGroup.UnregisterMaterial(matInfo.batchMaterialID);
                        matInfo.Dispose();
                        m_MaterialsContainer.Remove(materialHash);
                    }
                    else
                    {
                        m_MaterialsContainer[materialHash] = matInfo;
                    }
                }
            }
            m_GeometryPool.Unregister(handle);

        }

        public bool RegisterInstance(GeometryPoolBatchHandle batchHandle, int instanceIndex, in GeometryPoolEntryDesc desc, out GeometryPoolHandle geoHandle)
        {
            geoHandle = GeometryPoolHandle.Invalid;
            if (!batchHandle.valid)
                return false;

            var batchSlot = m_Batches[batchHandle.index];
            if (!batchSlot.valid || batchSlot.ready)
                return false;

            if (!m_GeometryPool.Register(desc, out geoHandle))
                return false;

            if (geoHandle.index >= 0xFFFF)
            {
                Debug.LogError("Geo handle count for batch exceeding 16 bits.");
                m_GeometryPool.Unregister(geoHandle);
                return false;
            }

            batchSlot.instanceBuffer.instanceValues[instanceIndex] = (short)geoHandle.index;

            foreach (var submeshData in desc.submeshData)
            {
                if (submeshData.material == null)
                    continue;

                int materialHashCode = submeshData.material.GetHashCode();
                if (m_MaterialsContainer.TryGetValue(materialHashCode, out var matInfo))
                {
                    ++matInfo.refCount;
                    matInfo.SetBatchUsage(batchHandle.index, true);
                    m_MaterialsContainer[materialHashCode] = matInfo;
                }
                else
                {
                    BatchMaterialID batchMaterialID = m_BatchRendererGroup.RegisterMaterial(submeshData.material);
                    GeometryPoolMaterialEntry entry = m_GeometryPool.globalMaterialEntries[submeshData.material.GetHashCode()];
                    uint materialGPUKey = entry.materialGPUKey;
                    var newMatInfo = new BRGMaterialInfo();
                    newMatInfo.Initialize(m_GeometryPool.maxBatchCount, entry.material, batchMaterialID, materialGPUKey);
                    newMatInfo.SetBatchUsage(batchHandle.index, true);
                    m_MaterialsContainer.Add(materialHashCode, newMatInfo);

                }
            }
            return true;
        }

        public void SubmitBatch(GeometryPoolBatchHandle batchHandle, NativeArray<MetadataValue> batchMetadata, GraphicsBufferHandle batchBuffer)
        {
            if (!batchHandle.valid)
                return;

            var batchSlot = m_Batches[batchHandle.index];
            if (batchSlot.ready)
                return;

            m_GeometryPool.SetBatchInstanceData(batchHandle, batchSlot.instanceBuffer);
            m_Batches[batchHandle.index] = batchSlot;

            if (batchSlot.BRGBatchID != BatchID.Null)
                m_BatchRendererGroup.RemoveBatch(batchSlot.BRGBatchID);
            batchSlot.BRGBatchID = m_BatchRendererGroup.AddBatch(batchMetadata, batchBuffer);

            batchSlot.ready = true;
            m_Batches[batchHandle.index] = batchSlot;
            m_GeometryPool.SendGpuCommands();
            m_MaterialDrawListDirty = true;
        }

        public void Update()
        {
            m_visibleInstancesBufferPool.SetFrame(m_frame);
            m_visibleInstancesBufferPool.SetReuseFrame(m_frame - 3);    // Reuse 3 frames old buffers. TODO: Use the proper API  to know when GPU has stopped using the data!
            m_frame++;
        }

        private void UpdateDraws()
        {
            if (!m_MaterialDrawListDirty)
                return;

            m_MaterialDrawList.Clear();
            foreach (var batchHandle in m_BatchHandles)
            {
                var batchSlot = m_Batches[batchHandle.index];
                foreach (var pair in m_MaterialsContainer)
                {
                    var materialInfo = pair.Value;
                    if (!materialInfo.IsUsedInBatch(batchHandle.index))
                        continue;

                    m_MaterialDrawList.Add(new BRGMaterialDrawInfo()
                    {
                        batchMaterialID = materialInfo.batchMaterialID,
                        batchHandle = batchHandle,
                        materialGPUKey = materialInfo.materialGPUKey,
                        BRGBatchID = batchSlot.BRGBatchID
                    });
                }
            }

#if USE_INDIRECT_DRAWS
            if (m_IndirectArguments != null)
            {
                m_IndirectArguments.Dispose();
            }
            m_IndirectArguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, GraphicsBuffer.UsageFlags.None, m_MaterialDrawList.Length, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            m_IndirectArgumentsID = m_IndirectArguments.bufferHandle;

            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[m_MaterialDrawList.Length];
            for (int i = 0; i < m_MaterialDrawList.Length; ++i)
            {
                args[i].indexCountPerInstance = (uint)m_MaxNumberOfTiles * 6;
                args[i].instanceCount = 1;
                args[i].startIndex = 0;
                args[i].baseVertexIndex = 0;
                args[i].startInstance = (uint)i;
            }
            m_IndirectArguments.SetData(args);
#endif

            m_MaterialDrawListDirty = false;
        }

        public void Dispose()
        {
#if USE_INDIRECT_DRAWS
            m_TileMeshIndices.Dispose();
            m_IndirectArguments.Dispose();
#endif

            m_visibleInstancesBufferPool.Dispose();

            foreach (var b in m_Batches)
            {
                if (b.valid)
                    DestroyBatch(b.batchHandle);
            }

            m_Batches = null;
            m_BatchHandles.Dispose();
            m_BatchRendererGroup.Dispose();
            m_GeometryPool.Dispose();
            m_MaterialDrawList.Dispose();
        }
    }

}
