using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    internal struct DecalSubDrawCall
    {
        public int start;
        public int end;
        public int count { get => end - start; }
    }

    /// <summary>
    /// Contains information about <see cref="DecalEntity"/> draw calls.
    /// </summary>
    internal class DecalDrawCallChunk : DecalChunk
    {
        public NativeArray<float4x4> decalToWorlds;
        public NativeArray<float4x4> normalToDecals;
        public NativeArray<float> renderingLayerMasks;
        public NativeArray<DecalSubDrawCall> subCalls;
        public NativeArray<int> subCallCounts;

        public int subCallCount { set { subCallCounts[0] = value; } get => subCallCounts[0]; }

        public override void RemoveAtSwapBack(int entityIndex)
        {
            RemoveAtSwapBack(ref decalToWorlds, entityIndex, count);
            RemoveAtSwapBack(ref normalToDecals, entityIndex, count);
            RemoveAtSwapBack(ref renderingLayerMasks, entityIndex, count);
            RemoveAtSwapBack(ref subCalls, entityIndex, count);
            count--;
        }

        public override void SetCapacity(int newCapacity)
        {
            decalToWorlds.ResizeArray(newCapacity);
            normalToDecals.ResizeArray(newCapacity);
            renderingLayerMasks.ResizeArray(newCapacity);
            subCalls.ResizeArray(newCapacity);
            capacity = newCapacity;
        }

        public override void Dispose()
        {
            subCallCounts.Dispose();

            if (capacity == 0)
                return;

            decalToWorlds.Dispose();
            normalToDecals.Dispose();
            renderingLayerMasks.Dispose();
            subCalls.Dispose();
            count = 0;
            capacity = 0;
        }
    }

    /// <summary>
    /// Outputs draw calls into <see cref="DecalDrawCallChunk"/>.
    /// </summary>
    internal class DecalCreateDrawCallSystem
    {
        private DecalEntityManager m_EntityManager;
        private ProfilingSampler m_Sampler;
        private float m_MaxDrawDistance;

        /// <summary>
        /// Provides acces to the maximum draw distance.
        /// </summary>
        public float maxDrawDistance
        {
            get { return m_MaxDrawDistance; }
            set { m_MaxDrawDistance = value; }
        }

        public DecalCreateDrawCallSystem(DecalEntityManager entityManager, float maxDrawDistance)
        {
            m_EntityManager = entityManager;
            m_Sampler = new ProfilingSampler("DecalCreateDrawCallSystem.Execute");
            m_MaxDrawDistance = maxDrawDistance;
        }

        public void Execute()
        {
            using (new ProfilingScope(null, m_Sampler))
            {
                for (int i = 0; i < m_EntityManager.chunkCount; ++i)
                    Execute(m_EntityManager.cachedChunks[i], m_EntityManager.culledChunks[i], m_EntityManager.drawCallChunks[i], m_EntityManager.cachedChunks[i].count);
            }
        }

        private void Execute(DecalCachedChunk cachedChunk, DecalCulledChunk culledChunk, DecalDrawCallChunk drawCallChunk, int count)
        {
            if (count == 0)
                return;

            DrawCallJob drawCallJob = new DrawCallJob()
            {
                decalToWorlds = cachedChunk.decalToWorlds,
                normalToWorlds = cachedChunk.normalToWorlds,
                sizeOffsets = cachedChunk.sizeOffsets,
                drawDistances = cachedChunk.drawDistances,
                angleFades = cachedChunk.angleFades,
                uvScaleBiases = cachedChunk.uvScaleBias,
                layerMasks = cachedChunk.layerMasks,
                sceneLayerMasks = cachedChunk.sceneLayerMasks,
                fadeFactors = cachedChunk.fadeFactors,
                boundingSpheres = cachedChunk.boundingSpheres,
                renderingLayerMasks = cachedChunk.renderingLayerMasks,

                cameraPosition = culledChunk.cameraPosition,
                sceneCullingMask = culledChunk.sceneCullingMask,
                cullingMask = culledChunk.cullingMask,
                visibleDecalIndices = culledChunk.visibleDecalIndices,
                visibleDecalCount = culledChunk.visibleDecalCount,
                maxDrawDistance = m_MaxDrawDistance,

                decalToWorldsDraw = drawCallChunk.decalToWorlds,
                normalToDecalsDraw = drawCallChunk.normalToDecals,
                renderingLayerMasksDraw = drawCallChunk.renderingLayerMasks,
                subCalls = drawCallChunk.subCalls,
                subCallCount = drawCallChunk.subCallCounts,
            };

            var handle = drawCallJob.Schedule(cachedChunk.currentJobHandle);
            drawCallChunk.currentJobHandle = handle;
            cachedChunk.currentJobHandle = handle;
        }

#if ENABLE_BURST_1_0_0_OR_NEWER
        [Unity.Burst.BurstCompile]
#endif
        struct DrawCallJob : IJob
        {
            [ReadOnly] public NativeArray<float4x4> decalToWorlds;
            [ReadOnly] public NativeArray<float4x4> normalToWorlds;
            [ReadOnly] public NativeArray<float4x4> sizeOffsets;
            [ReadOnly] public NativeArray<float2> drawDistances;
            [ReadOnly] public NativeArray<float2> angleFades;
            [ReadOnly] public NativeArray<float4> uvScaleBiases;
            [ReadOnly] public NativeArray<int> layerMasks;
            [ReadOnly] public NativeArray<ulong> sceneLayerMasks;
            [ReadOnly] public NativeArray<float> fadeFactors;
            [ReadOnly] public NativeArray<BoundingSphere> boundingSpheres;
            [ReadOnly] public NativeArray<uint> renderingLayerMasks;

            public Vector3 cameraPosition;
            public ulong sceneCullingMask;
            public int cullingMask;
            [ReadOnly] public NativeArray<int> visibleDecalIndices;
            public int visibleDecalCount;
            public float maxDrawDistance;

            [WriteOnly] public NativeArray<float4x4> decalToWorldsDraw;
            [WriteOnly] public NativeArray<float4x4> normalToDecalsDraw;
            [WriteOnly] public NativeArray<float> renderingLayerMasksDraw;
            [WriteOnly] public NativeArray<DecalSubDrawCall> subCalls;
            [WriteOnly] public NativeArray<int> subCallCount;

            public void Execute()
            {
                int subCallIndex = 0;
                int instanceIndex = 0;
                int instanceStart = 0;

                for (int i = 0; i < visibleDecalCount; ++i)
                {
                    int decalIndex = visibleDecalIndices[i];

#if UNITY_EDITOR
                    ulong decalSceneCullingMask = sceneLayerMasks[decalIndex];
                    if ((sceneCullingMask & decalSceneCullingMask) == 0)
                        continue;
#endif
                    int decalMask = 1 << layerMasks[decalIndex];
                    if ((cullingMask & decalMask) == 0)
                        continue;

                    BoundingSphere boundingSphere = boundingSpheres[decalIndex];
                    float2 drawDistance = drawDistances[decalIndex];

                    float distanceToDecal = (cameraPosition - boundingSphere.position).magnitude;
                    float cullDistance = math.min(drawDistance.x, maxDrawDistance) + boundingSphere.radius;
                    if (distanceToDecal > cullDistance)
                        continue;

                    decalToWorldsDraw[instanceIndex] = decalToWorlds[decalIndex];

                    float fadeFactorScaler = fadeFactors[decalIndex];
                    float2 angleFade = angleFades[decalIndex];
                    float4 uvScaleBias = uvScaleBiases[decalIndex];

                    float4x4 normalToDecals = normalToWorlds[decalIndex];
                    // NormalToWorldBatchis a Matrix4x4x but is a Rotation matrix so bottom row and last column can be used for other data to save space
                    float fadeFactor = fadeFactorScaler * math.clamp((cullDistance - distanceToDecal) / (cullDistance * (1.0f - drawDistance.y)), 0.0f, 1.0f);
                    normalToDecals.c0.w = uvScaleBias.x;
                    normalToDecals.c1.w = uvScaleBias.y;
                    normalToDecals.c2.w = uvScaleBias.z;
                    normalToDecals.c3 = new float4(fadeFactor * 1.0f, angleFade.x, angleFade.y, uvScaleBias.w);
                    normalToDecalsDraw[instanceIndex] = normalToDecals;

                    renderingLayerMasksDraw[instanceIndex] = (float)renderingLayerMasks[decalIndex];

                    instanceIndex++;

                    int instanceCount = instanceIndex - instanceStart;
                    bool isReachedMaximumBatchSize = instanceCount >= 250;
                    if (isReachedMaximumBatchSize)
                    {
                        subCalls[subCallIndex++] = new DecalSubDrawCall()
                        {
                            start = instanceStart,
                            end = instanceIndex,
                        };
                        instanceStart = instanceIndex;
                    }
                }

                int remainingInstanceCount = instanceIndex - instanceStart;
                if (remainingInstanceCount != 0)
                {
                    subCalls[subCallIndex++] = new DecalSubDrawCall()
                    {
                        start = instanceStart,
                        end = instanceIndex,
                    };
                }

                subCallCount[0] = subCallIndex;
            }
        }
    }
}
