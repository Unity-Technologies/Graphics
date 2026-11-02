#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Main thread only. Coalescing pending changes by key is safe because an EntityId is never reused.
    // Transforms are appended rather than coalesced, so producers can write them from a job.
    static class SurfaceCacheEntityChangeBridge
    {
        static NativeList<SurfaceCacheEntityInstanceRecord> s_PendingChanged;
        static NativeHashMap<EntityId, int> s_PendingChangedIndex;
        static NativeList<SurfaceCacheSubMeshMaterial> s_PendingMaterials;
        static NativeList<EntityId> s_PendingDestroyed;
        static NativeList<SurfaceCacheEntityTransformRecord> s_PendingTransforms;

        static bool s_ConsumerRegistered;
        static bool s_DrainInProgress;
        // Records and transforms share a key only across producer passes or after an aborted drain, so only then
        // is there anything to fold.
        static bool s_ProducerPassActive;
        static bool s_FoldNeeded;
        static uint s_ConsumerVersion;
        static JobHandle s_TransformJob;

        public static bool IsConsumerPresent => s_ConsumerRegistered;

        // Producers push their full state again when this changes, so a recreated consumer starts complete.
        public static uint ConsumerVersion => s_ConsumerVersion;

        public static bool TryRegisterConsumer()
        {
            if (s_ConsumerRegistered)
                return false;

            s_ConsumerRegistered = true;
            s_ConsumerVersion++;
            s_PendingChanged = new NativeList<SurfaceCacheEntityInstanceRecord>(Allocator.Persistent);
            s_PendingChangedIndex = new NativeHashMap<EntityId, int>(64, Allocator.Persistent);
            s_PendingMaterials = new NativeList<SurfaceCacheSubMeshMaterial>(Allocator.Persistent);
            s_PendingDestroyed = new NativeList<EntityId>(Allocator.Persistent);
            s_PendingTransforms = new NativeList<SurfaceCacheEntityTransformRecord>(Allocator.Persistent);
            ClearPending();
            return true;
        }

        public static void UnregisterConsumer()
        {
            Debug.Assert(s_ConsumerRegistered);

            CompleteTransformJob();
            s_ConsumerRegistered = false;
            s_DrainInProgress = false;

            if (s_PendingTransforms.IsCreated)
            {
                s_PendingChanged.Dispose();
                s_PendingChangedIndex.Dispose();
                s_PendingMaterials.Dispose();
                s_PendingDestroyed.Dispose();
                s_PendingTransforms.Dispose();
            }
        }

        // Appended to by producer jobs. The assert below catches acquiring across a drain, which is an ordering
        // mistake rather than a race: this bridge is main thread only, so there is nothing to lock against.
        public static NativeList<SurfaceCacheEntityTransformRecord> AcquireTransformChangedList()
        {
            Debug.Assert(s_ConsumerRegistered, "AcquireTransformChangedList without a consumer.");
            Debug.Assert(!s_DrainInProgress, "AcquireTransformChangedList during a drain.");
            CompleteTransformJob();
            MarkPushOutsideProducerPass();
            return s_PendingTransforms;
        }

        public static void SetTransformJob(JobHandle handle)
        {
            s_TransformJob = JobHandle.CombineDependencies(s_TransformJob, handle);
        }

        public static void CompleteTransformJob()
        {
            s_TransformJob.Complete();
            s_TransformJob = default;
        }

        public static void BeginProducerPass()
        {
            if (s_PendingChanged.Length != 0 || s_PendingDestroyed.Length != 0 || s_PendingTransforms.Length != 0)
                s_FoldNeeded = true;

            s_ProducerPassActive = true;
        }

        public static void EndProducerPass()
        {
            s_ProducerPassActive = false;
        }

        static void MarkPushOutsideProducerPass()
        {
            if (!s_ProducerPassActive)
                s_FoldNeeded = true;
        }

        public static void PushChanged(in SurfaceCacheEntityInstanceRecord record, ReadOnlySpan<SurfaceCacheSubMeshMaterial> materials)
        {
            if (!IsConsumerPresent)
                return;

            MarkPushOutsideProducerPass();
            Writer.PushChanged(record, materials);
        }

        // For jobs that push records. Unavailable without a consumer or during a drain, whose views a push would invalidate.
        public static bool TryAcquireChangeWriter(out SurfaceCacheEntityChangeWriter writer)
        {
            writer = Writer;
            MarkPushOutsideProducerPass();
            return s_ConsumerRegistered && !s_DrainInProgress;
        }

        static SurfaceCacheEntityChangeWriter Writer => new()
        {
            Changed = s_PendingChanged,
            Index = s_PendingChangedIndex,
            Materials = s_PendingMaterials,
            Transforms = s_PendingTransforms,
        };

        public static void PushDestroyed(in EntityId key)
        {
            if (!IsConsumerPresent)
                return;

            MarkPushOutsideProducerPass();

            if (s_PendingChangedIndex.TryGetValue(key, out int at))
            {
                s_PendingChangedIndex.Remove(key);
                s_PendingChanged.RemoveAtSwapBack(at);

                if (at < s_PendingChanged.Length)
                    s_PendingChangedIndex[s_PendingChanged[at].Key] = at;
            }

            s_PendingDestroyed.Add(key);
        }

        // Views into the bridge's storage: valid until EndDrain, invalidated by any push in between.
        public static void BeginDrain(
            out NativeArray<SurfaceCacheEntityInstanceRecord> changed,
            out NativeArray<SurfaceCacheSubMeshMaterial> changedMaterials,
            out NativeArray<EntityId> destroyed,
            out NativeArray<SurfaceCacheEntityTransformRecord> transformChanged)
        {
            Debug.Assert(!s_DrainInProgress, "BeginDrain without a matching EndDrain.");
            CompleteTransformJob();
            s_DrainInProgress = true;

            if (s_FoldNeeded && (s_PendingChanged.Length != 0 || s_PendingDestroyed.Length != 0))
                FoldSupersededTransforms();

            changed = s_PendingChanged.AsArray();
            changedMaterials = s_PendingMaterials.AsArray();
            destroyed = s_PendingDestroyed.AsArray();
            transformChanged = s_PendingTransforms.AsArray();
        }

        public static void EndDrain()
        {
            Debug.Assert(s_DrainInProgress, "EndDrain without a matching BeginDrain.");
            s_DrainInProgress = false;
            ClearPending();
        }

        public static void AbortDrain()
        {
            Debug.Assert(s_DrainInProgress, "AbortDrain without a matching BeginDrain.");
            s_DrainInProgress = false;
            s_FoldNeeded = true;
        }

        // A transform pushed before its key's record is older than the record and is dropped; one pushed after is folded in.
        static void FoldSupersededTransforms()
        {
            new FoldSupersededTransformsJob
            {
                Transforms = s_PendingTransforms,
                Changed = s_PendingChanged,
                ChangedIndex = s_PendingChangedIndex,
                Destroyed = s_PendingDestroyed,
            }.Run();
        }

        [BurstCompile]
        struct FoldSupersededTransformsJob : IJob
        {
            public NativeList<SurfaceCacheEntityTransformRecord> Transforms;
            public NativeList<SurfaceCacheEntityInstanceRecord> Changed;
            [ReadOnly] public NativeHashMap<EntityId, int> ChangedIndex;
            [ReadOnly] public NativeList<EntityId> Destroyed;

            public void Execute()
            {
                var destroyed = new NativeHashSet<EntityId>(math.max(1, Destroyed.Length), Allocator.Temp);
                for (int i = 0; i < Destroyed.Length; i++)
                    destroyed.Add(Destroyed[i]);

                int kept = 0;
                for (int i = 0; i < Transforms.Length; i++)
                {
                    var transform = Transforms[i];
                    if (ChangedIndex.TryGetValue(transform.Key, out int at))
                    {
                        var record = Changed[at];
                        if (i >= record.TransformsBefore)
                        {
                            record.LocalToWorld = transform.LocalToWorld;
                            Changed[at] = record;
                        }

                        continue;
                    }

                    if (destroyed.Contains(transform.Key))
                        continue;

                    if (kept != i)
                        Transforms[kept] = transform;
                    kept++;
                }

                Transforms.Length = kept;

                // No kept transform belongs to a pending record, so this only keeps later transforms from looking older.
                for (int r = 0; r < Changed.Length; r++)
                {
                    var record = Changed[r];
                    if (record.TransformsBefore > kept)
                    {
                        record.TransformsBefore = kept;
                        Changed[r] = record;
                    }
                }
            }
        }

        static void ClearPending()
        {
            if (s_PendingChanged.Length != 0)
                s_PendingChangedIndex.Clear();

            s_PendingChanged.Clear();
            s_PendingMaterials.Clear();
            s_PendingDestroyed.Clear();
            s_FoldNeeded = false;

            if (s_PendingTransforms.IsCreated)
                s_PendingTransforms.Clear();
        }
    }

    struct SurfaceCacheEntityChangeWriter
    {
        public NativeList<SurfaceCacheEntityInstanceRecord> Changed;
        public NativeHashMap<EntityId, int> Index;
        public NativeList<SurfaceCacheSubMeshMaterial> Materials;
        public NativeList<SurfaceCacheEntityTransformRecord> Transforms;

        public void PushChanged(in SurfaceCacheEntityInstanceRecord record, ReadOnlySpan<SurfaceCacheSubMeshMaterial> materials)
        {
            var stored = record;
            stored.MaterialsCount = materials.Length;
            stored.TransformsBefore = Transforms.Length;

            // Avoids re-appending when a key's assignment count does not grow.
            if (Index.TryGetValue(record.Key, out int at))
            {
                var previous = Changed[at];
                if (previous.MaterialsCount >= materials.Length)
                {
                    for (int i = 0; i < materials.Length; i++)
                        Materials[previous.MaterialsStart + i] = materials[i];

                    stored.MaterialsStart = previous.MaterialsStart;
                }
                else
                {
                    stored.MaterialsStart = Materials.Length;
                    for (int i = 0; i < materials.Length; i++)
                        Materials.Add(materials[i]);
                }

                Changed[at] = stored;
                return;
            }

            stored.MaterialsStart = Materials.Length;
            for (int i = 0; i < materials.Length; i++)
                Materials.Add(materials[i]);

            Index.Add(record.Key, Changed.Length);
            Changed.Add(stored);
        }
    }

    sealed class EntityChangeSource : IDisposable
    {
        internal const string k_ContentionError =
            "Multiple Surface Cache GI renderer features are loaded, but only one of them can receive entity (ECS) " +
            "changes. Entities will be missing from this feature's global illumination until the other features are " +
            "removed or their renderer is recreated. GameObject-based renderers are not affected.";

        static readonly Unity.Profiling.ProfilerMarker k_CollectMarker = new("SurfaceCache.EntityChangeCollection");

        bool m_BridgeAcquired;
        bool m_DrainBegun;
        bool m_ContentionLogged;

        bool TryAcquireBridge()
        {
            if (!m_BridgeAcquired)
                m_BridgeAcquired = SurfaceCacheEntityChangeBridge.TryRegisterConsumer();

            return m_BridgeAcquired;
        }

        public void CollectChanges(SurfaceCacheWorldChangeSet changeSet)
        {
            using var _ = k_CollectMarker.Auto();

            if (!TryAcquireBridge())
            {
                if (!m_ContentionLogged)
                {
                    m_ContentionLogged = true;
                    Debug.LogError(k_ContentionError);
                }

                return;
            }

            SurfaceCacheEntityChangeBridge.BeginDrain(out var changed, out var materials, out var destroyed, out var transformChanged);
            m_DrainBegun = true;

            changeSet.EntityInstanceChangedList = changed;
            changeSet.EntityInstanceMaterialList = materials;
            changeSet.EntityInstanceDestroyedList = destroyed;
            changeSet.EntityInstanceTransformChangedList = transformChanged;
        }

        public void EndCollectChanges()
        {
            if (!m_DrainBegun)
                return;

            m_DrainBegun = false;
            SurfaceCacheEntityChangeBridge.EndDrain();
        }

        public void AbortCollectChanges()
        {
            if (!m_DrainBegun)
                return;

            m_DrainBegun = false;
            SurfaceCacheEntityChangeBridge.AbortDrain();
        }

        public void Dispose()
        {
            if (!m_BridgeAcquired)
                return;

            AbortCollectChanges();
            m_BridgeAcquired = false;
            SurfaceCacheEntityChangeBridge.UnregisterConsumer();
        }
    }
}

#endif
