#define DEBUG_LOG_SCENE
//#define DEBUG_LOG_CULLING
//#define DEBUG_LOG_CULLING_RESULTS_SLOW

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{

    [Flags]
    public enum RangeShadowFlags
    {
        None = 0,
        ReceiveShadows = 1,
        StaticShadowCaster = 2,
    }

    public struct RangeKey : IEquatable<RangeKey>
    {
        public byte layer;
        public uint renderingLayerMask;
        public ShadowCastingMode shadowCastingMode;
        public RangeShadowFlags shadowFlags;

        public bool Equals(RangeKey other)
        {
            return
                layer == other.layer &&
                renderingLayerMask == other.renderingLayerMask &&
                shadowCastingMode == other.shadowCastingMode &&
                shadowFlags == other.shadowFlags;
        }
    }

    public struct DrawRange
    {
        public RangeKey key;
        public int drawCount;
        public int drawOffset;
    }

    public struct DrawKey : IEquatable<DrawKey>
    {
        public BatchMeshID meshID;
        public uint submeshIndex;
        public BatchMaterialID material;
        public BatchDrawCommandFlags flags;

        public RangeKey range;

        public bool Equals(DrawKey other)
        {
            return
                meshID == other.meshID &&
                submeshIndex == other.submeshIndex &&
                material == other.material &&
                flags == other.flags &&
                range.Equals(other.range);
        }
    }

    public struct DrawBatch
    {
        public DrawKey key;
        public int instanceCount;
        public int instanceOffset;
    }

    public struct DrawInstance
    {
        public DrawKey key;
        public int instanceIndex;
    }

    public struct DrawRenderer
    {
        public AABB bounds;
    }

    struct SHProperties
    {
        public float4 SHAr;
        public float4 SHAg;
        public float4 SHAb;
        public float4 SHBr;
        public float4 SHBg;
        public float4 SHBb;
        public float4 SHC;

        public SHProperties(SphericalHarmonicsL2 sh)
        {
            SHAr = GetSHA(sh, 0);
            SHAg = GetSHA(sh, 1);
            SHAb = GetSHA(sh, 2);

            SHBr = GetSHB(sh, 0);
            SHBg = GetSHB(sh, 1);
            SHBb = GetSHB(sh, 2);

            SHC = GetSHC(sh);
        }

        static float4 GetSHA(SphericalHarmonicsL2 sh, int i)
        {
            return new float4(sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6]);
        }

        static float4 GetSHB(SphericalHarmonicsL2 sh, int i)
        {
            return new float4(sh[i, 4], sh[i, 5], sh[i, 6] * 3f, sh[i, 7]);
        }

        static float4 GetSHC(SphericalHarmonicsL2 sh)
        {
            return new float4(sh[0, 8], sh[1, 8], sh[2, 8], 1);
        }
    }

    unsafe class SceneBRG
    {
        private BatchRendererGroup m_BatchRendererGroup;
        private ComputeBuffer m_GPUPersistentInstanceData;
        private BatchBufferID m_GPUPersistanceBufferId;

        private BatchID m_batchID;
        private bool m_initialized;

        private NativeHashMap<RangeKey, int> m_rangeHash;
        private NativeList<DrawRange> m_drawRanges;

        private NativeHashMap<DrawKey, int> m_batchHash;
        private NativeList<DrawBatch> m_drawBatches;

        private NativeList<DrawInstance> m_instances;
        private NativeArray<int> m_instanceIndices;
        private NativeArray<int> m_drawIndices;
        private NativeArray<DrawRenderer> m_renderers;

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
                NameID = nameID, Value = (uint)gpuAddress | (isOverridden ? kIsOverriddenBit : 0),
            };
        }

        [BurstCompile]
        private struct CullingJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<FrustumPlanes.PlanePacket4> planes;

            [ReadOnly] public NativeArray<DrawRenderer> renderers;

            [WriteOnly] public NativeArray<ulong> rendererVisibility;

            public void Execute(int index)
            {
                // Each invocation is culling 64 renderers (64 bit bitfield)
                int start = index * 64;
                int end = math.min(start + 64, renderers.Length);

                ulong visibleBits = 0;
                for (int i = start; i < end; i++)
                {
                    ulong bit = FrustumPlanes.Intersect2NoPartial(planes, renderers[i].bounds) ==
                                FrustumPlanes.IntersectResult.In
                        ? 1ul
                        : 0ul;
                    visibleBits |= bit << (i - start);
                }

                rendererVisibility[index] = visibleBits;
            }
        }

        [BurstCompile]
        private struct DrawCommandOutputJob : IJob
        {
            public BatchID batchID;
            public int viewID;
            public int sliceID;

            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ulong> rendererVisibility;

            [ReadOnly] public NativeArray<int> instanceIndices;

            [ReadOnly] public NativeList<DrawBatch> drawBatches;

            [ReadOnly] public NativeList<DrawRange> drawRanges;

            [ReadOnly] public NativeArray<int> drawIndices;

            public NativeArray<BatchCullingOutputDrawCommands> drawCommands;

            public void Execute()
            {
                var draws = drawCommands[0];

                int outIndex = 0;
                int outBatch = 0;
                int outRange = 0;

                int activeBatch = 0;
                int internalIndex = 0;
                int batchStartIndex = 0;

                int activeRange = 0;
                int internalDraw = 0;
                int rangeStartIndex = 0;

                for (int i = 0; i < instanceIndices.Length; i++)
                {
                    var rendererIndex = instanceIndices[i];
                    bool visible = (rendererVisibility[rendererIndex / 64] & (1ul << (rendererIndex % 64))) != 0;

                    if (visible)
                    {
                        draws.visibleInstances[outIndex] = instanceIndices[i];
                        outIndex++;
                    }

                    internalIndex++;

                    // Next draw batch?
                    var remappedIndex =
                        drawIndices[activeBatch]; // DrawIndices remap to get DrawCommands ordered by DrawRange
                    if (internalIndex == drawBatches[remappedIndex].instanceCount)
                    {
                        var visibleCount = outIndex - batchStartIndex;
                        if (visibleCount > 0)
                        {
                            draws.drawCommands[outBatch] = new BatchDrawCommand
                            {
                                visibleOffset = (uint)batchStartIndex,
                                visibleCount = (uint)visibleCount,
                                batchID = batchID,
                                materialID = drawBatches[remappedIndex].key.material,
                                meshID = drawBatches[remappedIndex].key.meshID,
                                submeshIndex = drawBatches[remappedIndex].key.submeshIndex,
                                flags = drawBatches[remappedIndex].key.flags,
                                sortingPosition = 0
                            };
                            outBatch++;
                        }

                        batchStartIndex = outIndex;
                        internalIndex = 0;
                        activeBatch++;
                        internalDraw++;

                        // Next draw range?
                        if (internalDraw == drawRanges[activeRange].drawCount)
                        {
                            var visibleDrawCount = outBatch - rangeStartIndex;
                            if (visibleDrawCount > 0)
                            {
                                var rangeKey = drawRanges[activeRange].key;
                                draws.drawRanges[outRange] = new BatchDrawRange
                                {
                                    drawCommandsBegin = (uint)rangeStartIndex,
                                    drawCommandsCount = (uint)visibleDrawCount,
                                    filterSettings = new BatchFilterSettings
                                    {
                                        renderingLayerMask = rangeKey.renderingLayerMask,
                                        layer = rangeKey.layer,
                                        motionMode = MotionVectorGenerationMode.Camera,
                                        shadowCastingMode = rangeKey.shadowCastingMode,
                                        receiveShadows =
                                            (rangeKey.shadowFlags & RangeShadowFlags.ReceiveShadows) ==
                                            RangeShadowFlags.ReceiveShadows,
                                        staticShadowCaster =
                                            (rangeKey.shadowFlags & RangeShadowFlags.StaticShadowCaster) ==
                                            RangeShadowFlags.StaticShadowCaster,
                                        allDepthSorted = false
                                    }
                                };
                                outRange++;
                            }

                            rangeStartIndex = outBatch;
                            internalDraw = 0;
                            activeRange++;
                        }
                    }
                }

                draws.drawCommandCount = outBatch;
                draws.visibleInstanceCount = outIndex;
                draws.drawRangeCount = outRange;
                drawCommands[0] = draws;

#if DEBUG_LOG_CULLING_RESULTS_SLOW
            Debug.Log(
                "[DrawCommandOutputJob] viewID=" + viewID +
                " sliceID=" + sliceID +
                " ranges=" + draws.drawRangeCount +
                " draws=" + draws.drawCommandCount +
                " instances=" + draws.visibleInstanceCount);
#endif
            }
        }

        public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            if (!m_initialized)
            {
                return new JobHandle();
            }

            var cc = cullingContext;
            var planes = FrustumPlanes.BuildSOAPlanePackets(cc.cullingPlanes, Allocator.TempJob);

            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();
            drawCommands.drawRanges = Malloc<BatchDrawRange>(m_drawRanges.Length);
            drawCommands.drawCommands = Malloc<BatchDrawCommand>(m_drawBatches.Length);
            drawCommands.visibleInstances = Malloc<int>(m_instanceIndices.Length);

            // Zero init: Culling job sets the values!
            drawCommands.drawRangeCount = 0;
            drawCommands.drawCommandCount = 0;
            drawCommands.visibleInstanceCount = 0;

            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;

            cullingOutput.drawCommands[0] = drawCommands;

            var visibilityLength = (m_renderers.Length + 63) / 64;
            var rendererVisibility = new NativeArray<ulong>(visibilityLength, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);

            var cullingJob = new CullingJob
            {
                planes = planes, renderers = m_renderers, rendererVisibility = rendererVisibility
            };

            var drawOutputJob = new DrawCommandOutputJob
            {
                viewID = cc.viewID.GetInstanceID(),
                sliceID = cc.viewID.GetInstanceID(),
                batchID = m_batchID,
                rendererVisibility = rendererVisibility,
                instanceIndices = m_instanceIndices,
                drawBatches = m_drawBatches,
                drawRanges = m_drawRanges,
                drawIndices = m_drawIndices,
                drawCommands = cullingOutput.drawCommands
            };

