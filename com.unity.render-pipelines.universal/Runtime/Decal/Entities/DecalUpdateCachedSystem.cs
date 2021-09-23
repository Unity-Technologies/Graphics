using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Contains <see cref="DecalProjector"/> cached properties needed for rendering.
    /// </summary>
    internal class DecalCachedChunk : DecalChunk
    {
        public MaterialPropertyBlock propertyBlock;
        public int passIndexDBuffer;
        public int passIndexEmissive;
        public int passIndexScreenSpace;
        public int passIndexGBuffer;
        public int drawOrder;
        public bool isCreated;

        public NativeArray<float4x4> decalToWorlds;
        public NativeArray<float4x4> normalToWorlds;
        public NativeArray<float4x4> sizeOffsets;
        public NativeArray<float2> drawDistances;
        public NativeArray<float2> angleFades;
        public NativeArray<float4> uvScaleBias;
        public NativeArray<int> layerMasks;
        public NativeArray<ulong> sceneLayerMasks;
        public NativeArray<float> fadeFactors;
        public NativeArray<BoundingSphere> boundingSpheres;
        public NativeArray<DecalScaleMode> scaleModes;
        public NativeArray<float3> positions;
        public NativeArray<quaternion> rotation;
        public NativeArray<float3> scales;
        public NativeArray<bool> dirty;

        public BoundingSphere[] boundingSphereArray;

        public override void RemoveAtSwapBack(int entityIndex)
        {
            RemoveAtSwapBack(ref decalToWorlds, entityIndex, count);
            RemoveAtSwapBack(ref normalToWorlds, entityIndex, count);
            RemoveAtSwapBack(ref sizeOffsets, entityIndex, count);
            RemoveAtSwapBack(ref drawDistances, entityIndex, count);
            RemoveAtSwapBack(ref angleFades, entityIndex, count);
            RemoveAtSwapBack(ref uvScaleBias, entityIndex, count);
            RemoveAtSwapBack(ref layerMasks, entityIndex, count);
            RemoveAtSwapBack(ref sceneLayerMasks, entityIndex, count);
            RemoveAtSwapBack(ref fadeFactors, entityIndex, count);
            RemoveAtSwapBack(ref boundingSphereArray, entityIndex, count);
            RemoveAtSwapBack(ref boundingSpheres, entityIndex, count);
            RemoveAtSwapBack(ref scaleModes, entityIndex, count);
            RemoveAtSwapBack(ref positions, entityIndex, count);
            RemoveAtSwapBack(ref rotation, entityIndex, count);
            RemoveAtSwapBack(ref scales, entityIndex, count);
            RemoveAtSwapBack(ref dirty, entityIndex, count);
            count--;
        }

        public override void SetCapacity(int newCapacity)
        {
            decalToWorlds.ResizeArray(newCapacity);
            normalToWorlds.ResizeArray(newCapacity);
            sizeOffsets.ResizeArray(newCapacity);
            drawDistances.ResizeArray(newCapacity);
            angleFades.ResizeArray(newCapacity);
            uvScaleBias.ResizeArray(newCapacity);
            layerMasks.ResizeArray(newCapacity);
            sceneLayerMasks.ResizeArray(newCapacity);
            fadeFactors.ResizeArray(newCapacity);
            boundingSpheres.ResizeArray(newCapacity);
            scaleModes.ResizeArray(newCapacity);
            positions.ResizeArray(newCapacity);
            rotation.ResizeArray(newCapacity);
            scales.ResizeArray(newCapacity);
            dirty.ResizeArray(newCapacity);

            ArrayExtensions.ResizeArray(ref boundingSphereArray, newCapacity);
            capacity = newCapacity;
        }

        public override void Dispose()
        {
            if (capacity == 0)
                return;

            decalToWorlds.Dispose();
            normalToWorlds.Dispose();
            sizeOffsets.Dispose();
            drawDistances.Dispose();
            angleFades.Dispose();
            uvScaleBias.Dispose();
            layerMasks.Dispose();
            sceneLayerMasks.Dispose();
            fadeFactors.Dispose();
            boundingSpheres.Dispose();
            scaleModes.Dispose();
            positions.Dispose();
            rotation.Dispose();
            scales.Dispose();
            dirty.Dispose();
            count = 0;
            capacity = 0;
        }
    }

    /// <summary>
    /// Caches <see cref="DecalProjector"/> properties into <see cref="DecalCachedChunk"/>.
    /// Uses jobs with <see cref="IJobParallelForTransform"/>.
    /// </summary>
    internal class DecalUpdateCachedSystem
    {
        private DecalEntityManager m_EntityManager;
        private ProfilingSampler m_Sampler;
        private ProfilingSampler m_SamplerJob;

        public DecalUpdateCachedSystem(DecalEntityManager entityManager)
        {
            m_EntityManager = entityManager;
            m_Sampler = new ProfilingSampler("DecalUpdateCachedSystem.Execute");
            m_SamplerJob = new ProfilingSampler("DecalUpdateCachedSystem.ExecuteJob");
        }

        public void Execute()
        {
            using (new ProfilingScope(null, m_Sampler))
            {
                for (int i = 0; i < m_EntityManager.chunkCount; ++i)
                    Execute(m_EntityManager.entityChunks[i], m_EntityManager.cachedChunks[i], m_EntityManager.entityChunks[i].count);
            }
        }

        private void Execute(DecalEntityChunk entityChunk, DecalCachedChunk cachedChunk, int count)
        {
            if (count == 0)
                return;

            cachedChunk.currentJobHandle.Complete();

            // Make sure draw order is up to date
            var material = entityChunk.material;
            if (material.HasProperty("_DrawOrder"))
                cachedChunk.drawOrder = material.GetInt("_DrawOrder");

            // Shader can change any time in editor, so we have to update passes each time
#if !UNITY_EDITOR
            if (!cachedChunk.isCreated)
#endif
            {
                int passIndexDBuffer = material.FindPass(DecalShaderPassNames.DBufferProjector);
                cachedChunk.passIndexDBuffer = passIndexDBuffer;

                int passIndexEmissive = material.FindPass(DecalShaderPassNames.DecalProjectorForwardEmissive);
                cachedChunk.passIndexEmissive = passIndexEmissive;

                int passIndexScreenSpace = material.FindPass(DecalShaderPassNames.DecalScreenSpaceProjector);
                cachedChunk.passIndexScreenSpace = passIndexScreenSpace;

                int passIndexGBuffer = material.FindPass(DecalShaderPassNames.DecalGBufferProjector);
                cachedChunk.passIndexGBuffer = passIndexGBuffer;

                cachedChunk.isCreated = true;
            }

            using (new ProfilingScope(null, m_SamplerJob))
            {
                UpdateTransformsJob updateTransformJob = new UpdateTransformsJob()
                {
                    positions = cachedChunk.positions,
                    rotations = cachedChunk.rotation,
                    scales = cachedChunk.scales,
                    dirty = cachedChunk.dirty,
                    scaleModes = cachedChunk.scaleModes,
                    sizeOffsets = cachedChunk.sizeOffsets,
                    decalToWorlds = cachedChunk.decalToWorlds,
                    normalToWorlds = cachedChunk.normalToWorlds,
                    boundingSpheres = cachedChunk.boundingSpheres,
                    minDistance = System.Single.Epsilon,
                };

                var handle = updateTransformJob.Schedule(entityChunk.transformAccessArray);
                cachedChunk.currentJobHandle = handle;
            }
        }

#if ENABLE_BURST_1_0_0_OR_NEWER
        [Unity.Burst.BurstCompile]
#endif
        public unsafe struct UpdateTransformsJob : IJobParallelForTransform
        {
            private static readonly quaternion k_MinusYtoZRotation = quaternion.EulerXYZ(-math.PI / 2.0f, 0, 0);

            public NativeArray<float3> positions;
            public NativeArray<quaternion> rotations;
            public NativeArray<float3> scales;
            public NativeArray<bool> dirty;

            [ReadOnly] public NativeArray<DecalScaleMode> scaleModes;
            [ReadOnly] public NativeArray<float4x4> sizeOffsets;
            [WriteOnly] public NativeArray<float4x4> decalToWorlds;
            [WriteOnly] public NativeArray<float4x4> normalToWorlds;
            [WriteOnly] public NativeArray<BoundingSphere> boundingSpheres;

            public float minDistance;

            private float DistanceBetweenQuaternions(quaternion a, quaternion b)
            {
                return math.distancesq(a.value, b.value);
            }

            public void Execute(int index, TransformAccess transform)
            {
                // Check if transform changed
                bool positionChanged = math.distancesq(transform.position, positions[index]) > minDistance;
                if (positionChanged)
                    positions[index] = transform.position;
                bool rotationChanged = DistanceBetweenQuaternions(transform.rotation, rotations[index]) > minDistance;
                if (rotationChanged)
                    rotations[index] = transform.rotation;
                bool scaleChanged = math.distancesq(transform.localScale, scales[index]) > minDistance;
                if (scaleChanged)
                    scales[index] = transform.localScale;

                // Early out if transform did not changed
                if (!positionChanged && !rotationChanged && !scaleChanged && !dirty[index])
                    return;

                float4x4 localToWorld;
                if (scaleModes[index] == DecalScaleMode.InheritFromHierarchy)
                {
                    localToWorld = transform.localToWorldMatrix;
                    localToWorld = math.mul(localToWorld, new float4x4(k_MinusYtoZRotation, float3.zero));
                }
                else
                {
                    quaternion rotation = math.mul(transform.rotation, k_MinusYtoZRotation);
                    localToWorld = float4x4.TRS(positions[index], rotation, new float3(1, 1, 1));
                }

                float4x4 decalRotation = localToWorld;
                // z/y axis swap for normal to decal space, Unity is column major
                float4 temp = decalRotation.c1;
                decalRotation.c1 = decalRotation.c2;
                decalRotation.c2 = temp;
                normalToWorlds[index] = decalRotation;

                float4x4 sizeOffset = sizeOffsets[index];
                float4x4 decalToWorld = math.mul(localToWorld, sizeOffset);
                decalToWorlds[index] = decalToWorld;
                boundingSpheres[index] = GetDecalProjectBoundingSphere(decalToWorld);

                dirty[index] = false;
            }

            private BoundingSphere GetDecalProjectBoundingSphere(Matrix4x4 decalToWorld)
            {
                float4 min = new float4(-0.5f, -0.5f, -0.5f, 1.0f);
                float4 max = new float4(0.5f, 0.5f, 0.5f, 1.0f);
                min = math.mul(decalToWorld, min);
                max = math.mul(decalToWorld, max);

                float3 position = ((max + min) / 2f).xyz;
                float radius = math.length(max - min) / 2f;

                BoundingSphere res = new BoundingSphere();
                res.position = position;
                res.radius = radius;
                return res;
            }
        }
    }
}
