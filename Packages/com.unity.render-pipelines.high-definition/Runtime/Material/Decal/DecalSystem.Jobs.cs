using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Jobs;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class DecalSystem
    {
        public void StartDecalUpdateJobs()
        {
            var decalSetsEnum = m_DecalSets.GetEnumerator();
            while (decalSetsEnum.MoveNext())
            {
                DecalSet decalSet = decalSetsEnum.Current.Value;
                if (decalSet.Count == 0)
                    continue;

                decalSet.updateJobHandle.Complete();
                decalSet.StartUpdateJob();
            }
        }

        partial class DecalSet
        {
            internal JobHandle updateJobHandle { get { return m_UpdateJobHandle; } }
            private JobHandle m_UpdateJobHandle;

            private TransformAccessArray m_CachedTransforms = new TransformAccessArray(kDecalBlockSize);
            private NativeArray<float3> m_Positions = new NativeArray<float3>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<quaternion> m_Rotations = new NativeArray<quaternion>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<float3> m_Scales = new NativeArray<float3>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<float3> m_Sizes = new NativeArray<float3>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<float3> m_Offsets = new NativeArray<float3>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<quaternion> m_ResolvedRotations = new NativeArray<quaternion>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<float3> m_ResolvedScales = new NativeArray<float3>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<float4x4> m_ResolvedSizeOffsets = new NativeArray<float4x4>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<DecalScaleMode> m_ScaleModes = new NativeArray<DecalScaleMode>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<float4x4> m_NormalToWorlds = new NativeArray<float4x4>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<float4x4> m_DecalToWorlds = new NativeArray<float4x4>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<BoundingSphere> m_BoundingSpheres = new NativeArray<BoundingSphere>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            private NativeArray<bool> m_Dirty = new NativeArray<bool>(kDecalBlockSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            private BoundingSphere[] m_CachedBoundingSpheres = new BoundingSphere[kDecalBlockSize];

            public void ResolveUpdateJob()
            {
                m_UpdateJobHandle.Complete();
                Assert.IsTrue(m_CachedBoundingSpheres.Length == m_BoundingSpheres.Length);
                m_BoundingSpheres.CopyTo(m_CachedBoundingSpheres);
            }

            private void GrowJobArrays(int growByAmount)
            {
                int newCapacity = m_DecalsCount + growByAmount;

                m_CachedTransforms.capacity = newCapacity;

                m_Positions.ResizeArray(newCapacity);
                m_Rotations.ResizeArray(newCapacity);
                m_Scales.ResizeArray(newCapacity);
                m_Sizes.ResizeArray(newCapacity);
                m_Offsets.ResizeArray(newCapacity);
                m_ResolvedRotations.ResizeArray(newCapacity);
                m_ResolvedScales.ResizeArray(newCapacity);
                m_ResolvedSizeOffsets.ResizeArray(newCapacity);
                m_ScaleModes.ResizeArray(newCapacity);
                m_NormalToWorlds.ResizeArray(newCapacity);
                m_DecalToWorlds.ResizeArray(newCapacity);
                m_BoundingSpheres.ResizeArray(newCapacity);
                m_Dirty.ResizeArray(newCapacity);
                ArrayExtensions.ResizeArray(ref m_CachedBoundingSpheres, newCapacity);
            }

            private void UpdateJobArrays(int index, DecalProjector decalProjector)
            {
                Assert.IsTrue(index <= m_DecalsCount, "Inconsistent indices found on m_CachedTransforms for decals");
                if (index == m_CachedTransforms.length)
                {
                    m_CachedTransforms.Add(decalProjector.transform);
                }
                else
                {
                    m_CachedTransforms[index] = decalProjector.transform;
                }

                m_Positions[index] = decalProjector.transform.position;
                m_Rotations[index] = decalProjector.transform.rotation;
                m_Scales[index] = decalProjector.transform.lossyScale;
                m_Sizes[index] = decalProjector.size;
                m_Offsets[index] = decalProjector.pivot;
                m_ScaleModes[index] = decalProjector.scaleMode;
                m_Dirty[index] = true;
            }

            private void RemoveFromJobArrays(int removeAtIndex)
            {
                m_CachedTransforms.RemoveAtSwapBack(removeAtIndex);
                m_Positions[removeAtIndex] = m_Positions[m_DecalsCount - 1];
                m_Rotations[removeAtIndex] = m_Rotations[m_DecalsCount - 1];
                m_Scales[removeAtIndex] = m_Scales[m_DecalsCount - 1];
                m_Sizes[removeAtIndex] = m_Sizes[m_DecalsCount - 1];
                m_Offsets[removeAtIndex] = m_Offsets[m_DecalsCount - 1];
                m_ResolvedRotations[removeAtIndex] = m_ResolvedRotations[m_DecalsCount - 1];
                m_ResolvedScales[removeAtIndex] = m_ResolvedScales[m_DecalsCount - 1];
                m_ResolvedSizeOffsets[removeAtIndex] = m_ResolvedSizeOffsets[m_DecalsCount - 1];
                m_ScaleModes[removeAtIndex] = m_ScaleModes[m_DecalsCount - 1];
                m_NormalToWorlds[removeAtIndex] = m_NormalToWorlds[m_DecalsCount - 1];
                m_DecalToWorlds[removeAtIndex] = m_DecalToWorlds[m_DecalsCount - 1];
                m_BoundingSpheres[removeAtIndex] = m_BoundingSpheres[m_DecalsCount - 1];
                m_Dirty[removeAtIndex] = m_Dirty[m_DecalsCount - 1];
                m_CachedBoundingSpheres[removeAtIndex] = m_CachedBoundingSpheres[m_DecalsCount - 1];
            }

            private void DisposeJobArrays()
            {
                m_CachedTransforms.Dispose();
                m_Positions.Dispose();
                m_Rotations.Dispose();
                m_Scales.Dispose();
                m_Sizes.Dispose();
                m_Offsets.Dispose();
                m_ResolvedRotations.Dispose();
                m_ResolvedScales.Dispose();
                m_ResolvedSizeOffsets.Dispose();
                m_ScaleModes.Dispose();
                m_NormalToWorlds.Dispose();
                m_DecalToWorlds.Dispose();
                m_BoundingSpheres.Dispose();
                m_Dirty.Dispose();
                m_CachedBoundingSpheres = null;
            }

            internal void StartUpdateJob()
            {
                m_UpdateJobHandle.Complete();
                var updateJob = new UpdateJob()
                {
                    positions = m_Positions,
                    rawRotations = m_Rotations,
                    rawScales = m_Scales,
                    resolvedScales = m_ResolvedScales,
                    resolvedRotations = m_ResolvedRotations,
                    resolvedSizesOffsets = m_ResolvedSizeOffsets,
                    dirty = m_Dirty,
                    rawSizes = m_Sizes,
                    rawOffsets = m_Offsets,
                    scaleModes = m_ScaleModes,
                    normalToWorlds = m_NormalToWorlds,
                    decalToWorlds = m_DecalToWorlds,
                    boundingSpheres = m_BoundingSpheres,
                    minDistance = System.Single.Epsilon
                };
                m_UpdateJobHandle = updateJob.Schedule(m_CachedTransforms);
            }
        }

#if ENABLE_BURST_1_5_0_OR_NEWER
        [Unity.Burst.BurstCompile]
#endif
        internal struct UpdateJob : IJobParallelForTransform
        {
            private static readonly quaternion k_MinusYtoZRotation = quaternion.EulerXYZ(-math.PI / 2.0f, 0, 0);
            private static readonly quaternion k_YtoZRotation = quaternion.EulerXYZ(math.PI / 2.0f, 0, 0);
            private static readonly float3 sFloat3One = new float3(1, 1, 1);
            public float minDistance;

            public NativeArray<float3> positions;
            public NativeArray<quaternion> rawRotations;
            public NativeArray<float3> rawScales;
            public NativeArray<float3> resolvedScales;
            public NativeArray<quaternion> resolvedRotations;
            public NativeArray<float4x4> resolvedSizesOffsets;
            public NativeArray<bool> dirty;

            [ReadOnly] public NativeArray<float3> rawSizes;
            [ReadOnly] public NativeArray<float3> rawOffsets;
            [ReadOnly] public NativeArray<DecalScaleMode> scaleModes;
            [WriteOnly] public NativeArray<float4x4> normalToWorlds;
            [WriteOnly] public NativeArray<float4x4> decalToWorlds;
            [WriteOnly] public NativeArray<BoundingSphere> boundingSpheres;

            private float DistanceBetweenQuaternions(quaternion a, quaternion b)
            {
                return math.distancesq(a.value, b.value);
            }

            private float3 effectiveScale(int index, in TransformAccess transform)
            {
                return scaleModes[index] == DecalScaleMode.InheritFromHierarchy ? transform.localToWorldMatrix.lossyScale : sFloat3One;
            }

            private float3 resolveDecalSize(int index, float3 scale, in TransformAccess transform)
            {
                // If Z-scale is negative the forward direction for rendering will be fixed by rotation,
                // so we need to flip the scale of the affected axes back.
                // The final sign of Z will depend on the other two axes, so we actually need to fix only Y here.
                if (scale.z < 0f)
                    scale.y *= -1f;

                // Flipped projector (with 1 or 3 negative components of scale) would be invisible.
                // In this case we additionally flip Z.
                bool flipped = scale.x < 0f ^ scale.y < 0f ^ scale.z < 0f;
                if (flipped)
                    scale.z *= -1f;

                float3 decalSize = rawSizes[index];
                return new float3(decalSize.x * scale.x, decalSize.z * scale.z, decalSize.y * scale.y);
            }

            private float3 resolveDecalOffset(int index, float3 scale, in TransformAccess transform)
            {
                // If Z-scale is negative the forward direction for rendering will be fixed by rotation,
                // so we need to flip the scale of the affected axes back.
                if (scale.z < 0f)
                {
                    scale.y *= -1f;
                    scale.z *= -1f;
                }

                float3 decalOffset = rawOffsets[index];
                return new float3(decalOffset.x * scale.x, -decalOffset.z * scale.z, decalOffset.y * scale.y);
            }

            private quaternion resolveRotation(int index, in float3 scale, in TransformAccess transform)
            {
                return transform.rotation * (scale.z >= 0f ? k_MinusYtoZRotation : k_YtoZRotation);
            }

            public void Execute(int index, TransformAccess transform)
            {
                bool isDirty = dirty[index];
                // Check if transform changed
                bool positionChanged = math.distancesq(transform.position, positions[index]) > minDistance;
                if (positionChanged)
                    positions[index] = transform.position;

                bool scaleChanged = math.distancesq(transform.localToWorldMatrix.lossyScale, rawScales[index]) > minDistance;
                if (scaleChanged)
                    rawScales[index] = transform.localToWorldMatrix.lossyScale;

                if (scaleChanged || isDirty)
                    resolvedScales[index] = effectiveScale(index, transform);

                bool rotationChanged = DistanceBetweenQuaternions(transform.rotation, rawRotations[index]) > minDistance;
                if (rotationChanged)
                    rawRotations[index] = transform.rotation;

                if (rotationChanged || isDirty)
                    resolvedRotations[index] = resolveRotation(index, resolvedScales[index], transform);

                // Early out if transform did not changed
                if (!positionChanged && !rotationChanged && !scaleChanged && !isDirty)
                    return;

                if (isDirty || rotationChanged || scaleChanged)
                    resolvedSizesOffsets[index] = math.mul(float4x4.Translate(resolveDecalOffset(index, resolvedScales[index], transform)), float4x4.Scale(resolveDecalSize(index, resolvedScales[index], transform)));

                float4x4 localToWorld = float4x4.TRS(transform.position, resolvedRotations[index], sFloat3One);
                float4x4 decalRotation = localToWorld;
                // z/y axis swap for normal to decal space, Unity is column major
                float4 temp = decalRotation.c1;
                decalRotation.c1 = decalRotation.c2;
                decalRotation.c2 = temp;
                normalToWorlds[index] = decalRotation;

                float4x4 sizeOffset = resolvedSizesOffsets[index];
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