#if DEBUG_LOG_CULLING
        Debug.Log(
            "[OnPerformCulling] viewType=" + cc.viewType +
            " viewID=" + cc.viewID.GetInstanceID() +
            " viewID.slice=" + cc.viewID.GetSliceIndex() +
            " LOD.position=" + cc.lodParameters.cameraPosition +
            " LOD.ortho=" + cc.lodParameters.isOrthographic +
            " LOD.fov=" + cc.lodParameters.fieldOfView +
            " layerMask=" + cc.cullingLayerMask +
            " sceneMask=" + cc.sceneCullingMask +
            " nearPlane=" + cc.nearPlane +
            " cullingPlanes=" + cc.cullingPlanes.Length);
#endif

            var jobHandleCulling = cullingJob.Schedule(visibilityLength, 2);
            var jobHandleOutput = drawOutputJob.Schedule(jobHandleCulling);

            return jobHandleOutput;
        }

        // Start is called before the first frame update
        public void Initialize(List<MeshRenderer> renderers)
        {
            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

            // Create a batch...
#if DEBUG_LOG_SCENE
            Debug.Log("Converting " + renderers.Count + " renderers...");
#endif

            m_renderers = new NativeArray<DrawRenderer>(renderers.Count, Allocator.Persistent);
            m_batchHash = new NativeHashMap<DrawKey, int>(1024, Allocator.Persistent);
            m_rangeHash = new NativeHashMap<RangeKey, int>(1024, Allocator.Persistent);
            m_drawBatches = new NativeList<DrawBatch>(Allocator.Persistent);
            m_drawRanges = new NativeList<DrawRange>(Allocator.Persistent);

            // Fill the GPU-persistent scene data ComputeBuffer
            int bigDataBufferVector4Count =
                4 /*zero*/
                + 1 /*probes*/
                + 1 /*speccube*/
                + 7 * m_renderers.Length /*per renderer SH*/
                + 1 * m_renderers.Length /*per renderer probe occlusion*/
                + 2 * m_renderers.Length /* per renderer lightmapindex + scale/offset*/
                + m_renderers.Length * 3 * 2 /*per renderer 4x3 matrix+inverse*/;
            var vectorBuffer = new NativeArray<Vector4>(bigDataBufferVector4Count, Allocator.Temp);

            // First 4xfloat4 of ComputeBuffer needed to be zero filled for default property fall back!
            vectorBuffer[0] = new Vector4(0, 0, 0, 0);
            vectorBuffer[1] = new Vector4(0, 0, 0, 0);
            vectorBuffer[2] = new Vector4(0, 0, 0, 0);
            vectorBuffer[3] = new Vector4(0, 0, 0, 0);
            var startOffset = 4;

            var specCubeOffset = startOffset;
            vectorBuffer[specCubeOffset] = ReflectionProbe.defaultTextureHDRDecodeValues;
            startOffset++;

            var SHArOffset = startOffset;
            var SHAgOffset = SHArOffset + m_renderers.Length;
            var SHAbOffset = SHAgOffset + m_renderers.Length;
            var SHBrOffset = SHAbOffset + m_renderers.Length;
            var SHBgOffset = SHBrOffset + m_renderers.Length;
            var SHBbOffset = SHBgOffset + m_renderers.Length;
            var SHCOffset = SHBbOffset + m_renderers.Length;

            var probeOcclusionOffset = SHCOffset + m_renderers.Length;
            var lightMapIndexOffset = probeOcclusionOffset + m_renderers.Length;
            var lightMapScaleOffset = lightMapIndexOffset + m_renderers.Length;
            var localToWorldOffset = lightMapScaleOffset + m_renderers.Length;
            var worldToLocalOffset = localToWorldOffset + m_renderers.Length * 3;

            m_instances = new NativeList<DrawInstance>(1024, Allocator.Persistent);

            var lightmaps = LightMaps.GetLightmapsStruct();
            var materialMap = LightMaps.GenerateRenderersToMaterials(renderers, lightmaps);

            LightProbesQuery lpq = new LightProbesQuery(Allocator.Temp);

            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];

                m_renderers[i] = new DrawRenderer
                {
                    bounds = new AABB {Center = new float3(0, 0, 0), Extents = new float3(0, 0, 0)}
                };

                var meshFilter = renderer.gameObject.GetComponent<MeshFilter>();
                if (!renderer || !meshFilter || !meshFilter.sharedMesh || renderer.enabled == false)
                {
                    continue;
                }

                // Disable the existing Unity MeshRenderer to avoid double rendering!
                renderer.enabled = false;

                /*  mat4x3 packed like this:
                      p1.x, p1.w, p2.z, p3.y,
                      p1.y, p2.x, p2.w, p3.z,
                      p1.z, p2.y, p3.x, p3.w,
                      0.0,  0.0,  0.0,  1.0
                */

                vectorBuffer[i + lightMapIndexOffset] = new Vector4(renderer.lightmapIndex, 0, 0, 0);
                vectorBuffer[i + lightMapScaleOffset] = renderer.lightmapScaleOffset;

                var rendererTransform = renderer.transform;
                var m = rendererTransform.localToWorldMatrix;
                vectorBuffer[i * 3 + 0 + localToWorldOffset] = new Vector4(m.m00, m.m10, m.m20, m.m01);
                vectorBuffer[i * 3 + 1 + localToWorldOffset] = new Vector4(m.m11, m.m21, m.m02, m.m12);
                vectorBuffer[i * 3 + 2 + localToWorldOffset] = new Vector4(m.m22, m.m03, m.m13, m.m23);

                var mi = rendererTransform.worldToLocalMatrix;
                vectorBuffer[i * 3 + 0 + worldToLocalOffset] = new Vector4(mi.m00, mi.m10, mi.m20, mi.m01);
                vectorBuffer[i * 3 + 1 + worldToLocalOffset] = new Vector4(mi.m11, mi.m21, mi.m02, mi.m12);
                vectorBuffer[i * 3 + 2 + worldToLocalOffset] = new Vector4(mi.m22, mi.m03, mi.m13, mi.m23);

                lpq.CalculateInterpolatedLightAndOcclusionProbe(rendererTransform.position, -1, out var lp,
                    out var probeOcclusion);

                var sh = new SHProperties(lp);
                vectorBuffer[SHArOffset + i] = sh.SHAr;
                vectorBuffer[SHAgOffset + i] = sh.SHAg;
                vectorBuffer[SHAbOffset + i] = sh.SHAb;
                vectorBuffer[SHBrOffset + i] = sh.SHBr;
                vectorBuffer[SHBgOffset + i] = sh.SHBg;
                vectorBuffer[SHBbOffset + i] = sh.SHBb;
                vectorBuffer[SHCOffset + i] = sh.SHC;

                vectorBuffer[probeOcclusionOffset + i] = probeOcclusion;

                // Renderer bounds
                var transformedBounds = AABB.Transform(m, meshFilter.sharedMesh.bounds.ToAABB());
                m_renderers[i] = new DrawRenderer {bounds = transformedBounds};

                var mesh = m_BatchRendererGroup.RegisterMesh(meshFilter.sharedMesh);

                // Different renderer settings? -> new draw range
                var rangeKey = new RangeKey
                {
                    layer = (byte)renderer.gameObject.layer,
                    renderingLayerMask = renderer.renderingLayerMask,
                    shadowCastingMode = renderer.shadowCastingMode,
                    shadowFlags = (renderer.receiveShadows ? RangeShadowFlags.ReceiveShadows : 0) |
                                  (renderer.staticShadowCaster ? RangeShadowFlags.StaticShadowCaster : 0)
                };

                var drawRange = new DrawRange {key = rangeKey, drawCount = 0, drawOffset = 0};

                int drawRangeIndex;
                if (m_rangeHash.TryGetValue(rangeKey, out drawRangeIndex))
                {
                    drawRange = m_drawRanges[drawRangeIndex];
                }
                else
                {
                    drawRangeIndex = m_drawRanges.Length;
                    m_drawRanges.Add(drawRange);
                    m_rangeHash[rangeKey] = drawRangeIndex;
                }

                // Sub-meshes...
                var sharedMaterials = new List<Material>();
                renderer.GetSharedMaterials(sharedMaterials);
                var startSubMesh = renderer.subMeshStartIndex;
                for (int matIndex = 0; matIndex < sharedMaterials.Count; matIndex++)
                {
                    Material matToUse;
                    if (!materialMap.TryGetValue(new Tuple<Renderer, int>(renderer, matIndex), out matToUse))
                        matToUse = sharedMaterials[matIndex];

                    var material = m_BatchRendererGroup.RegisterMaterial(matToUse);

                    var flags = BatchDrawCommandFlags.None;

                    bool flipWinding = math.determinant(renderer.transform.localToWorldMatrix) < 0.0;

                    if (flipWinding)
                        flags |= BatchDrawCommandFlags.FlipWinding;

                    var key = new DrawKey
                    {
                        material = material,
                        meshID = mesh,
                        submeshIndex = (uint)(matIndex + startSubMesh),
                        flags = flags,
                        range = rangeKey
                    };

                    var drawBatch = new DrawBatch {key = key, instanceCount = 0, instanceOffset = 0};

                    m_instances.Add(new DrawInstance {key = key, instanceIndex = i});

                    int drawBatchIndex;
                    if (m_batchHash.TryGetValue(key, out drawBatchIndex))
                    {
                        drawBatch = m_drawBatches[drawBatchIndex];
                    }
                    else
                    {
                        drawBatchIndex = m_drawBatches.Length;
                        m_drawBatches.Add(drawBatch);
                        m_batchHash[key] = drawBatchIndex;

                        drawRange.drawCount++;
                        m_drawRanges[drawRangeIndex] = drawRange;
                    }

                    drawBatch.instanceCount++;
                    m_drawBatches[drawBatchIndex] = drawBatch;
                }
            }

            m_GPUPersistentInstanceData =
                new ComputeBuffer((int)bigDataBufferVector4Count * 16 / 4, 4, ComputeBufferType.Raw);
            m_GPUPersistentInstanceData.SetData(vectorBuffer);

            m_GPUPersistanceBufferId = m_BatchRendererGroup.RegisterBuffer(m_GPUPersistentInstanceData);

#if DEBUG_LOG_SCENE
            Debug.Log("DrawRanges: " + m_drawRanges.Length + ", DrawBatches: " + m_drawBatches.Length +
                      ", Instances: " + m_instances.Length);
#endif

            // Prefix sum to calculate draw offsets for each DrawRange
            int prefixSum = 0;
            for (int i = 0; i < m_drawRanges.Length; i++)
            {
                var drawRange = m_drawRanges[i];
                drawRange.drawOffset = prefixSum;
                m_drawRanges[i] = drawRange;
                prefixSum += drawRange.drawCount;
            }

            // Generate draw index ranges for each DrawRange
            m_drawIndices = new NativeArray<int>(m_drawBatches.Length, Allocator.Persistent);
            var m_internalRangeIndex = new NativeArray<int>(m_drawRanges.Length, Allocator.Temp);
            for (int i = 0; i < m_drawBatches.Length; i++)
            {
                var draw = m_drawBatches[i];
                if (m_rangeHash.TryGetValue(draw.key.range, out int drawRangeIndex))
                {
                    var drawRange = m_drawRanges[drawRangeIndex];
                    m_drawIndices[drawRange.drawOffset + m_internalRangeIndex[drawRangeIndex]] = i;
                    m_internalRangeIndex[drawRangeIndex]++;
                }
            }

            m_internalRangeIndex.Dispose();

            // Prefix sum to calculate instance offsets for each DrawCommand
            prefixSum = 0;
            for (int i = 0; i < m_drawBatches.Length; i++)
            {
                // DrawIndices remap to get DrawCommands ordered by DrawRange
                var remappedIndex = m_drawIndices[i];
                var drawBatch = m_drawBatches[remappedIndex];
                drawBatch.instanceOffset = prefixSum;
                m_drawBatches[remappedIndex] = drawBatch;
                prefixSum += drawBatch.instanceCount;
            }

            // Generate instance index ranges for each DrawCommand
            m_instanceIndices = new NativeArray<int>(m_instances.Length, Allocator.Persistent);
            var m_internalDrawIndex = new NativeArray<int>(m_drawBatches.Length, Allocator.Temp);
            for (int i = 0; i < m_instances.Length; i++)
            {
                var instance = m_instances[i];
                if (m_batchHash.TryGetValue(instance.key, out int drawBatchIndex))
                {
                    var drawBatch = m_drawBatches[drawBatchIndex];
                    m_instanceIndices[drawBatch.instanceOffset + m_internalDrawIndex[drawBatchIndex]] =
                        instance.instanceIndex;
                    m_internalDrawIndex[drawBatchIndex]++;
                }
            }

            m_internalDrawIndex.Dispose();

            // Bounds ("infinite")
            UnityEngine.Bounds bounds =
                new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
            m_BatchRendererGroup.SetGlobalBounds(bounds);

            // Batch metadata buffer...

            // Per instance data
            int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
            int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
            int colorID = Shader.PropertyToID("_BaseColor");
            int lightmapIndexID = Shader.PropertyToID("unity_LightmapIndex");
            int lightmapSTID = Shader.PropertyToID("unity_LightmapST");

            // Global data (should be moved to C++ side)
            int probesOcclusionID = Shader.PropertyToID("unity_ProbesOcclusion");
            int specCubeID = Shader.PropertyToID("unity_SpecCube0_HDR");
            int SHArID = Shader.PropertyToID("unity_SHAr");
            int SHAgID = Shader.PropertyToID("unity_SHAg");
            int SHAbID = Shader.PropertyToID("unity_SHAb");
            int SHBrID = Shader.PropertyToID("unity_SHBr");
            int SHBgID = Shader.PropertyToID("unity_SHBg");
            int SHBbID = Shader.PropertyToID("unity_SHBb");
            int SHCID = Shader.PropertyToID("unity_SHC");

            var batchMetadata = new NativeArray<MetadataValue>(13, Allocator.Temp);
            batchMetadata[0] = CreateMetadataValue(objectToWorldID,
                localToWorldOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[1] = CreateMetadataValue(worldToObjectID,
                worldToLocalOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[2] =
                CreateMetadataValue(lightmapSTID, lightMapScaleOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[3] = CreateMetadataValue(lightmapIndexID,
                lightMapIndexOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[4] = CreateMetadataValue(probesOcclusionID,
                probeOcclusionOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[5] = CreateMetadataValue(specCubeID, specCubeOffset * UnsafeUtility.SizeOf<Vector4>(), false);
            batchMetadata[6] = CreateMetadataValue(SHArID, SHArOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[7] = CreateMetadataValue(SHAgID, SHAgOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[8] = CreateMetadataValue(SHAbID, SHAbOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[9] = CreateMetadataValue(SHBrID, SHBrOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[10] = CreateMetadataValue(SHBgID, SHBgOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[11] = CreateMetadataValue(SHBbID, SHBbOffset * UnsafeUtility.SizeOf<Vector4>(), true);
            batchMetadata[12] = CreateMetadataValue(SHCID, SHCOffset * UnsafeUtility.SizeOf<Vector4>(), true);

            // Register batch
            m_batchID = m_BatchRendererGroup.AddBatch(batchMetadata, m_GPUPersistanceBufferId);

            m_initialized = true;
        }

        void Update()
        {
            // TODO: Implement delta update for transforms
            // https://docs.unity3d.com/ScriptReference/Transform-hasChanged.html
            // https://docs.unity3d.com/ScriptReference/Jobs.TransformAccess.html
        }

        public void Destroy()
        {
            if (m_initialized)
            {
                // NOTE: Don't need to remove batch or unregister BatchRendererGroup resources. BRG.Dispose takes care of that.
                m_BatchRendererGroup.Dispose();
                m_GPUPersistentInstanceData.Dispose();

                m_renderers.Dispose();
                m_batchHash.Dispose();
                m_rangeHash.Dispose();
                m_drawBatches.Dispose();
                m_drawRanges.Dispose();
                m_instances.Dispose();
                m_instanceIndices.Dispose();
                m_drawIndices.Dispose();
            }
        }
    }

    public class RenderBRG : MonoBehaviour
    {
        private Dictionary<Scene, SceneBRG> m_Scenes = new();

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            CleanUp();
        }

        private static void GetValidChildRenderers(GameObject root, List<MeshRenderer> toAppend)
        {
            if (root == null || root.activeInHierarchy == false || root.GetComponent<BRGNoConvert>() != null)
                return;

            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null)
                toAppend.Add(mr);

            for (var i = 0; i < root.transform.childCount; ++i)
                GetValidChildRenderers(root.transform.GetChild(i).gameObject, toAppend);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var renderers = new List<MeshRenderer>();
            foreach (var go in scene.GetRootGameObjects())
                GetValidChildRenderers(go, renderers);

            SceneBRG brg = new SceneBRG();
            brg.Initialize(renderers);
            m_Scenes.Add(scene, brg);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            m_Scenes.TryGetValue(scene, out var brg);
            if (brg != null)
            {
                brg.Destroy();
                m_Scenes.Remove(scene);
            }
        }

        private void CleanUp()
        {
            foreach (var scene in m_Scenes)
                scene.Value?.Destroy();

            m_Scenes.Clear();
        }

        private void OnDestroy()
        {
            CleanUp();
        }
    }
}
